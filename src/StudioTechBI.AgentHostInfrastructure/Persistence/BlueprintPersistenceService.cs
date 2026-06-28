using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostDomain.Blueprints;

namespace StudioTechBI.AgentHostInfrastructure.Persistence;

/// <summary>
/// Writes blueprint JSON to the local filesystem under AgentOptions.BlueprintFolder.
/// File name: yyyyMMdd_HHmmss_{requestId}.json
/// </summary>
public sealed class BlueprintPersistenceService : IBlueprintPersistenceService
{
    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AgentOptions _options;
    private readonly ILogger<BlueprintPersistenceService> _logger;

    public BlueprintPersistenceService(
        IOptions<AgentOptions> options,
        ILogger<BlueprintPersistenceService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Guid requestId, BlueprintDocument document, CancellationToken ct = default)
    {
        var folder = Path.IsPathRooted(_options.BlueprintFolder)
            ? _options.BlueprintFolder
            : Path.Combine(AppContext.BaseDirectory, _options.BlueprintFolder);

        Directory.CreateDirectory(folder);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"{timestamp}_{requestId:N}.json";
        var filePath = Path.Combine(folder, fileName);

        var json = JsonSerializer.Serialize(document, _writeOptions);
        await File.WriteAllTextAsync(filePath, json, ct);

        _logger.LogInformation("Blueprint saved to {FilePath}", filePath);
        return filePath;
    }
}
