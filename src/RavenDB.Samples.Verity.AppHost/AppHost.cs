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
var hubInternalUrl = $"http://host.docker.internal:{settings.Port!.Value}";

var ravenDbServer = builder
    .AddRavenDB("RavenDB", settings)
    .WithImage("ravendb/ravendb", "7.2-latest")
    .WithIconName("Database")
    .WithReference(queues)
    .WaitFor(queues);

var dbName   = builder.Configuration["DatabaseName"]!;
var sinkName = $"{dbName}-sink";

var db = ravenDbServer
    .AddDatabase(dbName);

var sink = ravenDbServer
    .AddDatabase(sinkName);


// Verity App
var functions = builder.AddAzureFunctionsProject<RavenDB_Samples_Verity_App>("app")
    .WithHostStorage(storage)

    .WithReference(queues)
    .WaitFor(queues)

    .WithEnvironment("BindingConnection", queues.Resource.ConnectionStringExpression)

    .WithReference(db)
    .WaitFor(db)
    .WithReference(sink)
    .WaitFor(sink)

    .WithEnvironment("SAMPLES_VERITY_DB_NAME", dbName)

    .WithEnvironment("SAMPLES_VERITY_SEC_EDGAR_USER_AGENT", builder.Configuration["SecEdgar:UserAgent"])
    .WithEnvironment("SAMPLES_VERITY_OPENAI_API_KEY", builder.Configuration["OpenAI:ApiKey"])

    // Azure Storage – Remote Attachments
    .WithEnvironment("SAMPLES_VERITY_AZURE_STORAGE_IDENTITY", builder.Configuration["AzureStorage:Identity"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_STORAGE_CONTAINER", builder.Configuration["AzureStorage:StorageContainer"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_ACCOUNT_NAME", builder.Configuration["AzureStorage:AccountName"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_ACCOUNT_KEY", builder.Configuration["AzureStorage:AccountKey"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_REMOTE_FOLDER_NAME", builder.Configuration["AzureStorage:RemoteFolderName"]) // Optional

    // Azure Storage – Queue ETL + QueueTrigger
    .WithEnvironment("SAMPLES_VERITY_AZURE_QUEUE_DEFAULT_ENDPOINTS_PROTOCOL", builder.Configuration["AzureStorage:QueueDefaultEndpointsProtocol"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_QUEUE_ENDPOINT_SUFFIX",            builder.Configuration["AzureStorage:QueueEndpointSuffix"])
    .WithEnvironment("SAMPLES_VERITY_AZURE_QUEUE_CONNECTION",
        $"DefaultEndpointsProtocol={builder.Configuration["AzureStorage:QueueDefaultEndpointsProtocol"]};" +
        $"AccountName={builder.Configuration["AzureStorage:AccountName"]};" +
        $"AccountKey={builder.Configuration["AzureStorage:AccountKey"]};" +
        $"EndpointSuffix={builder.Configuration["AzureStorage:QueueEndpointSuffix"]}")

    // Hub/Sink Replication
    .WithEnvironment("SAMPLES_VERITY_SINK_SERVER_URL",         ravenDbServer.GetEndpoint("http"))
    .WithEnvironment("SAMPLES_VERITY_SINK_DATABASE_NAME",      sinkName)
    .WithEnvironment("SAMPLES_VERITY_HUB_SERVER_INTERNAL_URL", hubInternalUrl)

    .WithEnvironment("CommandKey", builder.Configuration["CommandKey"])
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

// DataSubscriptionsApp — interactive Spectre.Console TUI, must run in its own console window
// cmd /c start spawns a detached window so Aspire doesn't capture stdin/stdout
builder.AddExecutable("subscriptions", "cmd", "../RavenDB.Samples.Verity.DataSubscriptionsApp",
        "/c", "start", "Verity Subscriptions", "cmd", "/k", "dotnet run")
    .WithReference(sink)
    .WaitFor(sink);

// Frontend (Vite dev server — internal; BFF is the external entry point)
var frontend = builder.AddNpmApp("Frontend", "../RavenDB.Samples.Verity.Frontend", "dev")
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .PublishAsDockerFile();

// IdentityServer — local Duende IdentityServer for development
var identity = builder.AddProject<RavenDB_Samples_Verity_IdentityServer>("identity")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("SAMPLES_VERITY_DB_NAME", dbName);

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

record RavenLicense(string Id, string Name, string[] Keys);
