using Duende.IdentityModel;
using Duende.IdentityServer.Models;

namespace RavenDB.Samples.Verity.IdentityServer;

public static class IdentityConfig
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile
        {
            // role and company_id must be in an identity scope so that /bff/user returns them.
            UserClaims = [..new IdentityResources.Profile().UserClaims, JwtClaimTypes.Role, "company_id"],
        },
        new IdentityResources.Email(),
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("verity-api", "Verity API")
        {
            // Include role and company_id in every access token issued for this scope.
            UserClaims = [JwtClaimTypes.Role, "company_id"],
        },
    ];

    public static IEnumerable<Client> GetClients(string bffBaseUrl) =>
    [
        new Client
        {
            ClientId      = "verity-bff",
            ClientSecrets = { new Secret("bff-secret".Sha256()) },

            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce       = true,
            RequireConsent    = false,

            RedirectUris           = { $"{bffBaseUrl}/signin-oidc" },
            FrontChannelLogoutUri  = $"{bffBaseUrl}/signout-oidc",
            PostLogoutRedirectUris = { $"{bffBaseUrl}/signout-callback-oidc" },

            AllowOfflineAccess = true,
            AllowedScopes      = { "openid", "profile", "email", "verity-api", "offline_access" },

            // Put all user claims directly in the ID token so the BFF session
            // always has them — avoids relying on the UserInfo roundtrip.
            AlwaysIncludeUserClaimsInIdToken = true,

            AccessTokenLifetime = 3600,
            RefreshTokenUsage   = TokenUsage.ReUse,
        },
    ];
}
