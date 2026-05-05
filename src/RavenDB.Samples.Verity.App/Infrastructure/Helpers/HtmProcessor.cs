using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;

namespace RavenDB.Samples.Verity.App;

/// <summary>
/// Utility class for cleaning and chunking SEC EDGAR HTML filings
/// before they are sent to an LLM or stored in RavenDB.
/// </summary>
public static partial class HtmProcessor
{
    // ── Cleaning ──────────────────────────────────────────────────────────────
    // Optimisations (in order of impact):
    //   1. Remove entire XBRL namespace blocks (xbrli:xbrl, ix:hidden)   ~3 KB
    //   2. Unwrap inline XBRL tags (ix:*) – keep inner text, drop wrapper ~231 KB
    //   3. Strip XBRL-only attributes (contextref, unitref, decimals …)   ~71 KB
    //   4. Remove empty presentational elements                            ~59 KB
    //   5. Existing: script/style/link/noscript/comments/style-attr/meta

    private static readonly string[] _xbrlDataAttrs =
    [
        "contextref", "unitref", "decimals", "scale",
        "name",       "format",  "dimension", "scheme",
        "sign",       "escape",  "continuedat", "xsi:nil",
    ];

    // Tags to unwrap: inner content is kept, the wrapper tag itself is removed.
    private static readonly HashSet<string> _unwrapTags = new(StringComparer.OrdinalIgnoreCase)
    {
        // Inline styling / formatting
        "span", "b", "i", "u", "strong", "em", "s", "small", "sub", "sup",
        "font", "big", "tt", "cite", "abbr", "acronym", "bdo",
        // Block containers
        "div", "p", "pre", "blockquote",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "section", "article", "aside", "main", "header", "footer", "nav",
        // Table structure
        "table", "tbody", "thead", "tfoot", "tr", "td", "th",
        "colgroup", "col", "caption",
        // Lists
        "ul", "ol", "li", "dl", "dt", "dd",
        // Forms / misc wrappers
        "label", "form", "fieldset", "legend",
        // Document structure
        "html", "head", "body",
        // Links – keep link text, drop the <a> wrapper
        "a",
    };

