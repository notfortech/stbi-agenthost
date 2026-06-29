using StudioTechBI.AgentHostDomain.Blueprints;

namespace StudioTechBI.AgentHostApplication.Abstractions;

/// <summary>
/// Generates a formatted PDF report from a blueprint document and saves it to the configured folder.
/// </summary>
public interface IBlueprintPdfService
{
    /// <summary>
    /// Generates a PDF from <paramref name="document"/> and saves it to disk.
    /// Returns the full file path that was written.
    /// </summary>
    Task<string> SaveAsync(Guid requestId, BlueprintDocument document, CancellationToken ct = default);

    /// <summary>
    /// Returns the expected file path for a given request ID without generating the file.
    /// Used by the download endpoint to locate an existing PDF.
    /// </summary>
    string GetFilePath(Guid requestId);
}
