using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Subscriptions;

namespace RavenDB.Samples.Verity.Model.Subscriptions;

public record ReportNotification(string CompanyName, string AccessionNumber, string Filing);

public static class NewReportsSubscription
{
    public static string BuildName(string companyName) => $"New-Reports-{companyName}";

    public static async Task Create(IDocumentStore store, string companyName, string companyId)
    {
        await store.Subscriptions.CreateAsync(new SubscriptionCreationOptions<Report>
        {
            Name = BuildName(companyName),
            Filter = x => x.CompanyId == companyId && x.Summary != null,
            Projection = x => new
            {
                CompanyName = x.CompanyId.Split('/').Last(),
                AccessionNumber = x.AccessionNumber,
                Filing = $"{x.FormType} for period ended {x.ReportDate}"
            }
        });
    }
}