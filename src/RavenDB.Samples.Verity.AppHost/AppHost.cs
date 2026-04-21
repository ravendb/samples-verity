using CommunityToolkit.Aspire.Hosting.RavenDB;
using Microsoft.Extensions.Configuration;
using Projects;
using System.Text.Json;

var builder = DistributedApplication.CreateBuilder(args);

// Storage
var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queues = storage.AddQueues("queues");

// RavenDB
// https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/ravendb?tabs=dotnet-cli#hosting-integration
var license = JsonSerializer.Serialize(
    builder.Configuration.GetSection("RavenDB:License").Get<RavenLicense>());

var settings = RavenDBServerSettings.Unsecured();
if (!string.IsNullOrEmpty(license))
{
    settings.WithLicense(license);
}

settings.Port = 9534;
settings.TcpPort = 41350;

var ravenDbServer = builder
    .AddRavenDB("RavenDB", settings)
    .WithImage("ravendb/ravendb", "7.2-latest")
    .WithIconName("Database")
    .WithReference(queues)
    .WaitFor(queues);

const string dbName = "verity";

var db = ravenDbServer
    .AddDatabase(dbName);

// Secrets — read from user-secrets when available, fall back to dev defaults
var internalJwtKeyValue   = builder.Configuration["InternalJwtKey"]   ?? "dev-verity-internal-jwt-signing-key!!";
var oidcClientSecretValue = builder.Configuration["OidcClientSecret"] ?? "dev-verity-oidc-secret";

// Identity host
var identity = builder
    .AddProject<RavenDB_Samples_Verity_Identity>("identity")
    .WithReference(db)
    .WaitFor(db)
    .WithHttpsEndpoint()
    .WithExternalHttpEndpoints();

// Functions App
var functions = builder.AddAzureFunctionsProject<RavenDB_Samples_Verity_App>("app")
    .WithHostStorage(storage)

    .WithReference(queues)
    .WaitFor(queues)

    .WithEnvironment("BindingConnection", queues.Resource.ConnectionStringExpression)

    .WithReference(db)
    .WaitFor(db)

    .WithEnvironment("SAMPLES_VERITY_SEC_EDGAR_USER_AGENT", builder.Configuration["SecEdgar:UserAgent"])
    .WithEnvironment("SAMPLES_VERITY_OPENAI_API_KEY", builder.Configuration["OpenAI:ApiKey"])

    // Azure Storage – Remote Attachments
    .WithEnvironment("SAMPLES_VERITY_AZURE_STORAGE_IDENTITY", builder.Configuration["AzureStorage:Identity"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_STORAGE_CONTAINER", builder.Configuration["AzureStorage:StorageContainer"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_ACCOUNT_NAME", builder.Configuration["AzureStorage:AccountName"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_ACCOUNT_KEY", builder.Configuration["AzureStorage:AccountKey"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_REMOTE_FOLDER_NAME", builder.Configuration["AzureStorage:RemoteFolderName"]) // Optional

    .WithEnvironment("CommandKey", builder.Configuration["CommandKey"])

    // Identity / JWT parameters for migration 004
    .WithEnvironment("InternalJwtKey", internalJwtKeyValue)
    .WithEnvironment("IdentityAdmin__Email", builder.Configuration["IdentityAdmin:Email"] ?? "admin@verity.local")
    .WithEnvironment("IdentityAdmin__Password", builder.Configuration["IdentityAdmin:Password"] ?? "admin")
    .WithEnvironment("OidcClientSecret", oidcClientSecretValue)

    .WithHttpCommand(
        path: "/api/migrate",
        displayName: "Migrate DB",
        commandOptions: new HttpCommandOptions
        {
            Description = "Runs database migrations",
            PrepareRequest = context =>
            {
                context.Request.Headers.Add("X-Command-Key", builder.Configuration["CommandKey"]);
                return Task.CompletedTask;
            },
            IconName = "databaseArrowUp",
            IsHighlighted = true
        });

// Frontend (Vite dev server — no longer externally exposed; BFF is the public entry)
var frontend = builder.AddNpmApp("Frontend", "../RavenDB.Samples.Verity.Frontend", "dev")
    .WithReference(functions)
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .PublishAsDockerFile()
    .WaitFor(functions);

// BFF — the single public entry point
builder
    .AddProject<RavenDB_Samples_Verity_Bff>("bff")
    .WithReference(functions)
    .WaitFor(functions)
    .WithReference(identity)
    .WaitFor(identity)
    .WithEnvironment("Frontend__Url", frontend.GetEndpoint("http"))
    .WithEnvironment("Api__Url", functions.GetEndpoint("http"))
    .WithEnvironment("Authority", identity.GetEndpoint("https"))
    .WithEnvironment("InternalJwtKey", internalJwtKeyValue)
    .WithEnvironment("OidcClientSecret", oidcClientSecretValue)
    // Override YARP cluster addresses from Aspire-injected URLs
    .WithEnvironment("ReverseProxy__Clusters__api-cluster__Destinations__primary__Address", functions.GetEndpoint("http"))
    .WithEnvironment("ReverseProxy__Clusters__frontend-cluster__Destinations__primary__Address", frontend.GetEndpoint("http"))
    .WithHttpsEndpoint()
    .WithExternalHttpEndpoints();

builder.Build().Run();

record RavenLicense(string Id, string Name, string[] Keys);
