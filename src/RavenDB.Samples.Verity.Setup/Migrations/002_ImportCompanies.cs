using Raven.Migrations;
using RavenDB.Samples.Verity.Model;

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

    public override void Up()
    {
        if (Ciks.Length == 0)
            return;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", context.SecEdgarUserAgent);

        RunAsync(http).GetAwaiter().GetResult();
    }

    private async Task RunAsync(HttpClient http)
    {
        var paddedCiks = Ciks.Select(SecEdgar.NormalizeCik).ToArray();

        var fetchTasks = paddedCiks.Select(c => SecEdgarCompanyImporter.FetchCompanyDataAsync(http, c));
        var fetched    = await Task.WhenAll(fetchTasks);

        var rng = new Random(21);
        using var session = DocumentStore.OpenAsyncSession();
        foreach (var data in fetched)
            await SecEdgarCompanyImporter.StoreCompanyAsync(session, DocumentStore, data);
        await session.SaveChangesAsync();
    }

    public override void Down()
    {
        DocumentStore.Operations.Send(
            new Raven.Client.Documents.Operations.DeleteByQueryOperation(
                new Raven.Client.Documents.Queries.IndexQuery { Query = $"from {Company.Collection}" }));
    }
}
