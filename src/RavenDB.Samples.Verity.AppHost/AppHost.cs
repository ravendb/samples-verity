using CommunityToolkit.Aspire.Hosting.RavenDB;
using Projects;
using RavenDB.Samples.Verity.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Storage
var storage = builder.AddAzureStorage("storage").RunAsEmulator();

// Parameters
const string commandKey = "CommandKey";
var secretKey = builder.AddParameterWithRandomValue(commandKey, secret: true);

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

var hubInternalUrl = $"http://host.docker.internal:{settings.Port!.Value}";

// Verity App
var functions = builder.AddAzureFunctionsProject<RavenDB_Samples_Verity_App>("app")
    .WithHostStorage(storage)

    .WithEnvironment("BindingConnection", azureStorageConnectionString)

    .WithReference(db)
    .WaitFor(db)
    .WithReference(sink)
    .WaitFor(sink)

    .WithEnvironment("SAMPLES_VERITY_SEC_EDGAR_USER_AGENT", secEdgarUserAgent)
    .WithEnvironment("SAMPLES_VERITY_OPENAI_API_KEY", openAiApiKey)

    // Azure Storage – Remote Attachments & Queue ETL
    .WithEnvironment("SAMPLES_VERITY_AZURE_STORAGE_CONNECTION_STRING",  azureStorageConnectionString)

    // Hub/Sink Replication
    .WithEnvironment("SAMPLES_VERITY_SINK_SERVER_URL",         ravenDbServer.GetEndpoint("http"))
    .WithEnvironment("SAMPLES_VERITY_HUB_SERVER_INTERNAL_URL", hubInternalUrl)

    .WithEnvironment("CommandKey", secretKey)
    .WithHttpCommand(
        path: "/api/migrate",
        displayName: "Migrate DB",
        commandOptions: new HttpCommandOptions
        {
            Description = "Runs database migrations",
            PrepareRequest = async context =>
            {
                var key = await secretKey.Resource.GetValueAsync(CancellationToken.None);
                context.Request.Headers.Add("X-Command-Key", key);
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
builder.AddNpmApp("Frontend", "../RavenDB.Samples.Verity.Frontend", "dev")
    .WithReference(functions)
    .WithEnvironment("BROWSER", "none")
    .WithEnvironment("APP_HTTP", functions.GetEndpoint("http"))
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile()
    .WaitFor(functions);

builder.Build().Run();
