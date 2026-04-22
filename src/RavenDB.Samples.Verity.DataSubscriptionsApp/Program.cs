using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RavenDB.Samples.Verity.DataSubscriptionsApp;
using Spectre.Console;

AnsiConsole.Profile.Capabilities.Ansi = true;
AnsiConsole.Profile.Capabilities.Unicode = true;

var builder = Host.CreateApplicationBuilder(args);

builder.AddRavenDBClient("verity");
builder.Services.AddHostedService<SubscriptionWorker>();
builder.Logging.SetMinimumLevel(LogLevel.None);

await builder.Build().RunAsync();