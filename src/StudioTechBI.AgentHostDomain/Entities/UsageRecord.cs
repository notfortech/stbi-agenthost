using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostDomain.Entities;

/// <summary>Immutable record of one AI generation request. Written once; never updated.</summary>
public sealed class UsageRecord
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }
    public Guid? BlueprintId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long ExecutionTimeMs { get; set; }
    public int TokensUsed { get; set; }
    public int CreditsConsumed { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public UsageStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public TenantSubscription Subscription { get; set; } = null!;
}
