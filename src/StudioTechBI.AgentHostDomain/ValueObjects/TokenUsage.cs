namespace StudioTechBI.AgentHostDomain.ValueObjects;

public sealed record TokenUsage(int PromptTokens, int CompletionTokens, int TotalTokens);
