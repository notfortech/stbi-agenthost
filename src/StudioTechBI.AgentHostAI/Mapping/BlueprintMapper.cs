using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models.Responses;
using StudioTechBI.AgentHostDomain.Blueprints;
using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostAI.Mapping;

public sealed class BlueprintMapper : IBlueprintMapper
{
    public BlueprintResponse ToResponse(
        BlueprintDocument doc,
        ProviderResult meta,
        ValidationReport validation,
        string correlationId,
        int attempts,
        bool fallbackUsed)
    {
        var status = !validation.IsValid
            ? GenerationStatus.Failed
            : validation.Warnings.Count > 0
                ? GenerationStatus.PartiallyValid
                : GenerationStatus.Succeeded;

        return new BlueprintResponse
        {
            BlueprintId = Guid.NewGuid(),
            Provider = meta.Model.Provider.ToString(),
            Model = meta.Model.ModelId,
            GenerationDuration = TimeSpan.FromMilliseconds(meta.LatencyMs),
            Confidence = status == GenerationStatus.Succeeded ? 0.9 : 0.6,
            Blueprint = doc,
            Warnings = validation.Warnings,
            Diagnostics = new GenerationDiagnostics
            {
                CorrelationId = correlationId,
                Tokens = meta.Tokens,
                Attempts = attempts,
                FallbackUsed = fallbackUsed,
                SchemaValid = validation.IsValid,
                ProviderLatencyMs = meta.LatencyMs,
                PromptPackVersion = string.Empty,
                Status = status
            }
        };
    }
}
