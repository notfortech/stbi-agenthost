using StudioTechBI.AgentHostDomain.Blueprints;

namespace StudioTechBI.AgentHostApplication.Models.Responses;

/// <summary>
/// Wrapper returned by POST /api/blueprints/generate.
/// Contains diagnostics alongside the Analytics Deployment Contract blueprint.
/// </summary>
public sealed class BlueprintGenerationResult
{
    /// <summary>Unique ID for this generation request — use for support tracing.</summary>
    /// <example>b6c1d9f5-7d12-4a1d-a3b7-4c6e2c1f8abc</example>
    public Guid RequestId { get; init; }

    /// <summary>Generation outcome: Completed | Failed | PartiallyValid</summary>
    /// <example>Completed</example>
    public string Status { get; init; } = "Completed";

    /// <summary>Provider that served the request.</summary>
    /// <example>Claude</example>
    public string Provider { get; init; } = string.Empty;

    /// <summary>Model ID used.</summary>
    /// <example>claude-sonnet-4-6</example>
    public string Model { get; init; } = string.Empty;

    /// <summary>End-to-end processing time in milliseconds.</summary>
    /// <example>8421</example>
    public long ProcessingTimeMs { get; init; }

    /// <summary>Blueprint confidence score 0–100.</summary>
    /// <example>84</example>
    public int Confidence { get; init; }

    /// <summary>The Analytics Deployment Contract (ADC) blueprint.</summary>
    public BlueprintDocument Blueprint { get; init; } = new();

    /// <summary>Non-fatal warnings from schema validation.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Optional path to the saved blueprint file (only when SaveBlueprints=true).</summary>
    public string? SavedFilePath { get; init; }

    // ── Subscription / credit fields ───────────────────────────────────────────

    /// <summary>Credits remaining in the tenant's current cycle after this request.</summary>
    /// <example>499</example>
    public int? CreditsRemaining { get; init; }

    /// <summary>Credits consumed by this request.</summary>
    /// <example>1</example>
    public int? CreditsConsumed { get; init; }

    /// <summary>UTC date-time when the credit cycle resets. Null for unlimited or never-reset plans.</summary>
    public DateTimeOffset? ResetDate { get; init; }

    /// <summary>Subscription plan name for the tenant that made this request.</summary>
    /// <example>Professional</example>
    public string? SubscriptionPlan { get; init; }
}
