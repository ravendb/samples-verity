using Duende.Bff.AccessTokenManagement;
using Duende.Bff;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

// ─── URLs injected by AppHost ────────────────────────────────────────────────
var identityUrl = builder.Configuration["Identity:Url"]
    ?? throw new InvalidOperationException("Missing configuration: Identity:Url");
var apiUrl      = builder.Configuration["Api:Url"]
    ?? throw new InvalidOperationException("Missing configuration: Api:Url");
var frontendUrl = builder.Configuration["Frontend:Url"]
    ?? throw new InvalidOperationException("Missing configuration: Frontend:Url");

// ─── Duende BFF (session + management endpoints) ─────────────────────────────
// Generate RSA key for DPoP
var rsaKey = new RsaSecurityKey(RSA.Create(2048));
var jwk = JsonWebKeyConverter.ConvertFromSecurityKey(rsaKey);
jwk.Alg = SecurityAlgorithms.RsaSsaPssSha256;

builder.Services.AddBff(options =>
{
    options.DPoPJsonWebKey = DPoPProofKey.Parse(JsonSerializer.Serialize(jwk));
});

// ─── Authentication ───────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme          = "cookie";
        options.DefaultChallengeScheme = "oidc";
        options.DefaultSignOutScheme   = "oidc";
    })
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name         = "verity-bff";
        options.Cookie.SameSite     = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority                     = identityUrl;
        options.ClientId                      = "verity-bff";
        options.ClientSecret                  = "bff-secret";
        options.ResponseType                  = "code";
        options.ResponseMode                  = "query";
        options.RequireHttpsMetadata          = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.PushedAuthorizationBehavior   = PushedAuthorizationBehavior.Require;
        options.MapInboundClaims              = false;
        options.SaveTokens                    = true;

        options.Scope.Clear();
        foreach (var s in new[] { "openid", "profile", "email", "verity-api", "offline_access" })
            options.Scope.Add(s);
    });

// ─── Authorization ────────────────────────────────────────────────────────────
builder.Services.AddAuthorization();

// ─── HTTP clients ─────────────────────────────────────────────────────────────
builder.Services.AddHttpClient();
builder.Services.AddHttpForwarder();

// ─── Pipeline ─────────────────────────────────────────────────────────────────
var app = builder.Build();
app.MapDefaultEndpoints();

app.UseRouting();
app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

// BFF management: /bff/login  /bff/logout  /bff/user  etc.
app.MapBffManagementEndpoints();

// POST /bff/register — proxy JSON registration body to IdentityServer.
app.MapPost("/bff/register", async (HttpRequest req, IHttpClientFactory http) =>
{
    using var client  = http.CreateClient();
    var content       = new StreamContent(req.Body);
    content.Headers.ContentType = new("application/json");

    var response = await client.PostAsync($"{identityUrl}/api/register", content);
    var body     = await response.Content.ReadAsStringAsync();
    return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
}).AllowAnonymous();

// /api/* → Azure Functions.
// ApiTokenTransformer reads the access token from the session cookie and adds
// it as a Bearer header, implementing the BFF token-forwarding pattern without YARP.
app.MapForwarder("/api/{**catch-all}", apiUrl,
    new ForwarderRequestConfig(), new ApiTokenTransformer());

// /* → Vite dev server (plain proxy, WebSocket/HMR included).
app.MapForwarder("/{**catch-all}", frontendUrl);

app.Run();

// ─── Token transformer ────────────────────────────────────────────────────────
sealed class ApiTokenTransformer : HttpTransformer
{
    public override async ValueTask TransformRequestAsync(
        HttpContext        httpContext,
        HttpRequestMessage proxyRequest,
        string             destinationPrefix,
        CancellationToken  cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        // Reads the access token stored in the session cookie by SaveTokens = true.
        // Token refresh is handled automatically by Duende.AccessTokenManagement
        // (included with AddBff()) in the background.
        var token = await httpContext.GetTokenAsync("access_token");
        if (token is not null)
            proxyRequest.Headers.Authorization = new("Bearer", token);
    }
}
