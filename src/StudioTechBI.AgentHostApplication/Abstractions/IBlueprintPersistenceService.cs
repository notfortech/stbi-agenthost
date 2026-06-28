using StudioTechBI.AgentHostDomain.Blueprints;

namespace StudioTechBI.AgentHostApplication.Abstractions;

/// <summary>
/// Persists generated blueprints to a local folder.
/// Enabled/disabled via AgentOptions.SaveBlueprints.
/// </summary>
public interface IBlueprintPersistenceService
{
    /// <summary>
    /// Serialises <paramref name="document"/> to JSON and writes it to the configured folder.
    /// Returns the full file path that was written.
    /// </summary>
    Task<string> SaveAsync(Guid requestId, BlueprintDocument document, CancellationToken ct = default);
}
