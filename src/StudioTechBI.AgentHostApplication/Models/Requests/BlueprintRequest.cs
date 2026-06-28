using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostApplication.Models.Requests;

public sealed class BlueprintRequest
{
    public Guid RequestId { get; init; } = Guid.NewGuid();
    public string Industry { get; init; } = string.Empty;
    public string BusinessCapability { get; init; } = string.Empty;
    public string BusinessGoal { get; init; } = string.Empty;
    public string BusinessRequirements { get; init; } = string.Empty;
    public IReadOnlyList<SourceSystemDto>? SourceSystems { get; init; }
    public IReadOnlyList<DatasetMetadataDto>? DatasetMetadata { get; init; }
    public ProviderType? PreferredProvider { get; init; }
    public string? PreferredModel { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public OutputFormat OutputFormat { get; init; } = OutputFormat.Json;
}

public sealed class SourceSystemDto
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? ConnectionHint { get; init; }
}

public sealed class DatasetMetadataDto
{
    public string Name { get; init; } = string.Empty;
    public string? Grain { get; init; }
    public IReadOnlyList<ColumnMetadataDto> Columns { get; init; } = [];
}

public sealed class ColumnMetadataDto
{
    public string Name { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string? SampleValues { get; init; }
}
