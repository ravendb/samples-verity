using Raven.Migrations;
using RavenDB.Samples.Verity.Model;
using RavenDB.Samples.Verity.Model.Subscriptions;
using System.Globalization;
using System.Text.Json;

namespace RavenDB.Samples.Verity.Setup.Migrations;

[Migration(2)]
public sealed class ImportCompanies(MigrationContext context) : Migration
{
    // Fill in the CIK numbers for the companies you want to import
    private static readonly string[] Ciks = {
        "0000037996",
        "0000104169",
        "0000320187",
        "0001018724",
        "0001045810",
        "0001467858"
    };

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

    private static readonly Random Rng = new(21);

    public override void Up()
    {
        if (Ciks.Length == 0)
            return;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", context.SecEdgarUserAgent);

        using var session = DocumentStore.OpenSession();

        foreach (var cik in Ciks)
        {
            var paddedCik = cik.Trim().PadLeft(10, '0');
            var url       = $"{DataBase}/submissions/CIK{paddedCik}.json";

            using var response = http.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            using var doc    = JsonDocument.Parse(stream);
            var       root   = doc.RootElement;

            var name          = root.GetProperty("name").GetString() ?? paddedCik;
            var fiscalYearEnd = root.TryGetProperty("fiscalYearEnd", out var fye) ? fye.GetString() : null;

            var fiscalYearStart = new DateTime(1, int.Parse(fiscalYearEnd!.Substring(0, 2)), 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

            if (fiscalYearStart.Year != 1)
                fiscalYearStart = fiscalYearStart.AddYears(-1);

            var companyId = Company.BuildId(name);

            if (session.Advanced.Exists(companyId))
                continue;

            session.Store(new Company
            {
                Id              = companyId,
                Name            = name,
                Cik             = paddedCik,
                Sic             = root.TryGetProperty("sic",            out var sic)     ? sic.GetString()     : null,
                SicDescription  = root.TryGetProperty("sicDescription", out var sicDesc) ? sicDesc.GetString() : null,
                FiscalYearStart = fiscalYearStart
            });

            var usedPairs = new HashSet<string>();
            for (var i = 0; i < 2; i++)
            {
                string firstName, lastName;
                do
                {
                    firstName = FirstNames[Rng.Next(FirstNames.Length)];
                    lastName  = LastNames[Rng.Next(LastNames.Length)];
                } while (!usedPairs.Add($"{firstName} {lastName}"));

                var domain = name.Replace(" ", "").Replace(",", "").Replace(".", "").ToLowerInvariant();
                session.Store(new User
                {
                    Id        = User.BuildId(name, firstName, lastName),
                    CompanyId = companyId,
                    Name      = firstName,
                    Surname   = lastName,
                    Email     = $"{firstName.ToLower()}{lastName.ToLower()}@{domain}.com"
                });
            }

            NewReportsSubscription.Create(DocumentStore, name, companyId).GetAwaiter().GetResult();
        }

        session.SaveChanges();
    }

    public override void Down()
    {
        DocumentStore.Operations.Send(
            new Raven.Client.Documents.Operations.DeleteByQueryOperation(
                new Raven.Client.Documents.Queries.IndexQuery { Query = $"from {Company.Collection}" }));
    }
}
