using Duende.IdentityServer;
using RavenDB.Samples.Verity.IdentityServer;
using RavenDB.Samples.Verity.IdentityServer.Endpoints;
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

var dbName = builder.Configuration["SAMPLES_VERITY_DB_NAME"]
    ?? throw new InvalidOperationException("Missing configuration: SAMPLES_VERITY_DB_NAME");

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
    .AddIdentityServer(options =>
    {
        options.Events.RaiseErrorEvents   = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
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