    // Self-closing tags with no useful text content – remove entirely.
    private static readonly HashSet<string> _removeTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "br", "hr", "img", "input", "meta", "link",
        "col", "wbr", "area", "base",
    };

    public static MemoryStream CleanHtml(Stream rawStream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var win1252   = Encoding.GetEncoding(1252);
        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        var htmlDoc = new HtmlDocument
        {
            OptionOutputAsXml     = false,
            OptionWriteEmptyNodes = true
        };
        htmlDoc.Load(rawStream, win1252);

        // 1. Remove all XBRL namespace nodes entirely – they are pure metadata with
        //    no display value.  This covers xbrli:*, link:*, xbrldi:* and the
        //    ix:hidden block.  ix:nonfraction / ix:nonnumeric are handled in step 2
        //    (unwrap) so their visible text is kept.
        foreach (var node in htmlDoc.DocumentNode
            .Descendants()
            .Where(n =>
            {
                var name = n.Name;
                return name.StartsWith("xbrli:",  StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("link:",   StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("xbrldi:", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("ix:hidden",   StringComparison.OrdinalIgnoreCase)
                    || name.Equals("ix:header",   StringComparison.OrdinalIgnoreCase)
                    || name.Equals("ix:resources",StringComparison.OrdinalIgnoreCase);
            })
            .ToList())
        {
            if (node.ParentNode != null)
                node.Remove();
        }

        // 2. Unwrap remaining ix:* tags (ix:nonfraction, ix:nonnumeric, ix:continuation…)
        //    – keep the inner text (the actual number/value), drop the XBRL wrapper.
        foreach (var node in htmlDoc.DocumentNode
            .Descendants()
            .Where(n => n.Name.StartsWith("ix:", StringComparison.OrdinalIgnoreCase))
            .ToList())
        {
            var parent = node.ParentNode;
            if (parent == null) continue;
            foreach (var child in node.ChildNodes.ToList())
                parent.InsertBefore(child, node);
            parent.RemoveChild(node);
        }

        // 3. Strip XBRL-only attributes (they carry no display value)
        var allNodes = htmlDoc.DocumentNode.SelectNodes("//*");
        if (allNodes != null)
        {
            foreach (var node in allNodes)
                foreach (var attr in _xbrlDataAttrs)
                    node.Attributes[attr]?.Remove();
        }

        // 4. script / style / link / noscript / comments
        foreach (var xpath in new[] { "//script", "//style", "//link", "//noscript" })
            RemoveNodes(htmlDoc, xpath);

        RemoveNodes(htmlDoc, "//comment()");

        // 5. Remove inline style attributes
        var styledNodes = htmlDoc.DocumentNode.SelectNodes("//*[@style]");
        if (styledNodes != null)
            foreach (var node in styledNodes.ToList())
                node.Attributes["style"]?.Remove();

        RemoveNodes(htmlDoc, "//meta[@http-equiv]");

        // 6a. Remove self-closing / content-free tags entirely (br, hr, img, …)
        foreach (var node in htmlDoc.DocumentNode
            .Descendants()
            .Where(n => _removeTags.Contains(n.Name))
            .ToList())
        {
            if (node.ParentNode != null)
                node.Remove();
        }

        // 6b. Unwrap all remaining styling / structural tags.
        //     Children are re-inserted in place of the wrapper; text is preserved.
        //     Process bottom-up (leaves first) so inner wrappers are resolved before outer ones.
        foreach (var node in htmlDoc.DocumentNode
            .Descendants()
            .Where(n => _unwrapTags.Contains(n.Name))
            .ToList())
        {
            var parent = node.ParentNode;
            if (parent == null) continue;
            foreach (var child in node.ChildNodes.ToList())
                parent.InsertBefore(child, node);
            parent.RemoveChild(node);
        }

        var ms = new MemoryStream();
        htmlDoc.Save(ms, utf8NoBom);
        ms.Position = 0;
        return ms;

        static void RemoveNodes(HtmlDocument doc, string xpath)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes is null) return;
            foreach (var node in nodes.ToList())
                node.Remove();
        }
    }

    // ── Chunking ──────────────────────────────────────────────────────────────

    public static List<HtmChunk> ChunkHtml(
        string html,
        int    maxCharsPerChunk  = 190_000,
        int    boundaryLookahead = 15_000,
        int    minRemainingChars = 20_000)
    {
        if (string.IsNullOrEmpty(html))
            return [];

        // All character positions where a SEC "ITEM X[A]." appears in the text.
        int[] itemPositions = ItemBoundaryRegex()
            .Matches(html)
            .Select(m => m.Index)
            .ToArray();

        var chunks = new List<HtmChunk>();
        int pos    = 0;
        int total  = html.Length;

        while (pos < total)
        {
            int end = Math.Min(pos + maxCharsPerChunk, total);

            if (end < total)
            {
                // Absorb a tiny tail into the current chunk rather than
                // producing an orphan chunk smaller than minRemainingChars.
                if (total - end < minRemainingChars)
                {
                    end = total;
                }
                else
                {
                    // Snap the cut to the nearest ITEM boundary within the
                    // lookahead window — but only when the tail that remains
                    // after the snap is at least minRemainingChars.
                    int windowStart = Math.Max(pos + 1, end - boundaryLookahead / 2);
                    int windowEnd   = Math.Min(total,   end + boundaryLookahead);

                    int snap = Array.FindIndex(itemPositions,
                        p => p >= windowStart && p <= windowEnd);

                    if (snap >= 0 && total - itemPositions[snap] >= minRemainingChars)
                        end = itemPositions[snap];
                }
            }

            string       text     = html[pos..end];
            List<string> sections = ExtractSections(text);

            chunks.Add(new HtmChunk(
                Index:     chunks.Count + 1,
                Total:     0,           // patched below once the full count is known
                CharStart: pos,
                CharEnd:   end,
                Text:      text,
                Sections:  sections
            ));

            pos = end;
        }

        // Patch the Total field now that all chunks are known.
        int n = chunks.Count;
        for (int i = 0; i < n; i++)
            chunks[i] = chunks[i] with { Total = n };

        return chunks;
    }

    /// <summary>
    /// Convenience overload: reads a <see cref="Stream"/> (UTF-8) and chunks it directly.
    /// </summary>
    public static List<HtmChunk> ChunkHtml(
        Stream htmlStream,
        int    maxCharsPerChunk  = 190_000,
        int    boundaryLookahead = 15_000,
        int    minRemainingChars = 20_000)
    {
        using var reader = new StreamReader(htmlStream, Encoding.UTF8, leaveOpen: true);
        return ChunkHtml(reader.ReadToEnd(), maxCharsPerChunk, boundaryLookahead, minRemainingChars);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Returns unique, ordered SEC section identifiers found in a chunk of text.
    // e.g. ["ITEM 1.", "ITEM 1A.", "ITEM 2."]
    private static List<string> ExtractSections(string text)
    {
        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (Match m in ItemBoundaryRegex().Matches(text))
        {
            string key = NormaliseItem(m.Value);
            if (seen.Add(key))
                result.Add(key);
        }

        return result;
    }

    // "ITEM  1a." → "ITEM 1A."
    private static string NormaliseItem(string raw)
        => Regex.Replace(raw.Trim(), @"\s+", " ").ToUpperInvariant();

    [GeneratedRegex(@"\bITEM\s+\d+[A-Z]?\.", RegexOptions.IgnoreCase)]
    private static partial Regex ItemBoundaryRegex();
}

// ── Model ─────────────────────────────────────────────────────────────────────

public record HtmChunk(
    int          Index,
    int          Total,
    int          CharStart,
    int          CharEnd,
    string       Text,
    List<string> Sections
);
