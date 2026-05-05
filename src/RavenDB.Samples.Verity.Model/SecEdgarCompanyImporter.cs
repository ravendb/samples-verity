using System.Text.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using RavenDB.Samples.Verity.Model.Subscriptions;

namespace RavenDB.Samples.Verity.Model;


public static class SecEdgarCompanyImporter
{
    public record CompanyImportData(
        string   PaddedCik,
        string   Name,
        string?  Sic,
        string?  SicDescription,
        DateTime FiscalYearStart);
    
    private const string DataBase = "https://data.sec.gov";

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

    // Pure HTTP + JSON parsing — safe to call in parallel.
    public static async Task<CompanyImportData> FetchCompanyDataAsync(
        HttpClient http, string paddedCik, CancellationToken ct = default)
    {
        var url = $"{DataBase}/submissions/CIK{paddedCik}.json";

        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc  = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var       root = doc.RootElement;

        var name          = root.GetProperty("name").GetString() ?? paddedCik;
        var fiscalYearEnd = root.TryGetProperty("fiscalYearEnd", out var fye) ? fye.GetString() : null;

        var fiscalYearStart = new DateTime(1, int.Parse(fiscalYearEnd!.Substring(0, 2)), 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(1);
        if (fiscalYearStart.Year != 1)
            fiscalYearStart = fiscalYearStart.AddYears(-1);

        return new CompanyImportData(
            PaddedCik:      paddedCik,
            Name:           name,
            Sic:            root.TryGetProperty("sic",            out var sic)     ? sic.GetString()     : null,
            SicDescription: root.TryGetProperty("sicDescription", out var sicDesc) ? sicDesc.GetString() : null,
            FiscalYearStart: fiscalYearStart);
    }

    // Idempotent: if the company already exists, loads and returns it unchanged.
    // Stores Company + 2 Users and creates the subscription but does NOT call SaveChangesAsync —
    // that is the caller's responsibility so it can batch multiple companies in one commit.
    public static async Task<Company> StoreCompanyAsync(
        IAsyncDocumentSession session,
        IDocumentStore store,
        CompanyImportData data,
        CancellationToken ct = default)
    {
        var rng = Random.Shared;
        
        var companyId = Company.BuildId(data.Name);

        if (await session.Advanced.ExistsAsync(companyId, ct))
            return (await session.LoadAsync<Company>(companyId, ct))!;

        var company = new Company
        {
            Id              = companyId,
            Name            = data.Name,
            Cik             = data.PaddedCik,
            Sic             = data.Sic,
            SicDescription  = data.SicDescription,
            FiscalYearStart = data.FiscalYearStart,
        };

        await session.StoreAsync(company, companyId, ct);

        var usedPairs = new HashSet<string>();
        var domain    = data.Name.Replace(" ", "").Replace(",", "").Replace(".", "").ToLowerInvariant();

        for (var i = 0; i < 2; i++)
        {
            string firstName, lastName;
            do
            {
                firstName = FirstNames[rng.Next(FirstNames.Length)];
                lastName  = LastNames[rng.Next(LastNames.Length)];
            } while (!usedPairs.Add($"{firstName} {lastName}"));

            await session.StoreAsync(new User
            {
                Id        = User.BuildId(data.Name, firstName, lastName),
                CompanyId = companyId,
                Name      = firstName,
                Surname   = lastName,
                Email     = $"{firstName.ToLower()}{lastName.ToLower()}@{domain}.com"
            }, ct);
        }

        try
        {
            await NewReportsSubscription.Create(store, data.Name, companyId);
        }
        catch (Exception)
        {
            // Subscription already exists — harmless
        }

        return company;
    }
}
