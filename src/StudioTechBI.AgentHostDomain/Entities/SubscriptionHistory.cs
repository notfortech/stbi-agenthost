namespace StudioTechBI.AgentHostDomain.Entities;

/// <summary>Audit trail of every plan change for a tenant subscription.</summary>
public sealed class SubscriptionHistory
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }
    public Guid? PreviousPlanId { get; set; }
    public Guid NewPlanId { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ChangedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // Navigation (no cascade — plans outlive history records)
    public TenantSubscription Subscription { get; set; } = null!;
}
