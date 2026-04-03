using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using RavenDB.Samples.Verity.App.Models;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace RavenDB.Samples.Verity.App;

/// <summary>
/// Klient SEC EDGAR – pobiera raporty 10-Q w formacie HTM
/// i zapisuje je jako dokumenty RavenDB (kolekcja "Reports")
/// z załącznikiem "form10-q.htm".
/// </summary>
public class SecEdgarApi(HttpClient http, IDocumentStore store, ILogger<SecEdgarApi> logger)
{
    private const string EdgarBase = "https://www.sec.gov";
    private const string DataBase  = "https://data.sec.gov";

    // ── 1. CIK → lista zgłoszeń 10-Q ────────────────────────────────────────────

    public async Task<List<EdgarFiling>> GetRecent10QFilingsAsync(
        string paddedCik, int maxCount = 10, CancellationToken ct = default)
    {
        var url = $"{DataBase}/submissions/CIK{paddedCik}.json";
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var companyName = doc.RootElement.GetProperty("name").GetString() ?? "Unknown";
        var recent      = doc.RootElement.GetProperty("filings").GetProperty("recent");
        var forms       = recent.GetProperty("form");
        var accessions  = recent.GetProperty("accessionNumber");
        var dates       = recent.GetProperty("filingDate");
        var reportDates = recent.GetProperty("reportDate");
        var primaryDocs = recent.GetProperty("primaryDocument");

        var results = new List<EdgarFiling>();

        for (int i = 0; i < forms.GetArrayLength() && results.Count < maxCount; i++)
        {
            if (!string.Equals(forms[i].GetString(), "10-Q", StringComparison.OrdinalIgnoreCase))
                continue;

            // EDGAR zwraca daty w formacie yyyy-MM-dd
            var filingDate = DateTime.ParseExact(
                dates[i].GetString()!,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

            var reportDate = DateTime.ParseExact(
                reportDates[i].GetString()!,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

            results.Add(new EdgarFiling(
                Cik:             paddedCik,
                CompanyName:     companyName,
                AccessionNumber: accessions[i].GetString()!,
                FilingDate:      filingDate,
                ReportDate:      reportDate,
                PrimaryDocument: primaryDocs[i].GetString()!
            ));
        }

        return results;
    }

    // ── 2. Pobieranie HTM i zapis do RavenDB ─────────────────────────────────────

    public async Task DownloadAndSave10QAsync(EdgarFiling filing, CancellationToken ct = default)
    {
        var accNoDashes = filing.AccessionNumber.Replace("-", "");
        var cikNumeric  = long.Parse(filing.Cik).ToString();
        var htmUrl      = $"{EdgarBase}/Archives/edgar/data/{cikNumeric}/{accNoDashes}/{filing.PrimaryDocument}";

        logger.LogInformation("Pobieranie 10-Q: {Url}", htmUrl);

        using var response = await http.GetAsync(htmUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var docId = $"Reports/{filing.Cik}-{filing.AccessionNumber}";

        using var session = store.OpenAsyncSession();

        if (await session.Advanced.ExistsAsync(docId, ct))
        {
            logger.LogInformation("Dokument {DocId} już istnieje – pomijanie.", docId);
            return;
        }

        var report = new Report
        {
            Id              = docId,
            Company         = filing.CompanyName,
            Cik             = filing.Cik,
            AccessionNumber = filing.AccessionNumber,
            FilingDate      = filing.FilingDate.ToString("dd.MM.yyyy"),
            ReportDate      = filing.ReportDate.ToString("dd.MM.yyyy"),
            DaysBetween     = (filing.FilingDate - filing.ReportDate).Days,
            FormType        = "10-Q",
            SourceUrl       = htmUrl,
        };

        await session.StoreAsync(report, docId, ct);

        await using var htmStream     = await response.Content.ReadAsStreamAsync(ct);
        using var       cleanedStream = CleanHtml(htmStream);
        session.Advanced.Attachments.Store(docId, "form10-q.htm", cleanedStream, "text/html");
        session.Advanced.GetMetadataFor(report)["@archive-at"] = filing.ReportDate.AddYears(1);

        await session.SaveChangesAsync(ct);
        logger.LogInformation("Zapisano raport {DocId}", docId);
    }

    // ── 3. Główny punkt wejścia ──────────────────────────────────────────────────

    public async Task FetchAndSaveAll10QsAsync(
        string cik, int maxFilings = 5, CancellationToken ct = default)
    {
        var paddedCik = cik.Trim().PadLeft(10, '0');

        logger.LogInformation("CIK {Cik}", paddedCik);

        var filings = await GetRecent10QFilingsAsync(paddedCik, maxFilings, ct);
        logger.LogInformation("Znaleziono {Count} zgłoszeń 10-Q", filings.Count);

        foreach (var filing in filings)
        {
            await DownloadAndSave10QAsync(filing, ct);
            await Task.Delay(120, ct);
        }
    }

    // ── Czyszczenie HTML ─────────────────────────────────────────────────────────

    private static MemoryStream CleanHtml(Stream rawStream)
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

        foreach (var xpath in new[] { "//script", "//style", "//link", "//noscript" })
            RemoveNodes(htmlDoc, xpath);

        RemoveNodes(htmlDoc, "//comment()");

        var styledNodes = htmlDoc.DocumentNode.SelectNodes("//*[@style]");
        if (styledNodes != null)
            foreach (var node in styledNodes.ToList())
                node.Attributes["style"]?.Remove();

        RemoveNodes(htmlDoc, "//meta[@http-equiv]");

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

}

// ── Modele ───────────────────────────────────────────────────────────────────────

public record EdgarFiling(
    string   Cik,
    string   CompanyName,
    string   AccessionNumber,
    DateTime FilingDate,
    DateTime ReportDate,
    string   PrimaryDocument
);