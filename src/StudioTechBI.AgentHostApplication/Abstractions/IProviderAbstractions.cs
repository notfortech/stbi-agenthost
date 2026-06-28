using StudioTechBI.AgentHostApplication.Models.Requests;
using StudioTechBI.AgentHostDomain.Blueprints;
using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostApplication.Abstractions;

public interface IBlueprintProviderFactory
{
    IBlueprintProvider Create(ProviderType type);
}

public interface IProviderRouter
{
    IBlueprintProvider Resolve(BlueprintRequest request);
}

public interface IPromptBuilder
{
    Task<PromptBundle> BuildAsync(BlueprintRequest request, CancellationToken ct = default);
}

public interface IPromptResourceProvider
{
    Task<string> GetTemplateAsync(string key, CancellationToken ct = default);
    Task<string> GetKnowledgePackAsync(string industry, CancellationToken ct = default);
}

public interface IBlueprintJsonValidator
{
    ValidationReport Validate(string rawJson);
}

public interface IBlueprintResponseParser
{
    BlueprintDocument Parse(string rawJson);
}

public interface IBlueprintMapper
{
    Models.Responses.BlueprintResponse ToResponse(BlueprintDocument doc, ProviderResult meta, ValidationReport validation, string correlationId, int attempts, bool fallbackUsed);
}

public interface IBlueprintRequestValidator
{
    ValidationReport Validate(BlueprintRequest request);
}

public sealed record ValidationReport(bool IsValid, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);
