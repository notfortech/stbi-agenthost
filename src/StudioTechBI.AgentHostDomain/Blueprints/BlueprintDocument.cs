using System.Text.Json.Serialization;

namespace StudioTechBI.AgentHostDomain.Blueprints;

public sealed class BlueprintDocument
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = "1.0";

    [JsonPropertyName("overview")]
    public BlueprintOverview Overview { get; init; } = new();

    [JsonPropertyName("dashboards")]
    public IReadOnlyList<DashboardBlueprint> Dashboards { get; init; } = [];

    [JsonPropertyName("datasets")]
    public IReadOnlyList<DatasetBlueprint> Datasets { get; init; } = [];

    [JsonPropertyName("metrics")]
    public IReadOnlyList<MetricBlueprint> Metrics { get; init; } = [];

    [JsonPropertyName("relationships")]
    public IReadOnlyList<RelationshipBlueprint> Relationships { get; init; } = [];

    [JsonPropertyName("recommendations")]
    public IReadOnlyList<string> Recommendations { get; init; } = [];
}

public sealed class BlueprintOverview
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("industry")]
    public string Industry { get; init; } = string.Empty;

    [JsonPropertyName("businessCapability")]
    public string BusinessCapability { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;
}

public sealed class DashboardBlueprint
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("purpose")]
    public string Purpose { get; init; } = string.Empty;

    [JsonPropertyName("targetAudience")]
    public string TargetAudience { get; init; } = string.Empty;

    [JsonPropertyName("pages")]
    public IReadOnlyList<DashboardPage> Pages { get; init; } = [];
}

public sealed class DashboardPage
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("layout")]
    public string Layout { get; init; } = string.Empty;

    [JsonPropertyName("visuals")]
    public IReadOnlyList<VisualSpec> Visuals { get; init; } = [];
}

public sealed class VisualSpec
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("metricRefs")]
    public IReadOnlyList<string> MetricRefs { get; init; } = [];

    [JsonPropertyName("dimensions")]
    public IReadOnlyList<string> Dimensions { get; init; } = [];

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public sealed class DatasetBlueprint
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("grain")]
    public string Grain { get; init; } = string.Empty;

    [JsonPropertyName("sourceSystem")]
    public string SourceSystem { get; init; } = string.Empty;

    [JsonPropertyName("columns")]
    public IReadOnlyList<ColumnSpec> Columns { get; init; } = [];
}

public sealed class ColumnSpec
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

public sealed class MetricBlueprint
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("definition")]
    public string Definition { get; init; } = string.Empty;

    [JsonPropertyName("expression")]
    public string Expression { get; init; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; init; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;
}

public sealed class RelationshipBlueprint
{
    [JsonPropertyName("fromDataset")]
    public string FromDataset { get; init; } = string.Empty;

    [JsonPropertyName("toDataset")]
    public string ToDataset { get; init; } = string.Empty;

    [JsonPropertyName("cardinality")]
    public string Cardinality { get; init; } = string.Empty;

    [JsonPropertyName("keys")]
    public IReadOnlyList<string> Keys { get; init; } = [];
}
