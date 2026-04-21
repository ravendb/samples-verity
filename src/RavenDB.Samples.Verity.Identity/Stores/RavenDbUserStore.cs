using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using RavenDB.Samples.Verity.Identity.Documents;

namespace RavenDB.Samples.Verity.Identity.Stores;

public class RavenDbUserStore(IDocumentStore store, IPasswordHasher<IdentityUserDoc> hasher)
{
    public async Task<IdentityUserDoc?> FindByUsernameAsync(string username)
    {
        using var session = store.OpenAsyncSession();
        return await session.Query<IdentityUserDoc>()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<IdentityUserDoc?> FindBySubjectAsync(string sub)
    {
        using var session = store.OpenAsyncSession();
        return await session.LoadAsync<IdentityUserDoc>(IdentityUserDoc.DocId(sub));
    }

    public async Task<(bool Ok, IdentityUserDoc? User)> ValidateAsync(string username, string password)
    {
        var user = await FindByUsernameAsync(username);
        if (user is null) return (false, null);

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return (result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded, user);
    }
}
