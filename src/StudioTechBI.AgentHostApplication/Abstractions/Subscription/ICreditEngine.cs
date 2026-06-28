using StudioTechBI.AgentHostDomain.Entities;

namespace StudioTechBI.AgentHostApplication.Abstractions.Subscription;

/// <summary>
/// Central orchestrator for credit lifecycle: load, reset, validate, deduct.
/// Implemented in Infrastructure; consumed by middleware and controllers.
/// </summary>
public interface ICreditEngine
{
    /// <summary>
    /// Returns the active subscription for the tenant.
    /// If none exists and AutoRegisterNewTenants=true, creates a Trial subscription.
    /// </summary>
    Task<TenantSubscription> GetOrCreateSubscriptionAsync(string tenantId, string tenantName, CancellationToken ct = default);

    /// <summary>
    /// Checks if the subscription's reset date has passed and performs reset if needed.
    /// Returns true if a reset was performed.
    /// </summary>
    Task<bool> CheckAndResetIfNeededAsync(TenantSubscription subscription, CancellationToken ct = default);

    /// <summary>
    /// Deducts credits after a successful AI generation.
    /// Records UsageRecord + CreditTransaction. Returns credit status after deduction.
    /// </summary>
    Task<CreditDeductionResult> DeductAsync(
        TenantSubscription subscription,
        Guid requestId,
        Guid blueprintId,
        int tokensUsed,
        long executionTimeMs,
        string provider,
        string model,
        CancellationToken ct = default);

    /// <summary>Force-resets credits regardless of reset date. Used by admin API.</summary>
    Task<TenantSubscription> ForceResetAsync(string tenantId, string changedBy, string? reason, CancellationToken ct = default);

    /// <summary>Migrates tenant to a new plan, adjusts credits proportionally.</summary>
    Task<TenantSubscription> ChangePlanAsync(string tenantId, Guid newPlanId, string changedBy, string? notes, CancellationToken ct = default);
}

public sealed record CreditDeductionResult(
    int CreditsConsumed,
    int CreditsRemaining,
    DateTimeOffset? ResetDate,
    string PlanName,
    bool IsUnlimited);
