using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostDomain.Exceptions;

namespace StudioTechBI.AgentHostAPI.Middleware;

/// <summary>
/// Intercepts POST /api/blueprints/generate, resolves the tenant, loads (or creates) its
/// subscription, performs a lazy credit reset if the cycle has rolled over, then validates
/// that credits are available before forwarding the request.
///
/// Tenant resolution order:
///   1. X-Tenant-Id request header
///   2. SubscriptionDefaults:FallbackTenantId (empty string disables fallback for production)
///
/// The resolved TenantSubscription is stored in HttpContext.Items["TenantSubscription"] so
/// BlueprintsController can retrieve it without a second DB round-trip.
/// </summary>
public sealed class CreditValidationMiddleware
{
    private const string TenantIdHeader = "X-Tenant-Id";
    private const string TenantNameHeader = "X-Tenant-Name";
    internal const string SubscriptionItemKey = "TenantSubscription";
    internal const string TenantIdItemKey = "ResolvedTenantId";

    private static readonly PathString _generatePath = new("/api/blueprints/generate");

    private readonly RequestDelegate _next;

    public CreditValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ICreditEngine creditEngine,
        IOptions<SubscriptionDefaults> subscriptionDefaults)
    {
        if (!context.Request.Path.Equals(_generatePath, StringComparison.OrdinalIgnoreCase)
            || !HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var defaults = subscriptionDefaults.Value;

        var tenantId = context.Request.Headers[TenantIdHeader].FirstOrDefault()
            ?? defaults.FallbackTenantId;

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                status = 401,
                title = "Tenant identification required",
                detail = "Provide an X-Tenant-Id header."
            });
            return;
        }

        var tenantName = context.Request.Headers[TenantNameHeader].FirstOrDefault() ?? tenantId;

        var subscription = await creditEngine.GetOrCreateSubscriptionAsync(tenantId, tenantName, context.RequestAborted);

        await creditEngine.CheckAndResetIfNeededAsync(subscription, context.RequestAborted);

        if (!subscription.Plan.IsUnlimited && subscription.CreditsRemaining <= 0)
            throw new InsufficientCreditsException(
                tenantId,
                subscription.CreditsRemaining,
                subscription.NextResetDate,
                subscription.Plan.Name);

        context.Items[SubscriptionItemKey] = subscription;
        context.Items[TenantIdItemKey] = tenantId;

        await _next(context);
    }
}
