using System.Text.Json;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostDomain.Blueprints;
using StudioTechBI.AgentHostDomain.Exceptions;

namespace StudioTechBI.AgentHostAI.Parsing;

public sealed class BlueprintResponseParser : IBlueprintResponseParser
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public BlueprintDocument Parse(string rawJson)
    {
        var extracted = BlueprintJsonValidator.ExtractJson(rawJson);

        try
        {
            var doc = JsonSerializer.Deserialize<BlueprintDocument>(extracted, _options);
            if (doc is null)
                throw new BlueprintParseException("Deserialization returned null", extracted);
            return doc;
        }
        catch (JsonException ex)
        {
            throw new BlueprintParseException($"Failed to parse blueprint JSON: {ex.Message}", extracted, ex);
        }
    }
}
