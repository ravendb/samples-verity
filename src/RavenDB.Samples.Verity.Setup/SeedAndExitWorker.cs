using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Migrations;

namespace RavenDB.Samples.Verity.Setup;

internal sealed class SeedAndExitWorker(
    MigrationRunner migrations,
    IHostApplicationLifetime lifetime,
    ILogger<SeedAndExitWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Run(migrations.Run, stoppingToken);
            logger.LogInformation("Seed complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seed failed.");
            Environment.Exit(1);
            return;
        }
        lifetime.StopApplication();
    }
}
