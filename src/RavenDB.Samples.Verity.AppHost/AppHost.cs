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

// Library App
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

// Frontend
var frontend = builder.AddNpmApp("Frontend", "../RavenDB.Samples.Verity.Frontend", "dev")
    .WithReference(functions)
    .WithEnvironment("BROWSER", "none")
    .WithEnvironment("APP_HTTP", functions.GetEndpoint("http"))
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile()
    .WaitFor(functions);

builder.Build().Run();

record RavenLicense(string Id, string Name, string[] Keys);
