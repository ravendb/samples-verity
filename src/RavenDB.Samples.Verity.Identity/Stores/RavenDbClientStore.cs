using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Raven.Client.Documents;
using RavenDB.Samples.Verity.Identity.Documents;

namespace RavenDB.Samples.Verity.Identity.Stores;

public class RavenDbClientStore(IDocumentStore store) : IClientStore
{
    public async Task<Client?> FindClientByIdAsync(string clientId)
    {
        using var session = store.OpenAsyncSession();
        var doc = await session.LoadAsync<ClientDoc>(ClientDoc.DocId(clientId));
        return doc?.Client;
    }
}
