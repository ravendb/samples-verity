using CommunityToolkit.Aspire.Hosting.RavenDB;
using Projects;
using RavenDB.Samples.Verity.AppHost;
using RavenDB.Samples.Verity.Setup;

var builder = DistributedApplication.CreateBuilder(args);

// Storage
var storage = builder.AddAzureStorage("storage").RunAsEmulator();

// Parameters
var secretKey = builder.AddParameterWithRandomValue(Constants.EnvVars.CommandKey, secret: true);

var ravenDbLicense = builder
    .AddParameter("ravendb-license", secret: true)
    .WithDescription("Your Developer license formatted as JSON.");

var secEdgarUserAgent = builder
    .AddParameter("sec-edgar-user-agent")
    .WithDescription("The email that will be used to obtain data from SEC Edgar database.");

var openAiApiKey = builder.AddParameter("openai-api-key", secret: true)
    .WithDescription("OpenAI API key");

var azureStorageConnectionString = builder
    .AddParameter("azure-storage-connection-string")
    .WithDescription("Azure Storage Connection string. Used both, for the Remote Attachments and the Azure Storage Queues ETL");

// RavenDB
var settings = RavenDBServerSettings.Unsecured();
settings.Port = 9534;
settings.TcpPort = 41350;

var ravenDbServer = builder
    .AddRavenDB("RavenDB", settings)
    .WithImage("ravendb/ravendb", "7.2-latest")
    .WithIconName("Database")
    .WithEnvironment("RAVEN_License_Eula_Accepted", "true")
    .WithEnvironment("RAVEN_License", ravenDbLicense);

var db   = ravenDbServer.AddDatabase(Constants.DatabaseName);
var sink = ravenDbServer.AddDatabase(Constants.DatabaseSinkName);

var hubInternalUrl = $"http://host.docker.internal:{settings.Port!.Value}";

// Verity App
var functions = builder.AddAzureFunctionsProject<RavenDB_Samples_Verity_App>("app")
    .WithHostStorage(storage)

    .WithEnvironment("BindingConnection", azureStorageConnectionString)

    .WithReference(db)
    .WaitFor(db)
    .WithReference(sink)
    .WaitFor(sink)

    .WithEnvironment(Constants.EnvVars.SecEdgarUserAgent, secEdgarUserAgent)
    .WithEnvironment(Constants.EnvVars.OpenAiApiKey, openAiApiKey)

    // Azure Storage – Remote Attachments & Queue ETL
    .WithEnvironment(Constants.EnvVars.AzureStorageConnectionString, azureStorageConnectionString)

    // Hub/Sink Replication
    .WithEnvironment(Constants.EnvVars.SinkServerUrl,        ravenDbServer.GetEndpoint("http"))
    .WithEnvironment(Constants.EnvVars.HubServerInternalUrl, hubInternalUrl)

    .WithEnvironment(Constants.EnvVars.CommandKey, secretKey)
    .WithHttpCommand(
        path: "/api/migrate",
        displayName: "Migrate DB",
        commandOptions: new HttpCommandOptions
        {
            Description = "Runs database migrations",
            PrepareRequest = async context =>
            {
                var key = await secretKey.Resource.GetValueAsync(CancellationToken.None);
                context.Request.Headers.Add(Constants.HttpHeaders.CommandKey, key);
            },
            IconName = "databaseArrowUp",
            IsHighlighted = true
        });

// DataSubscriptionsApp — interactive Spectre.Console TUI, must run in its own console window
// cmd /c start spawns a detached window so Aspire doesn't capture stdin/stdout
builder.AddExecutable("subscriptions", "cmd", "../RavenDB.Samples.Verity.DataSubscriptionsApp",
        "/c", "start", "Verity Subscriptions", "cmd", "/k", "dotnet run")
    .WithReference(sink)
    .WaitFor(sink);

// Frontend
var frontend = builder.AddNpmApp("Frontend", "../RavenDB.Samples.Verity.Frontend", "dev")
    .WithReference(functions)
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .PublishAsDockerFile();

// IdentityServer — local Duende IdentityServer for development
var identity = builder.AddProject<RavenDB_Samples_Verity_IdentityServer>("identity")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);

// BFF — single entry point for the browser; proxies API (with tokens) + frontend
var bff = builder.AddProject<RavenDB_Samples_Verity_Bff>("bff")
    .WithReference(functions)
    .WaitFor(functions)
    .WithReference(frontend)
    .WaitFor(frontend)
    .WithReference(identity)
    .WaitFor(identity)
    // Pass actual URLs so OIDC issuer validation works correctly.
    .WithEnvironment("Identity__Url",  identity.GetEndpoint("http"))
    .WithEnvironment("Api__Url",       functions.GetEndpoint("http"))
    .WithEnvironment("Frontend__Url",  frontend.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

// Tell IdentityServer the BFF's base URL for redirect URI registration.
// Must be the external HTTPS endpoint — the browser sends redirect_uri based on
// the public URL it sees, so IdentityServer must register the same HTTPS address.
identity.WithEnvironment("Bff__BaseUrl", bff.GetEndpoint("https"));

builder.Build().Run();
