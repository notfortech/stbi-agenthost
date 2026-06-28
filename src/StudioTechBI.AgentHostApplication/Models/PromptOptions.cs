namespace StudioTechBI.AgentHostApplication.Models;

public sealed class PromptOptions
{
    public const string SectionName = "Prompting";

    public string ResourceRoot { get; init; } = "resources";
    public string PromptPackVersion { get; init; } = "2026.06";
    public int CacheTtlSeconds { get; init; } = 300;
    public bool WatchForChanges { get; init; } = true;
}
