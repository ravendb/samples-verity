using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RavenDB.Samples.Verity.Bff;
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

var authority = builder.Configuration["Authority"]
    ?? throw new InvalidOperationException("Authority not configured");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "__Host-verity";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = authority;
        options.ClientId = "verity-bff";
        options.ClientSecret = builder.Configuration["OidcClientSecret"];
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("verity.read");
        options.Scope.Add("verity.audit.write");
        options.Scope.Add("offline_access");
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";
        options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
        {
            // Store individual scope claims in the principal so InternalJwtTransform can forward them
            OnTokenValidated = ctx =>
            {
                var scopeString = ctx.TokenEndpointResponse?.Scope;
                if (!string.IsNullOrEmpty(scopeString))
                {
                    var identity = (System.Security.Claims.ClaimsIdentity)ctx.Principal!.Identity!;
                    foreach (var scope in scopeString.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        identity.AddClaim(new System.Security.Claims.Claim("scope", scope));
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddBff();

builder.Services.AddReverseProxy()
    .AddBffExtensions()
    .AddTransforms<InternalJwtTransform>()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseRouting();
app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();

app.MapBffReverseProxy();

app.MapDefaultEndpoints();

app.Run();
