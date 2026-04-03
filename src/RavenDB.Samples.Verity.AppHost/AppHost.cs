using CommunityToolkit.Aspire.Hosting.RavenDB;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Storage
var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var queues = storage.AddQueues("queues");

// RavenDB
// https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/ravendb?tabs=dotnet-cli#hosting-integration
var license = Environment.GetEnvironmentVariable("RAVEN_LICENSE");


var settings = RavenDBServerSettings.Unsecured();
if (license != null)
{
    // Use the license from the hardcoded JSON string
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

    .WithEnvironment("SAMPLES_VERITY_OPENAI_API_KEY", builder.Configuration["OpenAI:ApiKey"])
    .WithEnvironment("SAMPLES_VERITY_SEC_EDGAR_USER_AGENT", builder.Configuration["SecEdgar:UserAgent"]);

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
