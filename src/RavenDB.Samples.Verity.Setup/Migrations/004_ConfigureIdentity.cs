using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents.Operations.Revisions;
using Raven.Client.Documents.Operations.TimeSeries;
using Raven.Migrations;
using System.Security.Claims;

namespace RavenDB.Samples.Verity.Setup.Migrations;

[Migration(4)]
public sealed class ConfigureIdentity(MigrationContext context) : Migration
{
    public override void Up()
    {
        ConfigureRevisions();
        ConfigureTimeSeries();
        SeedIdentityResources();
        SeedApiResources();
        SeedClient();
        SeedAdminUser();
        SeedDemoUser();
    }

    private void ConfigureRevisions()
    {
        DocumentStore.Maintenance.Send(new ConfigureRevisionsOperation(new RevisionsConfiguration
        {
            Collections = new Dictionary<string, RevisionsCollectionConfiguration>
            {
                {
                    "IdentityClients", new RevisionsCollectionConfiguration
                    {
                        Disabled = false,
                        PurgeOnDelete = false,
                        MaximumRevisionsToDeleteUponDocumentUpdate = 50
                    }
                },
                {
                    "IdentityEvents", new RevisionsCollectionConfiguration
                    {
                        Disabled = false,
                        PurgeOnDelete = false
                    }
                },
                {
                    "SigningKeys", new RevisionsCollectionConfiguration
                    {
                        Disabled = false,
                        PurgeOnDelete = false,
                        MaximumRevisionsToDeleteUponDocumentUpdate = 10
                    }
                }
            }
        }));
    }

    private void ConfigureTimeSeries()
    {
        DocumentStore.Maintenance.Send(new ConfigureTimeSeriesOperation(new TimeSeriesConfiguration
        {
            Collections =
            {
                { "IdentityEventDaily", new TimeSeriesCollectionConfiguration() }
            }
        }));
    }

    private void SeedIdentityResources()
    {
        using var session = DocumentStore.OpenSession();

        void Upsert(string id, object doc)
        {
            if (session.Advanced.Exists(id)) return;
            session.Store(doc, id);
            session.Advanced.GetMetadataFor(doc)["@collection"] = "IdentityResources";
        }

        Upsert("IdentityResources/openid", new { Resource = new IdentityResources.OpenId() });
        Upsert("IdentityResources/profile", new { Resource = new IdentityResources.Profile() });

        session.SaveChanges();
    }

    private void SeedApiResources()
    {
        using var session = DocumentStore.OpenSession();

        void UpsertScope(string id, ApiScope scope)
        {
            if (session.Advanced.Exists(id)) return;
            var doc = new { Scope = scope };
            session.Store(doc, id);
            session.Advanced.GetMetadataFor(doc)["@collection"] = "ApiScopes";
        }

        void UpsertResource(string id, ApiResource resource)
        {
            if (session.Advanced.Exists(id)) return;
            var doc = new { Resource = resource };
            session.Store(doc, id);
            session.Advanced.GetMetadataFor(doc)["@collection"] = "ApiResources";
        }

        UpsertScope("ApiScopes/verity.read", new ApiScope("verity.read", "Read access to Verity data"));
        UpsertScope("ApiScopes/verity.audit.write", new ApiScope("verity.audit.write", "Write access to Verity audits"));

        UpsertResource("ApiResources/verity-api", new ApiResource("verity-api", "Verity API")
        {
            Scopes = { "verity.read", "verity.audit.write" },
            RequireResourceIndicator = true
        });

        session.SaveChanges();
    }

    private void SeedClient()
    {
        using var session = DocumentStore.OpenSession();

        const string clientDocId = "IdentityClients/verity-bff";
        if (session.Advanced.Exists(clientDocId)) return;

        var client = new Client
        {
            ClientId = "verity-bff",
            ClientName = "Verity BFF",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = true,
            ClientSecrets = { new Secret(context.OidcClientSecret.Sha256()) },
            AllowedScopes = { "openid", "profile", "verity.read", "verity.audit.write", OidcConstants.StandardScopes.OfflineAccess },
            AllowOfflineAccess = true,
            RedirectUris = { "https://localhost:7443/signin-oidc" },
            PostLogoutRedirectUris = { "https://localhost:7443/signout-callback-oidc" },
            FrontChannelLogoutUri = "https://localhost:7443/signout-oidc",
            AccessTokenLifetime = 300,
            AuthorizationCodeLifetime = 60,
            RefreshTokenUsage = TokenUsage.OneTimeOnly,
            RefreshTokenExpiration = TokenExpiration.Sliding,
            SlidingRefreshTokenLifetime = 86400
        };

        var doc = new { Client = client };
        session.Store(doc, clientDocId);
        session.Advanced.GetMetadataFor(doc)["@collection"] = "IdentityClients";
        session.SaveChanges();
    }

    private void SeedAdminUser()
    {
        using var session = DocumentStore.OpenSession();

        const string adminDocId = "IdentityUsers/admin";
        if (session.Advanced.Exists(adminDocId)) return;

        var hasher = new PasswordHasher<object>();
        var email    = string.IsNullOrWhiteSpace(context.IdentityAdminEmail)    ? "admin@verity.local" : context.IdentityAdminEmail;
        var password = string.IsNullOrWhiteSpace(context.IdentityAdminPassword) ? "admin"              : context.IdentityAdminPassword;

        var doc = new
        {
            SubjectId = "admin",
            Username = email,
            Email = email,
            DisplayName = "Verity Admin",
            PasswordHash = hasher.HashPassword(new object(), password),
            IsActive = true,
            Claims = new[]
            {
                new { Type = JwtClaimTypes.Role, Value = "admin" }
            }
        };

        session.Store(doc, adminDocId);
        session.Advanced.GetMetadataFor(doc)["@collection"] = "IdentityUsers";
        session.SaveChanges();
    }

    private void SeedDemoUser()
    {
        using var session = DocumentStore.OpenSession();

        const string docId = "IdentityUsers/auditor";
        if (session.Advanced.Exists(docId)) return;

        var hasher = new PasswordHasher<object>();
        var doc = new
        {
            SubjectId = "auditor",
            Username = "auditor@verity.local",
            Email = "auditor@verity.local",
            DisplayName = "Demo Auditor",
            PasswordHash = hasher.HashPassword(new object(), "auditor"),
            IsActive = true,
            Claims = Array.Empty<object>()
        };

        session.Store(doc, docId);
        session.Advanced.GetMetadataFor(doc)["@collection"] = "IdentityUsers";
        session.SaveChanges();
    }

    public override void Down()
    {
        throw new NotSupportedException(
            "Rolling back identity configuration is not supported. Remove clients, resources, and users via RavenDB Studio.");
    }
}

