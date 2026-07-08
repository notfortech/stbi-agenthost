using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Models;

namespace StudioTechBI.AgentHostAPI.Middleware;

/// <summary>
/// Validates the <c>X-Api-Key</c> header sent by Koru on every request.
/// Bypass is active when Auth:ApiKey is empty (dev / private-VNet deployments).
/// </summary>
public sealed class ApiKeyAuthMiddleware(RequestDelegate next)
{
    // Paths that must be reachable without an API key.
    private static readonly string[] _publicPrefixes =
    [
        "/swagger",
        "/healthz",
        "/health"
    ];

    public async Task InvokeAsync(HttpContext context, IOptions<AuthOptions> authOptions)
    {
        var requiredKey = authOptions.Value.ApiKey;

        // Bypass when no key is configured (dev / internal-only mode).
        if (string.IsNullOrEmpty(requiredKey))
        {
            await next(context);
            return;
        }

        // Always allow Swagger UI and health endpoints through without an API key.
        var path = context.Request.Path.Value ?? string.Empty;
        if (_publicPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var provided = context.Request.Headers["X-Api-Key"].FirstOrDefault();

        if (provided != requiredKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                status = 401,
                title = "Unauthorized",
                detail = "A valid X-Api-Key header is required."
            });
            return;
        }

        await next(context);
    }
}
