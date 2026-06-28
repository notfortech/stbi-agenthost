using StudioTechBI.AgentHostDomain.Blueprints;
using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostDomain.ValueObjects;

namespace StudioTechBI.AgentHostApplication.Models.Responses;

public sealed class BlueprintResponse
{
    public Guid BlueprintId { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public TimeSpan GenerationDuration { get; init; }
    public double Confidence { get; init; }
    public BlueprintDocument Blueprint { get; init; } = new();
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public GenerationDiagnostics Diagnostics { get; init; } = new();
}

public sealed class GenerationDiagnostics
{
    public string CorrelationId { get; init; } = string.Empty;
    public TokenUsage Tokens { get; init; } = new(0, 0, 0);
    public int Attempts { get; init; }
    public bool FallbackUsed { get; init; }
    public bool SchemaValid { get; init; }
    public long ProviderLatencyMs { get; init; }
    public string PromptPackVersion { get; init; } = string.Empty;
    public GenerationStatus Status { get; init; }
}
