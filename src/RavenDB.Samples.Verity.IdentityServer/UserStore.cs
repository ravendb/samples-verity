using Duende.IdentityModel;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using RavenDB.Samples.Verity.Model;
using System.Security.Claims;

namespace RavenDB.Samples.Verity.IdentityServer;

public sealed class UserStore(IDocumentStore store)
{
    private readonly PasswordHasher<User> _hasher = new();

    // ── Lookup ───────────────────────────────────────────────────────────────

    public async Task<User?> FindByUsernameAsync(string username)
    {
        using var session = store.OpenAsyncSession();
        return await session.Query<User>()
            .Where(u => u.Username == username.ToLowerInvariant())
            .FirstOrDefaultAsync();
    }

    // ── Credential validation ────────────────────────────────────────────────

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await FindByUsernameAsync(username);
        if (user is null) return false;
        return _hasher.VerifyHashedPassword(user, user.PasswordHash, password)
            != PasswordVerificationResult.Failed;
    }

    // ── Registration ─────────────────────────────────────────────────────────

    public async Task<(bool Success, string Error)> RegisterAsync(
        string username, string password, string displayName, string email,
        UserRole role = UserRole.User, string? companyId = null)
    {
        var existing = await FindByUsernameAsync(username);
        if (existing is not null)
            return (false, "Username already taken.");

        var subjectId = Guid.NewGuid().ToString();
        var parts     = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        var user = new User
        {
            Id           = $"users/{subjectId}",
            SubjectId    = subjectId,
            Username     = username.ToLowerInvariant(),
            Name         = parts.ElementAtOrDefault(0) ?? username,
            Surname      = parts.ElementAtOrDefault(1) ?? string.Empty,
            Email        = email,
            Role         = role,
            CompanyId    = companyId,
        };

        user.PasswordHash = _hasher.HashPassword(user, password);

        using var session = store.OpenAsyncSession();
        await session.StoreAsync(user);
        await session.SaveChangesAsync();
        return (true, string.Empty);
    }

    // ── Claims ───────────────────────────────────────────────────────────────

    public IEnumerable<Claim> GetClaims(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Name,              user.Username),
            new(JwtClaimTypes.PreferredUserName, user.Username),
            new(JwtClaimTypes.Email,             user.Email),
            new(JwtClaimTypes.GivenName,         user.Name),
            new(JwtClaimTypes.FamilyName,        user.Surname),
            new(JwtClaimTypes.Role,              user.Role.ToString()),
        };

        if (user.CompanyId is not null)
            claims.Add(new Claim("company_id", user.CompanyId));

        return claims;
    }
}
