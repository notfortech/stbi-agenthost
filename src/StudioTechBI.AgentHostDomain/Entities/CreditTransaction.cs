using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostDomain.Entities;

/// <summary>
/// Immutable ledger entry for every credit movement.
/// Positive Amount = credits added; negative = credits consumed.
/// </summary>
public sealed class CreditTransaction
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }

    /// <summary>Signed amount: positive = credit added, negative = credit deducted.</summary>
    public int Amount { get; set; }

    public TransactionType TransactionType { get; set; }

    /// <summary>RequestId, admin action ID, or other correlation reference.</summary>
    public string? ReferenceId { get; set; }

    public string Description { get; set; } = string.Empty;
    public int BalanceBefore { get; set; }
    public int BalanceAfter { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public TenantSubscription Subscription { get; set; } = null!;
}
