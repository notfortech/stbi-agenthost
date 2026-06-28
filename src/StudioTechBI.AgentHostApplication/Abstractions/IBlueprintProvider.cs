using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostDomain.ValueObjects;

namespace StudioTechBI.AgentHostApplication.Abstractions;

public interface IBlueprintProvider
{
    ProviderType Type { get; }
    Task<ProviderResult> GenerateAsync(PromptBundle prompt, GenerationParameters parameters, CancellationToken ct = default);
}

public sealed record PromptBundle(string System, string User, string PromptPackVersion);
public sealed record GenerationParameters(string? Model, double? Temperature, int? MaxTokens);
public sealed record ProviderResult(string RawJson, ModelDescriptor Model, TokenUsage Tokens, long LatencyMs);
