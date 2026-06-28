using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostAPI.Filters;

namespace StudioTechBI.AgentHostAPI.Controllers;

/// <summary>
/// Internal admin endpoints for subscription and credit management.
/// All endpoints require the X-Admin-Key header (bypassed when AdminApiKey is empty in dev mode).
/// </summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
[ServiceFilter(typeof(AdminAuthFilter))]
public sealed class AdminController(
    IPlanRepository planRepo,
    ISubscriptionRepository subscriptionRepo,
    IUsageRepository usageRepo,
    ICreditEngine creditEngine,
    IOptions<SubscriptionDefaults> subscriptionDefaults) : ControllerBase
{
    // ── Plans ─────────────────────────────────────────────────────────────────

    /// <summary>List all active subscription plans.</summary>
    /// <response code="200">List of plans.</response>
    [HttpGet("plans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var plans = await planRepo.GetAllActiveAsync(ct);
        return Ok(plans.Select(p => new
        {
            p.Id,
            p.Name,
            PlanType = p.PlanType.ToString(),
            p.CreditsPerCycle,
            p.IsUnlimited,
            ResetFrequency = p.ResetFrequency.ToString(),
            p.MaximumRequestsPerMinute,
            p.MaximumConcurrentRequests,
            p.IsActive,
        }));
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────

    /// <summary>List all tenant subscriptions (paginated).</summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (max 100).</param>
    /// <response code="200">Paginated list of subscriptions.</response>
    [HttpGet("subscriptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var subs = await subscriptionRepo.GetAllAsync(page, pageSize, ct);
        return Ok(subs.Select(s => new
        {
            s.Id,
            s.TenantId,
            s.TenantName,
            Plan = s.Plan.Name,
            PlanId = s.PlanId,
            s.CreditsRemaining,
            s.CreditsUsedThisCycle,
            s.CurrentCycleStart,
            s.NextResetDate,
            s.IsActive,
        }));
    }

    // ── Usage ─────────────────────────────────────────────────────────────────

    /// <summary>Query usage records. Optionally filter by tenant.</summary>
    /// <param name="tenantId">Tenant to filter; omit for all tenants.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (max 100).</param>
    /// <response code="200">Paginated usage records.</response>
    [HttpGet("usage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsage(
        [FromQuery] string? tenantId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var records = tenantId is not null
            ? await usageRepo.GetByTenantAsync(tenantId, page, pageSize, ct)
            : await usageRepo.GetAllAsync(page, pageSize, ct);

        var total = await usageRepo.GetTotalCountAsync(tenantId, ct);

        return Ok(new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = records.Select(r => new
            {
                r.Id,
                r.RequestId,
                r.TenantId,
                r.Provider,
                r.Model,
                r.CreditsConsumed,
                r.TokensUsed,
                r.ExecutionTimeMs,
                Status = r.Status.ToString(),
                r.Timestamp,
            })
        });
    }

    // ── Credit Actions ────────────────────────────────────────────────────────

    /// <summary>Force-reset a tenant's credits to their plan maximum immediately.</summary>
    /// <param name="tenantId">Tenant whose credits to reset.</param>
    /// <param name="reason">Optional reason recorded in the ledger.</param>
    /// <response code="200">Subscription after reset.</response>
    /// <response code="404">Tenant subscription not found.</response>
    [HttpPost("subscriptions/{tenantId}/reset-credits")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetCredits(
        string tenantId,
        [FromQuery] string? reason = null,
        CancellationToken ct = default)
    {
        var changedBy = User.Identity?.Name ?? "admin-api";
        var sub = await creditEngine.ForceResetAsync(tenantId, changedBy, reason, ct);
        return Ok(new
        {
            sub.TenantId,
            sub.CreditsRemaining,
            sub.CurrentCycleStart,
            sub.NextResetDate,
            Plan = sub.Plan.Name,
        });
    }

    /// <summary>Migrate a tenant to a new plan with proportional credit carry-over.</summary>
    /// <param name="tenantId">Tenant to migrate.</param>
    /// <param name="request">New plan details.</param>
    /// <response code="200">Subscription after plan change.</response>
    /// <response code="404">Tenant or plan not found.</response>
    [HttpPost("subscriptions/{tenantId}/change-plan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePlan(
        string tenantId,
        [FromBody] ChangePlanRequest request,
        CancellationToken ct = default)
    {
        var changedBy = User.Identity?.Name ?? "admin-api";
        var sub = await creditEngine.ChangePlanAsync(tenantId, request.NewPlanId, changedBy, request.Notes, ct);
        return Ok(new
        {
            sub.TenantId,
            sub.CreditsRemaining,
            sub.CurrentCycleStart,
            sub.NextResetDate,
            Plan = sub.Plan.Name,
        });
    }

    /// <summary>Update subscription default settings (runtime — not persisted to config file).</summary>
    /// <remarks>Changes apply only to the running process. Restart will revert to appsettings values.</remarks>
    /// <response code="200">Current defaults (read from bound options).</response>
    [HttpPut("settings/defaults")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDefaults()
    {
        var d = subscriptionDefaults.Value;
        return Ok(new
        {
            d.CreditsConsumedPerRequest,
            d.DefaultPlanName,
            d.FallbackTenantId,
            d.AutoRegisterNewTenants,
        });
    }
}

/// <summary>Request body for changing a tenant's plan.</summary>
public sealed record ChangePlanRequest(Guid NewPlanId, string? Notes);
