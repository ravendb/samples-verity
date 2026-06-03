using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Reflection;

namespace RavenDB.Samples.Verity.App;

// IFunctionsWorkerMiddleware runs inside the Azure Functions pipeline (before the function executes).
// It authenticates every HTTP request using the registered JWT Bearer scheme and enforces
// [Authorize] on functions that declare it.
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

        // Enforce [Authorize] when present on the function method or its class.
        if (RequiresAuth(context) && httpContext.User.Identity?.IsAuthenticated != true)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    // Resolves the function entry point (e.g. "RavenDB.Samples.Verity.App.Api.CreateAudit")
    // to a MethodInfo and checks for [Authorize].
    private static bool RequiresAuth(FunctionContext context)
    {
        var ep  = context.FunctionDefinition.EntryPoint; // "Namespace.Class.Method"
        var dot = ep.LastIndexOf('.');
        if (dot < 0) return false;

        var typeName   = ep[..dot];
        var methodName = ep[(dot + 1)..];

        var type = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
            .FirstOrDefault(t => t.FullName == typeName);

        var method = type?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        return method?.GetCustomAttribute<AuthorizeAttribute>() is not null
            || type?.GetCustomAttribute<AuthorizeAttribute>()  is not null;
    }
}
