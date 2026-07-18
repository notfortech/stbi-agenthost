using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostDomain.Entities;
using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostDomain.Exceptions;
using StudioTechBI.AgentHostInfrastructure.Data;

namespace StudioTechBI.AgentHostInfrastructure.Services;

public sealed class CreditEngine(
    ISubscriptionRepository subscriptionRepo,
    IPlanRepository planRepo,
    ICreditRepository creditRepo,
    IUsageRepository usageRepo,
    IOptions<SubscriptionDefaults> subscriptionDefaults,
    ILogger<CreditEngine> logger) : ICreditEngine
{
    private readonly SubscriptionDefaults _defaults = subscriptionDefaults.Value;

    public async Task<TenantSubscription> GetOrCreateSubscriptionAsync(
        string tenantId, string tenantName, CancellationToken ct = default)
    {
        var sub = await subscriptionRepo.GetByTenantIdAsync(tenantId, ct);
        var isInternal = _defaults.InternalTenantIds.Contains(tenantId);

        if (sub is not null)
        {
            if (isInternal && sub.Plan.Name != _defaults.InternalTenantPlanName)
            {
                // Self-heal: a tenant added to the internal/QA allowlist after it already has a
                // subscription (e.g. one that already exhausted a Trial allotment) gets upgraded
                // automatically on next contact — no separate manual admin action needed. Reuses
                // ChangePlanAsync's existing, audited plan-change logic (proportional carry-over,
                // TransactionType.PlanChange record, SubscriptionHistory row) instead of
                // duplicating field-mutation logic here.
                var internalPlan = await planRepo.GetByNameAsync(_defaults.InternalTenantPlanName, ct)
                    ?? throw new InvalidOperationException(
                        $"Internal tenant plan '{_defaults.InternalTenantPlanName}' not found in database.");
                logger.LogInformation(
                    "Internal tenant {TenantId} found on plan {CurrentPlan} — upgrading to {InternalPlan}",
                    tenantId, sub.Plan.Name, internalPlan.Name);
                return await ChangePlanAsync(tenantId, internalPlan.Id, "system:internal-tenant-allowlist",
                    "Auto-upgraded on contact — tenant is on the internal/QA allowlist", ct);
            }
            return sub;
        }

        if (!_defaults.AutoRegisterNewTenants)
            throw new SubscriptionNotFoundException(tenantId);

        var defaultPlanName = isInternal ? _defaults.InternalTenantPlanName : _defaults.DefaultPlanName;
        var plan = await planRepo.GetByNameAsync(defaultPlanName, ct)
            ?? throw new InvalidOperationException($"Default plan '{defaultPlanName}' not found in database.");

        var now = DateTimeOffset.UtcNow;
        sub = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantName = tenantName,
            PlanId = plan.Id,
            Plan = plan,
            CreditsRemaining = plan.IsUnlimited ? int.MaxValue : plan.CreditsPerCycle,
            CreditsUsedThisCycle = 0,
            CurrentCycleStart = now,
            NextResetDate = plan.ResetFrequency == ResetFrequency.Never
                ? null
                : DatabaseSeeder.ComputeNextReset(now, plan.ResetFrequency),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        sub = await subscriptionRepo.AddAsync(sub, ct);
        logger.LogInformation("Auto-registered tenant {TenantId} on plan {Plan}", tenantId, plan.Name);

        await RecordTransactionAsync(sub, plan.CreditsPerCycle, TransactionType.InitialGrant,
            null, "Initial credit grant on auto-registration", 0, sub.CreditsRemaining, ct);

        return sub;
    }

    public async Task<bool> CheckAndResetIfNeededAsync(TenantSubscription subscription, CancellationToken ct = default)
    {
        if (subscription.Plan.IsUnlimited) return false;
        if (subscription.NextResetDate is null) return false;
        if (subscription.NextResetDate > DateTimeOffset.UtcNow) return false;

        var plan = subscription.Plan;
        var balanceBefore = subscription.CreditsRemaining;
        var now = DateTimeOffset.UtcNow;

        subscription.CreditsRemaining = plan.CreditsPerCycle;
        subscription.CreditsUsedThisCycle = 0;
        subscription.CurrentCycleStart = now;
        subscription.NextResetDate = plan.ResetFrequency == ResetFrequency.Never
            ? null
            : DatabaseSeeder.ComputeNextReset(now, plan.ResetFrequency);

        await subscriptionRepo.UpdateAsync(subscription, ct);

        await RecordTransactionAsync(subscription, plan.CreditsPerCycle, TransactionType.CreditReset,
            null, $"Cycle reset — previous balance {balanceBefore}",
            balanceBefore, subscription.CreditsRemaining, ct);

        logger.LogInformation("Reset credits for tenant {TenantId}: {Credits} credits, next reset {NextReset}",
            subscription.TenantId, plan.CreditsPerCycle, subscription.NextResetDate);

        return true;
    }

    public async Task<CreditDeductionResult> DeductAsync(
        TenantSubscription subscription,
        Guid requestId,
        Guid blueprintId,
        int tokensUsed,
        long executionTimeMs,
        string provider,
        string model,
        CancellationToken ct = default)
    {
        var creditsToConsume = _defaults.CreditsConsumedPerRequest;
        var balanceBefore = subscription.CreditsRemaining;

        if (!subscription.Plan.IsUnlimited)
        {
            subscription.CreditsRemaining = Math.Max(0, subscription.CreditsRemaining - creditsToConsume);
            subscription.CreditsUsedThisCycle += creditsToConsume;
            await subscriptionRepo.UpdateAsync(subscription, ct);

            await RecordTransactionAsync(subscription, -creditsToConsume, TransactionType.CreditDeduction,
                requestId.ToString(), $"AI generation — {provider}/{model}",
                balanceBefore, subscription.CreditsRemaining, ct);
        }

        var usage = new UsageRecord
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            TenantId = subscription.TenantId,
            SubscriptionId = subscription.Id,
            BlueprintId = blueprintId,
            Provider = provider,
            Model = model,
            ExecutionTimeMs = executionTimeMs,
            TokensUsed = tokensUsed,
            CreditsConsumed = subscription.Plan.IsUnlimited ? 0 : creditsToConsume,
            Timestamp = DateTimeOffset.UtcNow,
            Status = UsageStatus.Succeeded,
        };
        await usageRepo.AddAsync(usage, ct);

        return new CreditDeductionResult(
            CreditsConsumed: subscription.Plan.IsUnlimited ? 0 : creditsToConsume,
            CreditsRemaining: subscription.Plan.IsUnlimited ? int.MaxValue : subscription.CreditsRemaining,
            ResetDate: subscription.NextResetDate,
            PlanName: subscription.Plan.Name,
            IsUnlimited: subscription.Plan.IsUnlimited);
    }

    public async Task<TenantSubscription> ForceResetAsync(
        string tenantId, string changedBy, string? reason, CancellationToken ct = default)
    {
        var sub = await subscriptionRepo.GetByTenantIdAsync(tenantId, ct)
            ?? throw new SubscriptionNotFoundException(tenantId);

        var balanceBefore = sub.CreditsRemaining;
        var now = DateTimeOffset.UtcNow;

        sub.CreditsRemaining = sub.Plan.IsUnlimited ? int.MaxValue : sub.Plan.CreditsPerCycle;
        sub.CreditsUsedThisCycle = 0;
        sub.CurrentCycleStart = now;
        sub.NextResetDate = sub.Plan.ResetFrequency == ResetFrequency.Never
            ? null
            : DatabaseSeeder.ComputeNextReset(now, sub.Plan.ResetFrequency);

        await subscriptionRepo.UpdateAsync(sub, ct);

        await RecordTransactionAsync(sub, sub.Plan.CreditsPerCycle, TransactionType.ManualAdjustment,
            null, reason ?? $"Force reset by {changedBy}", balanceBefore, sub.CreditsRemaining, ct);

        logger.LogInformation("Force-reset credits for tenant {TenantId} by {ChangedBy}", tenantId, changedBy);
        return sub;
    }

    public async Task<TenantSubscription> ChangePlanAsync(
        string tenantId, Guid newPlanId, string changedBy, string? notes, CancellationToken ct = default)
    {
        var sub = await subscriptionRepo.GetByTenantIdAsync(tenantId, ct)
            ?? throw new SubscriptionNotFoundException(tenantId);

        var newPlan = await planRepo.GetByIdAsync(newPlanId, ct)
            ?? throw new InvalidOperationException($"Plan {newPlanId} not found.");

        var previousPlanId = sub.PlanId;
        var balanceBefore = sub.CreditsRemaining;
        var now = DateTimeOffset.UtcNow;

        // Proportional credit carry-over: ratio of remaining credits in current cycle
        int newCredits;
        if (newPlan.IsUnlimited)
        {
            newCredits = int.MaxValue;
        }
        else if (sub.Plan.IsUnlimited || sub.Plan.CreditsPerCycle == 0)
        {
            newCredits = newPlan.CreditsPerCycle;
        }
        else
        {
            var ratio = (double)sub.CreditsRemaining / sub.Plan.CreditsPerCycle;
            newCredits = (int)Math.Round(newPlan.CreditsPerCycle * ratio);
        }

        sub.PlanId = newPlan.Id;
        sub.Plan = newPlan;
        sub.CreditsRemaining = newCredits;
        sub.NextResetDate = newPlan.ResetFrequency == ResetFrequency.Never
            ? null
            : DatabaseSeeder.ComputeNextReset(now, newPlan.ResetFrequency);

        await subscriptionRepo.UpdateAsync(sub, ct);

        await RecordTransactionAsync(sub, newCredits - balanceBefore, TransactionType.PlanChange,
            null, $"Plan change to {newPlan.Name} by {changedBy}. {notes}",
            balanceBefore, sub.CreditsRemaining, ct);

        var history = new SubscriptionHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubscriptionId = sub.Id,
            PreviousPlanId = previousPlanId,
            NewPlanId = newPlanId,
            ChangedAt = now,
            ChangedBy = changedBy,
            Notes = notes,
        };
        // History is saved via navigation but subscription is already tracked; add directly
        sub.History.Add(history);
        await subscriptionRepo.UpdateAsync(sub, ct);

        logger.LogInformation("Changed plan for tenant {TenantId} to {Plan} by {ChangedBy}",
            tenantId, newPlan.Name, changedBy);

        return sub;
    }

    private Task RecordTransactionAsync(
        TenantSubscription sub, int amount, TransactionType type,
        string? referenceId, string description, int balanceBefore, int balanceAfter,
        CancellationToken ct)
    {
        var tx = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = sub.TenantId,
            SubscriptionId = sub.Id,
            Amount = amount,
            TransactionType = type,
            ReferenceId = referenceId,
            Description = description.Length > 500 ? description[..500] : description,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        return creditRepo.AddTransactionAsync(tx, ct);
    }
}
