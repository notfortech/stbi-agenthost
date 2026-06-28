namespace StudioTechBI.AgentHostDomain.Entities;

/// <summary>
/// Represents an organisation's (tenant's) active subscription.
/// Credits are pooled at this level — all users in the org share the credit balance.
/// One active subscription per tenant at any given time.
/// </summary>
public sealed class TenantSubscription
{
    public Guid Id { get; set; }

    /// <summary>Opaque organisation identifier (e.g. GUID from the calling system or slug).</summary>
    public string TenantId { get; set; } = string.Empty;

    public string TenantName { get; set; } = string.Empty;

    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    /// <summary>Current credit balance for this org. Ignored when Plan.IsUnlimited=true.</summary>
    public int CreditsRemaining { get; set; }

    public int CreditsUsedThisCycle { get; set; }

    public DateTimeOffset CurrentCycleStart { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Null when ResetFrequency=Never or plan is unlimited.</summary>
    public DateTimeOffset? NextResetDate { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<UsageRecord> UsageRecords { get; set; } = [];
    public ICollection<CreditTransaction> CreditTransactions { get; set; } = [];
    public ICollection<SubscriptionHistory> History { get; set; } = [];
}
