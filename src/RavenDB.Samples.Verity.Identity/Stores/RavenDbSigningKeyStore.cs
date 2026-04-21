using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Raven.Client.Documents;
using RavenDB.Samples.Verity.Identity.Documents;

namespace RavenDB.Samples.Verity.Identity.Stores;

public class RavenDbSigningKeyStore(IDocumentStore store) : ISigningKeyStore
{
    public async Task<IEnumerable<SerializedKey>> LoadKeysAsync()
    {
        using var session = store.OpenAsyncSession();
        var docs = await session.Query<SigningKeyDoc>().ToListAsync();
        return docs.Select(d => d.ToKey()).ToList();
    }

    public async Task StoreKeyAsync(SerializedKey key)
    {
        using var session = store.OpenAsyncSession();
        var doc = SigningKeyDoc.FromKey(key);
        await session.StoreAsync(doc);
        await session.SaveChangesAsync();
    }

    public async Task DeleteKeyAsync(string id)
    {
        using var session = store.OpenAsyncSession();
        session.Delete(SigningKeyDoc.DocId(id));
        await session.SaveChangesAsync();
    }
}
