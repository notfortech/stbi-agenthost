using StudioTechBI.AgentHostApplication.Models.Requests;
using StudioTechBI.AgentHostApplication.Models.Responses;

namespace StudioTechBI.AgentHostApplication.Abstractions;

public interface IBlueprintGenerationService
{
    Task<BlueprintResponse> GenerateAsync(BlueprintRequest request, CancellationToken ct = default);
    Task<ValidationReport> ValidateAsync(BlueprintRequest request, CancellationToken ct = default);
}
