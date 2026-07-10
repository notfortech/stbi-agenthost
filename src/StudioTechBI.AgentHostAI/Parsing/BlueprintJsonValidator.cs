using System.Text.Json;
using StudioTechBI.AgentHostApplication.Abstractions;

namespace StudioTechBI.AgentHostAI.Parsing;

public sealed class BlueprintJsonValidator : IBlueprintJsonValidator
{
    private static readonly string[] RequiredSections =
    [
        "meta", "detection", "capabilities", "data_model", "measures", "kpis", "pages",
        "executive_questions", "security", "governance", "quality_frameworks",
        "expected_targets", "self_review", "confidence"
    ];

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

            // Required top-level sections per DashboardBlueprintSchema.md's "Mandatory Fields" table.
            foreach (var section in RequiredSections)
            {
                if (!root.TryGetProperty(section, out _))
                    errors.Add($"Missing required section: {section}");
            }

            CheckMinimumCount(root, "data_model", "fact_tables", 2, warnings);
            CheckMinimumCount(root, "data_model", "dimension_tables", 3, warnings);
            CheckMinimumCount(root, "data_model", "relationships", 5, warnings);
            CheckMinimumCount(root, null, "measures", 10, warnings);
            CheckMinimumCount(root, null, "kpis", 6, warnings);
            CheckMinimumCount(root, null, "pages", 5, warnings);
            CheckMinimumCount(root, null, "executive_questions", 8, warnings);

            if (root.TryGetProperty("self_review", out var selfReview)
                && selfReview.TryGetProperty("gates", out var gates)
                && gates.ValueKind == JsonValueKind.Array
                && gates.GetArrayLength() != 9)
            {
                warnings.Add($"self_review.gates has {gates.GetArrayLength()} entries, expected exactly 9.");
            }
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

    private static void CheckMinimumCount(
        JsonElement root, string? parentProperty, string arrayProperty, int minimum, List<string> warnings)
    {
        var container = root;
        if (parentProperty is not null && !root.TryGetProperty(parentProperty, out container))
            return;

        if (!container.TryGetProperty(arrayProperty, out var array) || array.ValueKind != JsonValueKind.Array)
            return;

        var count = array.GetArrayLength();
        if (count < minimum)
        {
            var path = parentProperty is null ? arrayProperty : $"{parentProperty}.{arrayProperty}";
            warnings.Add($"{path} has {count} entries, expected at least {minimum}.");
        }
    }
}
