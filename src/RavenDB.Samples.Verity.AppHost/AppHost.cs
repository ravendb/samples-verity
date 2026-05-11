using CommunityToolkit.Aspire.Hosting.RavenDB;
using Projects;
using RavenDB.Samples.Verity.AppHost;

// Env var names must match Constants.EnvVars in RavenDB.Samples.Verity.Setup
const string envOpenAiApiKey                = "SAMPLES_VERITY_OPENAI_API_KEY";
const string envSecEdgarUserAgent           = "SAMPLES_VERITY_SEC_EDGAR_USER_AGENT";
const string envAzureStorageConnectionString = "SAMPLES_VERITY_AZURE_STORAGE_CONNECTION_STRING";
const string envSinkServerUrl               = "SAMPLES_VERITY_SINK_SERVER_URL";
const string envHubServerInternalUrl        = "SAMPLES_VERITY_HUB_SERVER_INTERNAL_URL";

var builder = DistributedApplication.CreateBuilder(args);

// Storage
var storage = builder.AddAzureStorage("storage").RunAsEmulator();

// Parameters
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

var db   = ravenDbServer.AddDatabase("Verity");
var sink = ravenDbServer.AddDatabase("Verity-sink");

var ravenHttp = ravenDbServer.GetEndpoint("http");

// Seeder — runs all migrations then exits 0; gates the backend and subscriptions TUI
var seeder = builder.AddProject<RavenDB_Samples_Verity_Setup>("seeder")
    .WithReference(db).WaitFor(db)
    .WithReference(sink).WaitFor(sink)
    .WithEnvironment(envSecEdgarUserAgent, secEdgarUserAgent)
    .WithEnvironment(envOpenAiApiKey, openAiApiKey)
    .WithEnvironment(envAzureStorageConnectionString, azureStorageConnectionString)
    .WithEnvironment(envSinkServerUrl, ravenHttp)
    .WithEnvironment(
        envHubServerInternalUrl,
        ReferenceExpression.Create($"http://localhost:{ravenHttp.Property(EndpointProperty.TargetPort)}"));

// Verity App
var functions = builder.AddAzureFunctionsProject<RavenDB_Samples_Verity_App>("app")
    .WithHostStorage(storage)

    .WithEnvironment("BindingConnection", azureStorageConnectionString)

    .WithReference(db)
    .WaitFor(db)
    .WithReference(sink)
    .WaitFor(sink)

    .WaitForCompletion(seeder)

    .WithEnvironment(envSecEdgarUserAgent, secEdgarUserAgent)
    .WithEnvironment(envOpenAiApiKey, openAiApiKey)

    // Azure Storage – Remote Attachments & Queue ETL
    .WithEnvironment(envAzureStorageConnectionString, azureStorageConnectionString)

    // Hub/Sink Replication
    // SinkServerUrl: how the host-resident App reaches the RavenDB server.
    .WithEnvironment(envSinkServerUrl, ravenHttp)
    // HubServerInternalUrl: how the Sink (inside the RavenDB container) reaches the Hub.
    // Hub and Sink share the same RavenDB container, so the Hub is reachable at the
    // container's own loopback on the target port (8080 for unsecured RavenDB).
    .WithEnvironment(
        envHubServerInternalUrl,
        ReferenceExpression.Create($"http://localhost:{ravenHttp.Property(EndpointProperty.TargetPort)}"));

// DataSubscriptionsApp — interactive Spectre.Console TUI, must run in its own console window
// cmd /c start spawns a detached window so Aspire doesn't capture stdin/stdout
builder.AddExecutable("subscriptions", "cmd", "../RavenDB.Samples.Verity.DataSubscriptionsApp",
        "/c", "start", "Verity Subscriptions", "cmd", "/k", "dotnet run")
    .WithReference(sink)
    .WaitFor(sink)
    .WaitForCompletion(seeder);

// Frontend
builder.AddNpmApp("Frontend", "../RavenDB.Samples.Verity.Frontend", "dev")
    .WithReference(functions)
    .WithEnvironment("BROWSER", "none")
    .WithEnvironment("APP_HTTP", functions.GetEndpoint("http"))
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile()
    .WaitFor(functions);

builder.Build().Run();
