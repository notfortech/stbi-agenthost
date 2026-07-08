using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Models;

namespace StudioTechBI.AgentHostAPI.Middleware;

/// <summary>
/// Validates the <c>X-Api-Key</c> header sent by Koru on every request.
/// Bypass is active when Auth:ApiKey is empty (dev / private-VNet deployments).
/// </summary>
public sealed class ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
{
    // Paths that must be reachable without an API key.
    private static readonly string[] _publicPrefixes =
    [
        "/swagger",
        "/healthz",
        "/health",
        "/api/health",
        "/ping"
    ];

    public async Task InvokeAsync(HttpContext context, IOptions<AuthOptions> authOptions)
    {
        var authHeader   = context.Request.Headers.Authorization.ToString();
        var xApiKeyHeader = context.Request.Headers["X-Api-Key"].ToString();

        logger.LogInformation(
            "ApiKeyAuth {Method} {Path} | " +
            "AuthorizationPresent={AuthPresent} AuthorizationLength={AuthLen} AuthorizationPrefix={AuthPrefix} | " +
            "XApiKeyPresent={KeyPresent} XApiKeyLength={KeyLen} XApiKeyPrefix={KeyPrefix}",
            context.Request.Method,
            context.Request.Path,
            !string.IsNullOrEmpty(authHeader),
            authHeader.Length,
            authHeader.Length >= 4 ? authHeader[..4] : authHeader,
            !string.IsNullOrEmpty(xApiKeyHeader),
            xApiKeyHeader.Length,
            xApiKeyHeader.Length >= 4 ? xApiKeyHeader[..4] : xApiKeyHeader);

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
