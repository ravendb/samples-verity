using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RavenDB.Samples.Verity.App;
using RavenDB.Samples.Verity.App.Application.Usage;
using RavenDB.Samples.Verity.App.Infrastructure.Middleware;
using RavenDB.Samples.Verity.App.Infrastructure.RavenDB;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddDefaultHealthChecks();

builder.AddRavenDBClient("verity", settings =>
{
    settings.CreateDatabase = true;
});

builder.Services.AddHostedService<RavenInitializer>();

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

builder.Services.AddSingleton<SessionApiUsageLimiter>();
builder.Services.AddSingleton<GlobalApiUsageLimiter>();

builder.Services.AddSingleton<SessionApiUsageTracker>();
builder.Services.AddSingleton<GlobalApiUsageTracker>();

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