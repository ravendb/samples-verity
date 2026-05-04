using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Attachments;
using Raven.Client.Documents.Subscriptions;
using RavenDB.Samples.Verity.Model;
using RavenDB.Samples.Verity.Model.Subscriptions;
using Sparrow;
using System.ComponentModel.Design;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RavenDB.Samples.Verity.App.Infrastructure;

public class SecEdgarApi(HttpClient http, IDocumentStore store, ILogger<SecEdgarApi> logger)
{
    private const string EdgarBase = "https://www.sec.gov";
    private const string DataBase  = "https://data.sec.gov";
    private string Cik = null!;
    private string CompanyName = null!;
    private DateTime FiscalYearStart = default;

    // ── 1. CIK → list of filings of a given type ─────────────────────────────

    public async Task<List<EdgarFiling>> GetRecentFilingsAsync(List<string> formType, int maxCount, CancellationToken ct = default)
    {
        var url = $"{DataBase}/submissions/CIK{Cik}.json";
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var recent      = doc.RootElement.GetProperty("filings").GetProperty("recent");
        var forms       = recent.GetProperty("form");
        var accessions  = recent.GetProperty("accessionNumber");
        var dates       = recent.GetProperty("filingDate");
        var reportDates = recent.GetProperty("reportDate");
        var primaryDocs = recent.GetProperty("primaryDocument");

        var results = new List<EdgarFiling>();

        for (int i = 0; i < forms.GetArrayLength() && results.Count < maxCount; i++)
        {
            if (!formType.Contains(forms[i].GetString()!))
                continue;

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
                AccessionNumber: accessions[i].GetString()!,
                FilingDate:      filingDate,
                ReportDate:      reportDate,
                PrimaryDocument: primaryDocs[i].GetString()!,
                FormType:        forms[i].GetString()!
            ));
        }

        return results;
    }

    // ── 2. Downloading HTM, cleaning, chunking and saving to RavenDB ─────────

    private static int GetFiscalQuarter(int fiscalYearStart, int month)
    {
        int monthsIntoFiscalYear = ((month - fiscalYearStart + 12) % 12) + 1;
        return (monthsIntoFiscalYear - 1) / 3 + 1;
    }

    public async Task DownloadAndSaveFilingAsync(EdgarFiling filing, string formType, CancellationToken ct = default)
    {
        var accNoDashes = filing.AccessionNumber.Replace("-", "");
        var cikNumeric  = long.Parse(Cik).ToString();
        var htmUrl      = $"{EdgarBase}/Archives/edgar/data/{cikNumeric}/{accNoDashes}/{filing.PrimaryDocument}";
        var quarter     = GetFiscalQuarter(FiscalYearStart.Month, filing.ReportDate.Month);

        logger.LogInformation("Downloading {FormType}: {Url}", formType, htmUrl);

        using var response = await http.GetAsync(htmUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var docId = $"Reports/{CompanyName}/{filing.ReportDate.Year}/Q{quarter}";

        using var session = store.OpenAsyncSession();

        if (await session.Advanced.ExistsAsync(docId, ct))
        {
            logger.LogInformation("Document {DocId} already exists – skipping.", docId);
            return;
        }

        // ── Clean + split document if need be ────────────────────────────────
        await using var rawStream     = await response.Content.ReadAsStreamAsync(ct);
        using  var      cleanedStream = HtmProcessor.CleanHtml(rawStream);

        const int maxSize = 600 * 1024; // 600 KB
        byte[] allBytes = cleanedStream.ToArray();
        const int chunkBytes = 200 * 1024; // 200 KB
        int total = 0;

        if (allBytes.Length > maxSize)
        {
            total = Math.Max(1, (int)Math.Ceiling((double)allBytes.Length / chunkBytes));
        }

        // ── Build report document ────────────────────────────────────────────
        var report = new Report
        {
            Id              = docId,
            CompanyId       = $"Companies/{CompanyName}",
            AccessionNumber = filing.AccessionNumber,
            ReportDate      = filing.ReportDate.ToString("yyyy-MM-dd"),
            FilingDate      = filing.FilingDate.ToString("yyyy-MM-dd"),
            DaysBetween     = (filing.FilingDate - filing.ReportDate).Days,
            Quarter         = quarter,
            Year            = filing.ReportDate.Year,
            FormType        = formType,
            SourceUrl       = htmUrl,
            ChunkCount      = total
        };

        await session.StoreAsync(report, docId, ct);

        var remoteParameters = new RemoteAttachmentParameters(
            identifier: Setup.Constants.RemoteAttachmentId, // The remote destination ID you’ve defined
            at: DateTime.UtcNow.AddMinutes(10));

        var storeParameters = new StoreAttachmentParameters($"form{formType}.htm", cleanedStream)
        {
            RemoteParameters = remoteParameters,
            ContentType = "text/html"
        };

        session.Advanced.Attachments.Store(docId, storeParameters);

        await session.SaveChangesAsync(ct);

        // ── Archive metadata ─────────────────────────────────────────────────

        int monthsBeforeSub = 12 - FiscalYearStart.Month;
        int yearsToSub = 0;
        if ((filing.ReportDate.Month - FiscalYearStart.Month + 12) % 12 > monthsBeforeSub)
            yearsToSub = 1;

        session.Advanced.GetMetadataFor(report)["@archive-at"] = FiscalYearStart.AddYears(filing.ReportDate.Year - yearsToSub + 2);
        session.Advanced.GetMetadataFor(report)["@expires"] = FiscalYearStart.AddYears(filing.ReportDate.Year - yearsToSub + 5);

        await session.SaveChangesAsync(ct);
        logger.LogInformation("Saved report {DocId}", docId);

        if (DateTime.UtcNow >= FiscalYearStart.AddYears(filing.ReportDate.Year - yearsToSub + 2))
        {
            logger.LogInformation("Report {DocId} is already archived based on fiscal year start – skipping parts.", docId);
            return;
        }else if (total == 0)
        {
            logger.LogInformation("Report {DocId} is under size limit – no need to split into parts.", docId);
            return;
        }

        for (int i = 1; i <= total; i++)
        {
            int start  = (i - 1) * chunkBytes;
            int length = Math.Min(chunkBytes, allBytes.Length - start);

            var part = new ReportPart
            {
                Id              = $"ReportParts/{i}/{filing.AccessionNumber}",
                ReportId        = docId,
                AccessionNumber = filing.AccessionNumber,
                FormType        = formType,
                Index           = i,
                Total           = total,
            };

            await session.StoreAsync(part, part.Id, ct);

            var textStream = new MemoryStream(allBytes, start, length, writable: false);
            session.Advanced.Attachments.Store(part.Id, "text.htm", textStream, "text/html");
        }
        await session.SaveChangesAsync();
    }

    // ── 3. Main entry point ──────────────────────────────────────────────────

    public async Task FetchAndSaveAllFilingsAsync(
        Company company, int maxFilings = 5, CancellationToken ct = default)
    {
        Cik             = company.Cik;
        CompanyName     = company.Name;
        FiscalYearStart = company.FiscalYearStart;

        logger.LogInformation("CIK {Cik} – {Name}", Cik, CompanyName);

        var formTypes = new List<string> { "10-Q", "10-K" };
        var filings   = await GetRecentFilingsAsync(formTypes, maxFilings, ct);

        logger.LogInformation("Found {Count} filings", filings.Count);

        foreach (var filing in filings)
        {
            await DownloadAndSaveFilingAsync(filing, filing.FormType, ct);
            await Task.Delay(120, ct);
        }

        Cik             = null!;
        CompanyName     = null!;
        FiscalYearStart = default;
    }

    // ── 4. Fetching and saving company data ──────────────────────────────────

    private static readonly string[] FirstNames =
    [
        "James", "Mary", "Robert", "Patricia", "Michael",
            "Jennifer", "William", "Linda", "David", "Barbara"
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones",
        "Garcia", "Miller", "Davis", "Wilson", "Martinez"
    ];

    public async Task<Company> FetchAndSaveCompanyAsync(string paddedCik, CancellationToken ct = default)
    {
        var url = $"{DataBase}/submissions/CIK{paddedCik}.json";

        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc  = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var       root = doc.RootElement;

        var companyName = root.GetProperty("name").GetString() ?? paddedCik;
        var fiscalYearEnd   = root.TryGetProperty("fiscalYearEnd", out var fye) ? fye.GetString() : null;
        var fiscalYearStart = new DateTime(1, int.Parse(fiscalYearEnd!.Substring(0, 2)), 1, 0, 0, 0, DateTimeKind.Utc);
        fiscalYearStart     = fiscalYearStart.AddMonths(1);

        if (fiscalYearStart.Year != 1)
            fiscalYearStart = fiscalYearStart.AddYears(-1);

        var company = new Company
        {
            Id = $"Companies/{companyName}",
            Name = companyName,
            Cik = paddedCik,
            Sic = root.TryGetProperty("sic", out var sic) ? sic.GetString() : null,
            SicDescription = root.TryGetProperty("sicDescription", out var sicDesc) ? sicDesc.GetString() : null,
            FiscalYearStart = fiscalYearStart,
        };

        NewReportsSubscription.Create(store, companyName, company.Id).GetAwaiter().GetResult();

        using var session = store.OpenAsyncSession();
        await session.StoreAsync(company, company.Id, ct);

        Random Rng = new(21);
        var usedPairs = new HashSet<string>();
        for (var i = 0; i < 2; i++)
        {
            string firstName, lastName;
            do
            {
                firstName = FirstNames[Rng.Next(FirstNames.Length)];
                lastName = LastNames[Rng.Next(LastNames.Length)];
            } while (!usedPairs.Add($"{firstName} {lastName}"));

            var domain = company.Name.Replace(" ", "").Replace(",", "").Replace(".", "").ToLowerInvariant();
            await session.StoreAsync(new User
            {
                Id = $"Users/{company.Name}/{firstName} {lastName}",
                CompanyId = company.Id,
                Name = firstName,
                Surname = lastName,
                Email = $"{firstName.ToLower()}{lastName.ToLower()}@{domain}.com"
            });
        }
        await session.SaveChangesAsync(ct);

        logger.LogInformation("Saved company {Name} (CIK: {Cik})", company.Name, company.Cik);
        return company;
    }
}

// ── Models ────────────────────────────────────────────────────────────────────

public record EdgarFiling(
    string   AccessionNumber,
    DateTime FilingDate,
    DateTime ReportDate,
    string   PrimaryDocument,
    string   FormType
);
