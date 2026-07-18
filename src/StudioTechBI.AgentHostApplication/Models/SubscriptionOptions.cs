namespace StudioTechBI.AgentHostApplication.Models;

/// <summary>
/// Default behaviour for credit and subscription management.
/// Bound from "SubscriptionDefaults" config section.
/// </summary>
public sealed class SubscriptionDefaults
{
    public const string SectionName = "SubscriptionDefaults";

    /// <summary>Number of credits consumed per AI generation request.</summary>
    public int CreditsConsumedPerRequest { get; init; } = 1;

    /// <summary>Plan name assigned to newly auto-registered tenants.</summary>
    public string DefaultPlanName { get; init; } = "Trial";

    /// <summary>
    /// Default TenantId when X-Tenant-Id header is absent.
    /// Allows immediate Swagger testing without setting headers.
    /// Set to empty string to enforce the header in production.
    /// </summary>
    public string FallbackTenantId { get; init; } = "demo-tenant";

    /// <summary>When true, unknown tenants are automatically registered on the default plan.</summary>
    public bool AutoRegisterNewTenants { get; init; } = true;

    /// <summary>
    /// Tenant IDs (GUID strings, matching whatever callers send as X-Tenant-Id) that should
    /// always be provisioned on InternalTenantPlanName instead of DefaultPlanName — for known
    /// internal/QA accounts that would otherwise repeatedly exhaust a real-customer-sized Trial
    /// allotment during active testing. Compared case-insensitively. Populate via Azure App
    /// Settings using indexed keys, e.g. SubscriptionDefaults__InternalTenantIds__0=&lt;guid&gt;,
    /// __1=&lt;guid&gt;, etc. — no code change needed to add or remove a tenant.
    /// </summary>
    public HashSet<string> InternalTenantIds { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Plan assigned to tenants in InternalTenantIds, on both first contact and (if they
    /// already have a subscription on a different plan) automatically on next contact.</summary>
    public string InternalTenantPlanName { get; init; } = "Enterprise";

    /// <summary>
    /// Temporary operational override: when true, no tenant is ever blocked or deducted by the
    /// app-side credit ledger (treated the same as Plan.IsUnlimited in the check/deduct paths),
    /// regardless of Plan or CreditsRemaining. Does not affect actual AI provider usage/billing —
    /// only the app's own credit gate. Intended to be flipped back to false once real per-tenant
    /// credit enforcement is wanted again.
    /// </summary>
    public bool BypassCreditLimit { get; init; } = false;
}

/// <summary>
/// Per-plan limits used to seed the database on first run.
/// Bound from "AgentLimits" config section.
/// Live plan values are authoritative in the database after seeding.
/// </summary>
public sealed class AgentLimits
{
    public const string SectionName = "AgentLimits";

    public PlanSeedConfig Trial { get; init; } = new(10, "Monthly", false, 5, 1);
    public PlanSeedConfig Starter { get; init; } = new(100, "Monthly", false, 10, 2);
    public PlanSeedConfig Professional { get; init; } = new(500, "Monthly", false, 30, 5);
    public PlanSeedConfig Enterprise { get; init; } = new(0, "Monthly", true, 100, 20);
}

public sealed record PlanSeedConfig(
    int CreditsPerCycle,
    string ResetFrequency,
    bool IsUnlimited,
    int MaxRequestsPerMinute,
    int MaxConcurrentRequests);

/// <summary>
/// Admin API security and feature settings.
/// Bound from "AdminSettings" config section.
/// AdminApiKey must come from environment variable or Key Vault — never appsettings.json.
/// </summary>
public sealed class AdminSettings
{
    public const string SectionName = "AdminSettings";

    /// <summary>
    /// When non-empty, all /api/admin/* requests must include a matching X-Admin-Key header.
    /// When empty (dev default), admin endpoints are open — do not deploy with empty key.
    /// </summary>
    public string AdminApiKey { get; init; } = string.Empty;

    public bool EnableDetailedUsageLogging { get; init; } = true;
}
