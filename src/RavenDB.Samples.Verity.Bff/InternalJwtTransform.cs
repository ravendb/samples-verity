using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace RavenDB.Samples.Verity.Bff;

public class InternalJwtTransform : ITransformProvider
{
    private readonly SymmetricSecurityKey _signingKey;
    private const string ApiRouteId = "api-route";

    public InternalJwtTransform(IConfiguration configuration)
    {
        var keyValue = configuration["InternalJwtKey"]
            ?? throw new InvalidOperationException("InternalJwtKey not configured");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
    }

    public void Apply(TransformBuilderContext context)
    {
        if (context.Route.RouteId != ApiRouteId) return;

        context.AddRequestTransform(transform =>
        {
            var user = transform.HttpContext.User;
            if (user.Identity?.IsAuthenticated is not true)
                return ValueTask.CompletedTask;

            var token = MintToken(user);
            transform.ProxyRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return ValueTask.CompletedTask;
        });
    }

    public void ValidateCluster(TransformClusterValidationContext context) { }
    public void ValidateRoute(TransformRouteValidationContext context) { }

    private string MintToken(ClaimsPrincipal user)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>();

        foreach (var claimType in new[] { "sub", "name", "email", "sid" })
        {
            var claim = user.FindFirst(claimType);
            if (claim is not null) claims.Add(claim);
        }

        // Split space-separated scope claims into individual claims
        foreach (var scopeClaim in user.FindAll("scope"))
            foreach (var scope in scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                claims.Add(new Claim("scope", scope));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "verity-bff",
            Audience = "verity-api",
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddSeconds(60),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
            TokenType = "at+jwt"
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
