using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Collections.Concurrent;
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

    // Cached per entry-point string — reflection runs once per unique function, not per request.
    private static readonly ConcurrentDictionary<string, AuthorizeAttribute?> _authorizeCache = new();

    private static AuthorizeAttribute? GetAuthorizeAttribute(FunctionContext context)
    {
        var entryPoint = context.FunctionDefinition.EntryPoint;
        if (string.IsNullOrWhiteSpace(entryPoint))
            return null;

        return _authorizeCache.GetOrAdd(entryPoint, static ep =>
        {
            var dot = ep.LastIndexOf('.');
            if (dot < 0) return null;

            var typeName   = ep[..dot];
            var methodName = ep[(dot + 1)..];

            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
                .FirstOrDefault(t => t.FullName == typeName);

            // Include Static so static entry points are not silently skipped.
            var method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            return method?.GetCustomAttribute<AuthorizeAttribute>()
                ?? type?.GetCustomAttribute<AuthorizeAttribute>();
        });
    }
}
