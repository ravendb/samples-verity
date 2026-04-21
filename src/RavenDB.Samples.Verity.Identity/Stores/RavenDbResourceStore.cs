using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Raven.Client.Documents;
using RavenDB.Samples.Verity.Identity.Documents;

namespace RavenDB.Samples.Verity.Identity.Stores;

public class RavenDbResourceStore(IDocumentStore store) : IResourceStore
{
    public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        using var session = store.OpenAsyncSession();
        var names = scopeNames.ToHashSet();
        var docs = await session
            .Query<IdentityResourceDoc>()
            .ToListAsync();
        return docs.Where(d => names.Contains(d.Resource.Name)).Select(d => d.Resource).ToList();
    }

    public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
    {
        using var session = store.OpenAsyncSession();
        var names = scopeNames.ToHashSet();
        var docs = await session
            .Query<ApiScopeDoc>()
            .ToListAsync();
        return docs.Where(d => names.Contains(d.Scope.Name)).Select(d => d.Scope).ToList();
    }

    public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        using var session = store.OpenAsyncSession();
        var names = scopeNames.ToHashSet();
        var docs = await session
            .Query<ApiResourceDoc>()
            .ToListAsync();
        return docs.Where(d => d.Resource.Scopes.Any(names.Contains)).Select(d => d.Resource).ToList();
    }

    public async Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
    {
        using var session = store.OpenAsyncSession();
        var names = apiResourceNames.ToHashSet();
        var docs = await session
            .Query<ApiResourceDoc>()
            .ToListAsync();
        return docs.Where(d => names.Contains(d.Resource.Name)).Select(d => d.Resource).ToList();
    }

    public async Task<Resources> GetAllResourcesAsync()
    {
        using var session = store.OpenAsyncSession();
        var identityDocs = await session.Query<IdentityResourceDoc>().ToListAsync();
        var scopeDocs = await session.Query<ApiScopeDoc>().ToListAsync();
        var apiDocs = await session.Query<ApiResourceDoc>().ToListAsync();

        return new Resources(
            identityDocs.Select(d => d.Resource),
            apiDocs.Select(d => d.Resource),
            scopeDocs.Select(d => d.Scope));
    }
}
