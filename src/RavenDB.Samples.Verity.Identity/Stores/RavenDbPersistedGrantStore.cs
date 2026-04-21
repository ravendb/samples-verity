using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using RavenDB.Samples.Verity.Identity.Documents;
using RavenDB.Samples.Verity.Identity.Indexes;

namespace RavenDB.Samples.Verity.Identity.Stores;

public class RavenDbPersistedGrantStore(IDocumentStore store) : IPersistedGrantStore
{
    public async Task StoreAsync(PersistedGrant grant)
    {
        using var session = store.OpenAsyncSession();
        var doc = PersistedGrantDoc.FromGrant(grant);
        await session.StoreAsync(doc);
        if (grant.Expiration.HasValue)
        {
            var metadata = session.Advanced.GetMetadataFor(doc);
            metadata[Raven.Client.Constants.Documents.Metadata.Expires] = grant.Expiration.Value.ToUniversalTime();
        }
        await session.SaveChangesAsync();
    }

    public async Task<PersistedGrant?> GetAsync(string key)
    {
        using var session = store.OpenAsyncSession();
        var doc = await session.LoadAsync<PersistedGrantDoc>(PersistedGrantDoc.DocId(key));
        return doc?.ToGrant();
    }

    public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
    {
        using var session = store.OpenAsyncSession();
        var query = ApplyFilter(session.Query<PersistedGrants_ByLookup.Result, PersistedGrants_ByLookup>(), filter);
        var docs = await query.OfType<PersistedGrantDoc>().ToListAsync();
        return docs.Select(d => d.ToGrant()).ToList();
    }

    public async Task RemoveAsync(string key)
    {
        using var session = store.OpenAsyncSession();
        session.Delete(PersistedGrantDoc.DocId(key));
        await session.SaveChangesAsync();
    }

    public async Task RemoveAllAsync(PersistedGrantFilter filter)
    {
        using var session = store.OpenAsyncSession();
        var query = ApplyFilter(session.Query<PersistedGrants_ByLookup.Result, PersistedGrants_ByLookup>(), filter);
        var docs = await query.OfType<PersistedGrantDoc>().ToListAsync();
        foreach (var d in docs)
            session.Delete(d.Id);
        await session.SaveChangesAsync();
    }

    private static IRavenQueryable<PersistedGrants_ByLookup.Result> ApplyFilter(
        IRavenQueryable<PersistedGrants_ByLookup.Result> query,
        PersistedGrantFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.ClientId))
            query = query.Where(d => d.ClientId == filter.ClientId);
        if (filter.ClientIds is { } clientIds && clientIds.Any())
            query = query.Where(d => d.ClientId!.In(clientIds));
        if (!string.IsNullOrWhiteSpace(filter.SubjectId))
            query = query.Where(d => d.SubjectId == filter.SubjectId);
        if (!string.IsNullOrWhiteSpace(filter.SessionId))
            query = query.Where(d => d.SessionId == filter.SessionId);
        if (!string.IsNullOrWhiteSpace(filter.Type))
            query = query.Where(d => d.Type == filter.Type);
        if (filter.Types is { } types && types.Any())
            query = query.Where(d => d.Type!.In(types));
        return query;
    }
}
