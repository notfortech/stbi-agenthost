using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models;

namespace StudioTechBI.AgentHostInfrastructure.Prompting;

public sealed class FilePromptResourceProvider : IPromptResourceProvider
{
    private readonly string _resourceRoot;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;
    private readonly ILogger<FilePromptResourceProvider> _logger;

    public FilePromptResourceProvider(
        IOptions<PromptOptions> options,
        IMemoryCache cache,
        ILogger<FilePromptResourceProvider> logger)
    {
        _resourceRoot = options.Value.ResourceRoot;
        _cache = cache;
        _ttl = TimeSpan.FromSeconds(options.Value.CacheTtlSeconds);
        _logger = logger;
    }

    public Task<string> GetTemplateAsync(string key, CancellationToken ct = default)
    {
        var cacheKey = $"template:{key}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return Task.FromResult(cached!);

        var path = Path.Combine(_resourceRoot, "prompts", $"{key}.md");
        if (!File.Exists(path))
        {
            _logger.LogWarning("Prompt template not found at {Path}, using fallback", path);
            return Task.FromResult(GetFallbackTemplate(key));
        }

        var content = File.ReadAllText(path);
        _cache.Set(cacheKey, content, _ttl);
        return Task.FromResult(content);
    }

    public Task<string> GetKnowledgePackAsync(string industry, CancellationToken ct = default)
    {
        var cacheKey = $"knowledge:{industry}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return Task.FromResult(cached!);

        var normalized = industry.ToLowerInvariant().Replace(" ", "-");
        var path = Path.Combine(_resourceRoot, "knowledge", "industry-packs", $"{normalized}.json");

        if (!File.Exists(path))
        {
            _logger.LogWarning("No knowledge pack for industry '{Industry}', using default", industry);
            path = Path.Combine(_resourceRoot, "knowledge", "industry-packs", "default.json");
        }

        var content = File.Exists(path) ? File.ReadAllText(path) : "{}";
        _cache.Set(cacheKey, content, _ttl);
        return Task.FromResult(content);
    }

    private static string GetFallbackTemplate(string key) => key switch
    {
        "blueprint.system" =>
            """
            You are an expert BI architect. Generate a comprehensive analytics blueprint as valid JSON.
            The JSON must include: schemaVersion, overview, dashboards, datasets, metrics, relationships, recommendations.
            `recommendations` MUST be a flat array of plain strings, one complete sentence per recommendation.
            Never nest an object inside `recommendations` — no category/priority/rationale sub-fields.
            `metricRefs`, `dimensions` (inside visuals), and `keys` (inside relationships) MUST likewise be
            flat arrays of plain strings — never objects.
            {{knowledge_pack}}
            """,
        "blueprint.user" =>
            """
            Generate a BI analytics blueprint for the following requirements:
            Industry: {{industry}}
            Business Capability: {{business_capability}}
            Business Goal: {{business_goal}}
            Requirements: {{business_requirements}}
            Source Systems: {{source_systems}}
            Dataset Metadata: {{dataset_metadata}}
            Return only valid JSON.
            """,
        _ => string.Empty
    };
}
