namespace StudioTechBI.AgentHostApplication.Models;

/// <summary>
/// Top-level agent behaviour settings. Bound from the "Agent" config section.
/// Provider and Model drive default routing; SaveBlueprints enables local persistence.
/// </summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>AI provider to use by default (Claude | OpenAI | Groq).</summary>
    public string Provider { get; init; } = "Claude";

    /// <summary>Model identifier sent to the provider.</summary>
    public string Model { get; init; } = "claude-sonnet-4-6";

    /// <summary>When true, each generated blueprint is written to <see cref="BlueprintFolder"/>.</summary>
    public bool SaveBlueprints { get; init; } = true;

    /// <summary>Relative or absolute path where blueprint JSON files are saved.</summary>
    public string BlueprintFolder { get; init; } = "GeneratedBlueprints";
}
