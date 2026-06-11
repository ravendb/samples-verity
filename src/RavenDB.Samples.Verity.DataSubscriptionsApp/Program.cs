using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Http;
using RavenDB.Samples.Verity.DataSubscriptionsApp;
using Spectre.Console;
using System.Net.Security;

// Accept self-signed server certs — chain errors only; name and revocation checks still apply.
RequestExecutor.RemoteCertificateValidationCallback +=
    (_, _, _, errors) => (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.None;

AnsiConsole.Profile.Capabilities.Ansi = true;
AnsiConsole.Profile.Capabilities.Unicode = true;

var builder = Host.CreateApplicationBuilder(args);

builder.AddRavenDBClient("verity-sink");
builder.Services.AddHostedService<SubscriptionWorker>();
builder.Logging.SetMinimumLevel(LogLevel.None);

await builder.Build().RunAsync();