using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Migrations;
using RavenDB.Samples.Verity.App.Infrastructure;
using RavenDB.Samples.Verity.Setup.Migrations;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddDefaultHealthChecks();

builder.AddRavenDBClient(RavenDB.Samples.Verity.Setup.Constants.DatabaseName, settings =>
{
    settings.CreateDatabase = true;
});

builder.Services.AddHostedService<MigrationStartup>();

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.ConfigureFunctionsWebApplication();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<SecEdgarApi>(client =>
{
    var userAgent = Environment.GetEnvironmentVariable(Constants.EnvVars.SecEdgarUserAgent)
                    ?? throw new InvalidOperationException($"No environment variable '{Constants.EnvVars.SecEdgarUserAgent}' found.");
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
});

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();

internal sealed class MigrationStartup(MigrationRunner migrations) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) { migrations.Run(); return Task.CompletedTask; }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
