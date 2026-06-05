using RavenDB.Samples.Verity.Model;

namespace RavenDB.Samples.Verity.IdentityServer.Endpoints;

public static class RegisterEndpoints
{
    public static IEndpointRouteBuilder MapRegisterEndpoints(this IEndpointRouteBuilder app)
    {
        // Called by the BFF proxy: POST /bff/register → POST /api/register
        app.MapPost("/api/register", async (RegisterApiRequest req, UserStore users) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length < 2)
                return Results.BadRequest(new { error = "Username must be at least 2 characters." });
            if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
                return Results.BadRequest(new { error = "Password must be at least 8 characters." });
            if (string.IsNullOrWhiteSpace(req.DisplayName) || req.DisplayName.Length < 2)
                return Results.BadRequest(new { error = "Full name must be at least 2 characters." });
            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.BadRequest(new { error = "Email is required." });

            var (success, error) = await users.RegisterAsync(
                req.Username, req.Password, req.DisplayName, req.Email);

            return success ? Results.Ok() : Results.BadRequest(new { error });
        }).AllowAnonymous();

        return app;
    }
}

record RegisterApiRequest(
    string Username,
    string Password,
    string DisplayName,
    string Email);
