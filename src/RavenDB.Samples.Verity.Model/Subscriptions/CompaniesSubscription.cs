using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;

namespace RavenDB.Samples.Verity.Model.Subscriptions;

public static class CompaniesSubscription
{
    public const string Name = "Companies-Watch";

    public static async Task Create(IDocumentStore store)
    {
        await store.Subscriptions.CreateAsync(new SubscriptionCreationOptions<Company>
        {
            Name = Name
        });
    }
}
