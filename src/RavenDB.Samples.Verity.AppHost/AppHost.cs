using CommunityToolkit.Aspire.Hosting.RavenDB;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Storage
var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queues = storage.AddQueues("queues");

// Parameters
var ravenDbLicense        = builder.AddParameter("ravendb-license", secret: true);
var databaseName          = builder.AddParameter("database-name");
var commandKey            = builder.AddParameter("command-key", secret: true);
var secEdgarUserAgent     = builder.AddParameter("sec-edgar-user-agent");
var openAiApiKey          = builder.AddParameter("openai-api-key", secret: true);
var azureStorageIdentity  = builder.AddParameter("azure-storage-identity");
var azureStorageContainer = builder.AddParameter("azure-storage-container");
var azureAccountName      = builder.AddParameter("azure-account-name");
var azureAccountKey       = builder.AddParameter("azure-account-key", secret: true);
var azureRemoteFolder     = builder.AddParameter("azure-remote-folder-name");
var azureQueueProtocol    = builder.AddParameter("azure-queue-protocol");
var azureQueueSuffix      = builder.AddParameter("azure-queue-endpoint-suffix");

// RavenDB
var settings = RavenDBServerSettings.Unsecured();
var licenseJson = builder.Configuration["Parameters:ravendb-license"];
if (!string.IsNullOrEmpty(licenseJson))
{
    settings.WithLicense(licenseJson);
}

settings.Port = 9534;
settings.TcpPort = 41350;
var hubInternalUrl = $"http://host.docker.internal:{settings.Port!.Value}";

var dbName   = builder.Configuration["Parameters:database-name"] ?? "verity";
var sinkName = $"{dbName}-sink";

var ravenDbServer = builder
    .AddRavenDB("RavenDB", settings)
    .WithImage("ravendb/ravendb", "7.2-latest")
    .WithIconName("Database")
    .WithReference(queues)
    .WaitFor(queues);

var db   = ravenDbServer.AddDatabase(dbName);
var sink = ravenDbServer.AddDatabase(sinkName);

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

    .WithEnvironment("SAMPLES_VERITY_DB_NAME", databaseName)

    .WithEnvironment("SAMPLES_VERITY_SEC_EDGAR_USER_AGENT", secEdgarUserAgent)
    .WithEnvironment("SAMPLES_VERITY_OPENAI_API_KEY", openAiApiKey)

    // Azure Storage – Remote Attachments
    .WithEnvironment("SAMPLES_VERITY_AZURE_STORAGE_IDENTITY",   azureStorageIdentity)
    .WithEnvironment("SAMPLES_VERITY_AZURE_STORAGE_CONTAINER",  azureStorageContainer)
    .WithEnvironment("SAMPLES_VERITY_AZURE_ACCOUNT_NAME",       azureAccountName)
    .WithEnvironment("SAMPLES_VERITY_AZURE_ACCOUNT_KEY",        azureAccountKey)
    .WithEnvironment("SAMPLES_VERITY_AZURE_REMOTE_FOLDER_NAME", azureRemoteFolder)

    // Azure Storage – Queue ETL + QueueTrigger
    .WithEnvironment("SAMPLES_VERITY_AZURE_QUEUE_DEFAULT_ENDPOINTS_PROTOCOL", azureQueueProtocol)
    .WithEnvironment("SAMPLES_VERITY_AZURE_QUEUE_ENDPOINT_SUFFIX",            azureQueueSuffix)
    .WithEnvironment("SAMPLES_VERITY_AZURE_QUEUE_CONNECTION",
        ReferenceExpression.Create(
            $"DefaultEndpointsProtocol={azureQueueProtocol.Resource};AccountName={azureAccountName.Resource};AccountKey={azureAccountKey.Resource};EndpointSuffix={azureQueueSuffix.Resource}"))

    // Hub/Sink Replication
    .WithEnvironment("SAMPLES_VERITY_SINK_SERVER_URL",         ravenDbServer.GetEndpoint("http"))
    .WithEnvironment("SAMPLES_VERITY_SINK_DATABASE_NAME",
        ReferenceExpression.Create($"{databaseName.Resource}-sink"))
    .WithEnvironment("SAMPLES_VERITY_HUB_SERVER_INTERNAL_URL", hubInternalUrl)

    .WithEnvironment("CommandKey", commandKey)
    .WithHttpCommand(
        path: "/api/migrate",
        displayName: "Migrate DB",
        commandOptions: new HttpCommandOptions
        {
            Description = "Runs database migrations",
            PrepareRequest = async context =>
            {
                var key = await commandKey.Resource.GetValueAsync(CancellationToken.None);
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
