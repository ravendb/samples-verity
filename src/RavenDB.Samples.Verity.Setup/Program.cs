using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Http;
using Raven.Migrations;
using RavenDB.Samples.Verity.Setup;
using RavenDB.Samples.Verity.Setup.Migrations;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

// Accept self-signed server certs (chain validation fails because our dev CA is not trusted).
// Name and revocation checks still apply — only chain-of-trust errors are forgiven.
RequestExecutor.RemoteCertificateValidationCallback +=
    (_, _, _, errors) => (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None;

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
    Environment.GetEnvironmentVariable(Constants.EnvVars.HubServerInternalUrl) ?? "",
    Environment.GetEnvironmentVariable(Constants.EnvVars.SinkCertPublicBase64) ?? "",
    Environment.GetEnvironmentVariable(Constants.EnvVars.SinkCertPfxBase64) ?? "",
    Environment.GetEnvironmentVariable(Constants.EnvVars.ServerCertPath) ?? ""
));

builder.Services.AddRavenDbMigrations(migrations =>
{
    migrations.Assemblies = [typeof(MigrationContext).Assembly];
});

builder.Services.AddHostedService<SeedAndExitWorker>();

await builder.Build().RunAsync();
