using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostAI.Options;

public sealed class AIProviderOptions
{
    public const string SectionName = "AIProviders";

    public ProviderType DefaultProvider { get; init; } = ProviderType.Groq;
    public IReadOnlyList<ProviderType> FallbackOrder { get; init; } = [ProviderType.Groq, ProviderType.OpenAI];
    public OpenAIOptions OpenAI { get; init; } = new();
    public GroqOptions Groq { get; init; } = new();
}

public sealed class OpenAIOptions
{
    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://api.openai.com/v1";
    public string ApiKey { get; init; } = string.Empty;
    public string DefaultModel { get; init; } = "gpt-4o";
    public IReadOnlyList<string> Models { get; init; } = ["gpt-4o", "gpt-4o-mini"];
    public double Temperature { get; init; } = 0.2;
    public int MaxTokens { get; init; } = 4096;
    public int TimeoutSeconds { get; init; } = 60;
}

public sealed class GroqOptions
{
    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://api.groq.com/openai/v1";
    public string ApiKey { get; init; } = string.Empty;
    public string DefaultModel { get; init; } = "llama-3.3-70b-versatile";
    public IReadOnlyList<string> Models { get; init; } = ["llama-3.3-70b-versatile", "llama-3.1-8b-instant"];
    public double Temperature { get; init; } = 0.2;
    public int MaxTokens { get; init; } = 4096;
    public int TimeoutSeconds { get; init; } = 30;
}
