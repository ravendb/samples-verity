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

            var companyId = $"Companies/{name}";

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

            NewReportsSubscription.Create(DocumentStore, name, companyId).GetAwaiter().GetResult();
        }

        session.SaveChanges();
    }

    public override void Down()
    {
        DocumentStore.Operations.Send(
            new Raven.Client.Documents.Operations.DeleteByQueryOperation(
                new Raven.Client.Documents.Queries.IndexQuery { Query = "from Companies" }));
    }
}
