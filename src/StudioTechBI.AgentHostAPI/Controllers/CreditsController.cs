using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostDomain.Exceptions;

namespace StudioTechBI.AgentHostAPI.Controllers;

/// <summary>
/// Generic credit check/consume surface for AI-assisted features that don't go through
/// stbi-agenthost's own generation pipeline (e.g. koru-main's AI report-model generation,
/// proxied to stbi_transformers). Blueprint generation continues to use
/// <see cref="StudioTechBI.AgentHostAPI.Middleware.CreditValidationMiddleware"/> and
/// <c>ICreditEngine.DeductAsync</c> directly — this controller lets other trusted callers
/// (authenticated via the same X-Api-Key scheme as every other route here) reuse the one
/// credit ledger instead of each service inventing its own.
/// </summary>
[ApiController]
[Route("api/credits")]
[Produces("application/json")]
public sealed class CreditsController(
    ICreditEngine creditEngine,
    IOptions<SubscriptionDefaults> subscriptionDefaults,
    ILogger<CreditsController> logger) : ControllerBase
{
    private const string TenantIdHeader = "X-Tenant-Id";
    private const string TenantNameHeader = "X-Tenant-Name";

    /// <summary>
    /// Pre-flight check — resolves (or auto-creates) the tenant's subscription, performs a lazy
    /// cycle reset if due, and returns the current balance. Returns 402 if the tenant has no
    /// credits left; callers should not proceed with the AI call in that case. Respects
    /// SubscriptionDefaults.EffectiveBypassCreditLimit, same as CreditValidationMiddleware and
    /// ICreditEngine.DeductAsync — without this, a caller hitting /check directly (bypassing
    /// koru-main's own client-side bypass) would still get blocked here.
    /// </summary>
    /// <response code="200">Tenant has credits available.</response>
    /// <response code="400">X-Tenant-Id header missing.</response>
    /// <response code="402">Tenant has insufficient credits.</response>
    [HttpPost("check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    public async Task<IActionResult> Check(CancellationToken ct)
    {
        var tenantId = Request.Headers[TenantIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { status = 400, title = "Validation Failed", detail = $"{TenantIdHeader} header is required." });

        var tenantName = Request.Headers[TenantNameHeader].FirstOrDefault() ?? tenantId;

        var subscription = await creditEngine.GetOrCreateSubscriptionAsync(tenantId, tenantName, ct);
        await creditEngine.CheckAndResetIfNeededAsync(subscription, ct);

        var bypassed = subscriptionDefaults.Value.EffectiveBypassCreditLimit;

        if (!subscription.Plan.IsUnlimited && subscription.CreditsRemaining <= 0 && !bypassed)
            throw new InsufficientCreditsException(
                tenantId, subscription.CreditsRemaining, subscription.NextResetDate, subscription.Plan.Name);

        return Ok(new
        {
            tenantId,
            plan = subscription.Plan.Name,
            creditsRemaining = subscription.Plan.IsUnlimited ? (int?)null : (bypassed ? SubscriptionDefaults.BypassCreditsRemaining : subscription.CreditsRemaining),
            isUnlimited = subscription.Plan.IsUnlimited,
            nextResetDate = subscription.NextResetDate,
        });
    }

    /// <summary>
    /// Deducts one request's worth of credits for a completed AI-assisted action outside the
    /// Blueprint pipeline (e.g. AI report-model generation). Call this only after the AI call
    /// succeeded — a failed generation should not consume credits.
    /// </summary>
    /// <response code="200">Credits deducted (or plan is unlimited).</response>
    /// <response code="400">X-Tenant-Id header missing.</response>
    [HttpPost("consume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Consume([FromBody] ConsumeCreditRequest request, CancellationToken ct)
    {
        var tenantId = Request.Headers[TenantIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tenantId))
            return BadRequest(new { status = 400, title = "Validation Failed", detail = $"{TenantIdHeader} header is required." });

        var tenantName = Request.Headers[TenantNameHeader].FirstOrDefault() ?? tenantId;

        var subscription = await creditEngine.GetOrCreateSubscriptionAsync(tenantId, tenantName, ct);
        await creditEngine.CheckAndResetIfNeededAsync(subscription, ct);

        var requestId = Guid.TryParse(request.RequestId, out var parsed) ? parsed : Guid.NewGuid();

        var result = await creditEngine.DeductAsync(
            subscription,
            requestId,
            blueprintId: Guid.Empty,
            tokensUsed: 0,
            executionTimeMs: request.ExecutionTimeMs,
            provider: "koru-main",
            model: request.Feature,
            ct: ct);

        logger.LogInformation(
            "Credit consumed outside Blueprint pipeline | TenantId={TenantId} | Feature={Feature} | RequestId={RequestId}",
            tenantId, request.Feature, requestId);

        return Ok(new
        {
            creditsConsumed = result.CreditsConsumed,
            creditsRemaining = result.IsUnlimited ? (int?)null : result.CreditsRemaining,
            isUnlimited = result.IsUnlimited,
            plan = result.PlanName,
            resetDate = result.ResetDate,
        });
    }
}

/// <summary>Request body for <see cref="CreditsController.Consume"/>.</summary>
/// <param name="Feature">Short identifier for the AI-assisted action, e.g. "report-model-generation".</param>
/// <param name="RequestId">Correlation id from the calling service, if any — used for the usage ledger.</param>
/// <param name="ExecutionTimeMs">How long the AI call took, for the usage ledger.</param>
public sealed record ConsumeCreditRequest(string Feature, string? RequestId, long ExecutionTimeMs = 0);
