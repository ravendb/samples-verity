using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RavenDB.Samples.Verity.App;
using RavenDB.Samples.Verity.App.Infrastructure;
using RavenDB.Samples.Verity.Setup;
using System.Text.Json.Serialization;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddDefaultHealthChecks();

builder.AddRavenDBClient(Constants.DatabaseName);

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
builder.Services.AddHttpContextAccessor();

//Serialize enums as strings ("Admin", "Analyst", "Viewer"), not as numbers
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpClient<SecEdgarApi>(client =>
{
    var userAgent = Environment.GetEnvironmentVariable(Constants.EnvVars.SecEdgarUserAgent)
                    ?? throw new InvalidOperationException($"No environment variable '{Constants.EnvVars.SecEdgarUserAgent}' found.");
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.Authority = Environment.GetEnvironmentVariable(Constants.EnvVars.IdentityUrl)
                        ?? throw new InvalidOperationException($"No environment variable '{Constants.EnvVars.IdentityUrl}' found.");
        opt.Audience = "verity-api";
        opt.RequireHttpsMetadata = false;
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters.RoleClaimType = "role";
        opt.TokenValidationParameters.NameClaimType = "name";
    });
builder.Services.AddAuthorization();


builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.UseMiddleware<AuthMiddleware>();

builder.Build().Run();
