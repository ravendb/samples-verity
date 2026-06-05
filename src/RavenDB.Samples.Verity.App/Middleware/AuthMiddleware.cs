using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Reflection;
using System.Security.Claims;

namespace RavenDB.Samples.Verity.App;

// IFunctionsWorkerMiddleware runs inside the Azure Functions pipeline (before the function executes).
// It authenticates every HTTP request using the registered JWT Bearer scheme and enforces
// [Authorize] and [Authorize(Roles = "...")] on functions that declare it.
public sealed class AuthMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();

        // Non-HTTP triggers (Queue, Timer, …) skip auth entirely.
        if (httpContext is null)
        {
            await next(context);
            return;
        }

        // Authenticate: validates the JWT and populates HttpContext.User.
        var result = await httpContext.AuthenticateAsync();
        if (result.Succeeded)
            httpContext.User = result.Principal;

        var authorize = GetAuthorizeAttribute(context);

        // Enforce [Authorize]: user must be authenticated.
        if (authorize is not null && httpContext.User.Identity?.IsAuthenticated != true)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Enforce [Authorize(Roles = "...")]: user must have at least one of the required roles.
        if (authorize?.Roles is { Length: > 0 } roles)
        {
            var hasRole = roles.Split(',')
                .Select(r => r.Trim())
                .Any(r => httpContext.User.HasClaim(
                    c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == r));

            if (!hasRole)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        await next(context);
    }

    // Resolves the function entry point (e.g. "RavenDB.Samples.Verity.App.Api.CreateAudit")
    // to a MethodInfo and returns the [Authorize] attribute if present.
    private static AuthorizeAttribute? GetAuthorizeAttribute(FunctionContext context)
    {
        var ep  = context.FunctionDefinition.EntryPoint; // "Namespace.Class.Method"
        var dot = ep.LastIndexOf('.');
        if (dot < 0) return null;

        var typeName   = ep[..dot];
        var methodName = ep[(dot + 1)..];

        var type = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
            .FirstOrDefault(t => t.FullName == typeName);

        var method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        return method?.GetCustomAttribute<AuthorizeAttribute>()
            ?? type?.GetCustomAttribute<AuthorizeAttribute>();
    }
}
