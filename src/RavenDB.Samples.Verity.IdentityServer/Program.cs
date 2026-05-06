using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Microsoft.IdentityModel.Tokens;
using RavenDB.Samples.Verity.IdentityServer;
using RavenDB.Samples.Verity.IdentityServer.Endpoints;
using RavenDB.Samples.Verity.Setup;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorPages();

// Frontend sends role as a string ("User" / "Employee") — configure minimal-API
// JSON deserialization to accept string enum values instead of numeric ones.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var bffBaseUrl = builder.Configuration["Bff:BaseUrl"]
    ?? throw new InvalidOperationException("Missing configuration: Bff:BaseUrl");

var dbName = Constants.DatabaseName ?? "";

builder.AddRavenDBClient(dbName);

builder.Services.AddSingleton<UserStore>();

// IdentityServer sets SameSite=None on its auth cookies, which requires Secure
// (HTTPS). In dev we run on plain HTTP so the browser rejects those cookies and
// the user appears unauthenticated on the very next request. Override every
// outgoing SameSite=None cookie at middleware level before headers are sent.
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.OnAppendCookie = ctx =>
    {
        if (ctx.CookieOptions.SameSite == SameSiteMode.None)
        {
            ctx.CookieOptions.SameSite = SameSiteMode.Lax;
            ctx.CookieOptions.Secure   = false;
        }
    };
});

builder.Services
    .AddIdentityServer(opt =>
    {
        opt.Events.RaiseErrorEvents   = true;
        opt.Events.RaiseFailureEvents = true;
        opt.Events.RaiseSuccessEvents = true;
        if (builder.Environment.IsProduction())
        {
            opt.KeyManagement.KeyPath = "/tmp/keys";
        }
        opt.KeyManagement.SigningAlgorithms.Add(new SigningAlgorithmOptions(SecurityAlgorithms.RsaSsaPssSha256));

        opt.DPoP.SupportedDPoPSigningAlgorithms = [
            SecurityAlgorithms.RsaSsaPssSha256,
        SecurityAlgorithms.RsaSsaPssSha384,
        SecurityAlgorithms.RsaSsaPssSha512,

        SecurityAlgorithms.EcdsaSha256,
        SecurityAlgorithms.EcdsaSha384,
        SecurityAlgorithms.EcdsaSha512
        ];
        opt.SupportedClientAssertionSigningAlgorithms = [
            SecurityAlgorithms.RsaSsaPssSha256,
        SecurityAlgorithms.RsaSsaPssSha384,
        SecurityAlgorithms.RsaSsaPssSha512,

        SecurityAlgorithms.EcdsaSha256,
        SecurityAlgorithms.EcdsaSha384,
        SecurityAlgorithms.EcdsaSha512
        ];
        opt.SupportedRequestObjectSigningAlgorithms = [
            SecurityAlgorithms.RsaSsaPssSha256,
        SecurityAlgorithms.RsaSsaPssSha384,
        SecurityAlgorithms.RsaSsaPssSha512,

        SecurityAlgorithms.EcdsaSha256,
        SecurityAlgorithms.EcdsaSha384,
        SecurityAlgorithms.EcdsaSha512
        ];
        opt.JwtValidationClockSkew = TimeSpan.FromSeconds(10);


    })
    .AddInMemoryIdentityResources(IdentityConfig.IdentityResources)
    .AddInMemoryApiScopes(IdentityConfig.ApiScopes)
    .AddInMemoryClients(IdentityConfig.GetClients(bffBaseUrl));

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();
app.MapRazorPages().AllowAnonymous();
app.MapRegisterEndpoints();

app.Run();
