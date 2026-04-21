using CommunityToolkit.Aspire.RavenDB.Client;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using RavenDB.Samples.Verity.Identity.Documents;
using RavenDB.Samples.Verity.Identity.Indexes;
using RavenDB.Samples.Verity.Identity.Stores;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

builder.AddRavenDBClient(connectionName: "verity");

builder.Services.AddSingleton<IPasswordHasher<IdentityUserDoc>, PasswordHasher<IdentityUserDoc>>();
builder.Services.AddScoped<RavenDbUserStore>();

builder.Services.AddRazorPages();

builder.Services
    .AddIdentityServer(options =>
    {
        options.KeyManagement.Enabled = true;
        options.KeyManagement.SigningAlgorithms = new[]
        {
            new SigningAlgorithmOptions(SecurityAlgorithms.RsaSsaPssSha256)
        };

        options.PushedAuthorization.Required = true;
        options.EmitStaticAudienceClaim = true;

        options.Events.RaiseSuccessEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
    })
    .AddClientStore<RavenDbClientStore>()
    .AddResourceStore<RavenDbResourceStore>()
    .AddPersistedGrantStore<RavenDbPersistedGrantStore>()
    .AddSigningKeyStore<RavenDbSigningKeyStore>()
    .AddProfileService<RavenDbProfileService>();

builder.Services.AddTransient<IEventSink, RavenDbEventSink>();

builder.Services.AddAuthentication();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
    IndexCreation.CreateIndexes(typeof(PersistedGrants_ByLookup).Assembly, store);
}

app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages().RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();
