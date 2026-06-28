namespace StudioTechBI.AgentHostApplication.Models.Requests;

/// <summary>
/// Simplified public-facing input for the blueprint generation endpoint.
/// Maps internally to <see cref="BlueprintRequest"/> before service invocation.
/// </summary>
public sealed class GenerateBlueprintRequest
{
    /// <summary>Describe what the dashboards need to achieve.</summary>
    /// <example>Create an Executive Dashboard for Property Management.</example>
    public string BusinessRequirement { get; init; } = string.Empty;

    /// <summary>Vertical or sector context (e.g. "Property Management", "Retail").</summary>
    /// <example>Property Management</example>
    public string Industry { get; init; } = string.Empty;

    /// <summary>
    /// Optional JSON string describing an existing data schema.
    /// Column names and types are safe to include; do not include sample row values.
    /// </summary>
    /// <example>null</example>
    public string? ExistingSchema { get; init; }
}
