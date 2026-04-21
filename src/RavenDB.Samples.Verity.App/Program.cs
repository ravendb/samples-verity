using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Raven.Migrations;
using RavenDB.Samples.Verity.App.Application.Usage;
using RavenDB.Samples.Verity.App.Infrastructure;
using RavenDB.Samples.Verity.Setup.Migrations;
using System.Text;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddDefaultHealthChecks();

builder.AddRavenDBClient("verity", settings =>
{
    settings.CreateDatabase = true;
});

builder.Services.AddHostedService<MigrationStartup>();

builder.Services.AddSingleton(new MigrationContext(
    openAiApiKey:                       Environment.GetEnvironmentVariable(Constants.EnvVars.OpenAiApiKey) ?? "",
    azureStorageIdentity:               Environment.GetEnvironmentVariable(Constants.EnvVars.AzureStorageIdentity) ?? "",
    azureStorageContainer:              Environment.GetEnvironmentVariable(Constants.EnvVars.AzureStorageContainer) ?? "",
    azureAccountName:                   Environment.GetEnvironmentVariable(Constants.EnvVars.AzureAccountName) ?? "",
    azureAccountKey:                    Environment.GetEnvironmentVariable(Constants.EnvVars.AzureAccountKey) ?? "",
    azureRemoteFolderName:              Environment.GetEnvironmentVariable(Constants.EnvVars.AzureRemoteFolderName) ?? "",
    azureStorageQueuesConnectionString: Environment.GetEnvironmentVariable("BindingConnection") ?? "",
    secEdgarUserAgent:                  Environment.GetEnvironmentVariable(Constants.EnvVars.SecEdgarUserAgent) ?? "",
    internalJwtKey:                     builder.Configuration["InternalJwtKey"] ?? "",
    identityAdminEmail:                 builder.Configuration["IdentityAdmin:Email"] ?? "",
    identityAdminPassword:              builder.Configuration["IdentityAdmin:Password"] ?? "",
    oidcClientSecret:                   builder.Configuration["OidcClientSecret"] ?? ""
));

builder.Services.AddRavenDbMigrations(migrations =>
{
    migrations.Assemblies = [typeof(MigrationContext).Assembly];
});

var internalJwtKey = builder.Configuration["InternalJwtKey"]
    ?? throw new InvalidOperationException("InternalJwtKey not configured");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "verity-bff",
            ValidateAudience = true,
            ValidAudience = "verity-api",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(internalJwtKey)),
            ValidTypes = ["at+jwt"],
            ClockSkew = TimeSpan.FromSeconds(10)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("verity.read", p => p.RequireClaim("scope", "verity.read"));
    options.AddPolicy("verity.audit.write", p => p.RequireClaim("scope", "verity.audit.write"));
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

internal sealed class MigrationStartup(MigrationRunner migrations) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) { migrations.Run(); return Task.CompletedTask; }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}