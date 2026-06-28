using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostDomain.Entities;

/// <summary>
/// Subscription plan definition. Plans are system-wide; tenants subscribe to a plan.
/// Credits belong to the tenant (org), not individual users.
/// </summary>
public sealed class Plan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlanType PlanType { get; set; }

    /// <summary>Credits allocated per reset cycle. 0 = blocked; only meaningful when IsUnlimited=false.</summary>
    public int CreditsPerCycle { get; set; }

    /// <summary>When true CreditsPerCycle is ignored and all requests are allowed through.</summary>
    public bool IsUnlimited { get; set; }

    public ResetFrequency ResetFrequency { get; set; } = ResetFrequency.Monthly;
    public int MaximumRequestsPerMinute { get; set; } = 10;
    public int MaximumConcurrentRequests { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<TenantSubscription> Subscriptions { get; set; } = [];
}
