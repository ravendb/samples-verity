using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Migrations;
using RavenDB.Samples.Verity.Setup;
using RavenDB.Samples.Verity.Setup.Migrations;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddRavenDBClient(Constants.DatabaseName, settings =>
{
    settings.CreateDatabase = true;
});

builder.Services.AddSingleton(new MigrationContext(
    Environment.GetEnvironmentVariable(Constants.EnvVars.OpenAiApiKey) ?? "",
    Environment.GetEnvironmentVariable(Constants.EnvVars.AzureStorageConnectionString) ?? "",
    Environment.GetEnvironmentVariable(Constants.EnvVars.SecEdgarUserAgent) ?? "",
    Environment.GetEnvironmentVariable(Constants.EnvVars.SinkServerUrl) ?? "",
    Environment.GetEnvironmentVariable(Constants.EnvVars.HubServerInternalUrl) ?? ""
));

builder.Services.AddRavenDbMigrations(migrations =>
{
    migrations.Assemblies = [typeof(MigrationContext).Assembly];
});

builder.Services.AddHostedService<SeedAndExitWorker>();

await builder.Build().RunAsync();
