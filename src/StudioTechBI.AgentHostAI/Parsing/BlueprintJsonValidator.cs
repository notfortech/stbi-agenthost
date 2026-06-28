using System.Text.Json;
using StudioTechBI.AgentHostApplication.Abstractions;

namespace StudioTechBI.AgentHostAI.Parsing;

public sealed class BlueprintJsonValidator : IBlueprintJsonValidator
{
    public ValidationReport Validate(string rawJson)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            errors.Add("Response is empty");
            return new ValidationReport(false, errors, warnings);
        }

        var extracted = ExtractJson(rawJson);

        try
        {
            using var doc = JsonDocument.Parse(extracted);
            var root = doc.RootElement;

            if (!root.TryGetProperty("dashboards", out var dashboards))
                errors.Add("Missing required section: dashboards");
            else if (dashboards.GetArrayLength() == 0)
                warnings.Add("No dashboards defined");

            if (!root.TryGetProperty("datasets", out _))
                errors.Add("Missing required section: datasets");

            if (!root.TryGetProperty("metrics", out _))
                errors.Add("Missing required section: metrics");
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
        }

        return new ValidationReport(errors.Count == 0, errors, warnings);
    }

    internal static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
            return raw[start..(end + 1)];
        return raw;
    }
}
