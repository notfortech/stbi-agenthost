using System.Text;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models.Requests;

namespace StudioTechBI.AgentHostAI.Prompting;

public sealed class PromptBuilder : IPromptBuilder
{
    private readonly IPromptResourceProvider _resources;
    private readonly PromptOptions _options;

    public PromptBuilder(IPromptResourceProvider resources, IOptions<PromptOptions> options)
    {
        _resources = resources;
        _options = options.Value;
    }

    public async Task<PromptBundle> BuildAsync(BlueprintRequest request, CancellationToken ct = default)
    {
        var systemTemplate = await _resources.GetTemplateAsync("blueprint.system", ct);
        var userTemplate = await _resources.GetTemplateAsync("blueprint.user", ct);
        var knowledgePack = await _resources.GetKnowledgePackAsync(request.Industry, ct);

        var system = systemTemplate.Replace("{{knowledge_pack}}", knowledgePack);
        var user = BuildUserPrompt(userTemplate, request);

        return new PromptBundle(system, user, _options.PromptPackVersion);
    }

    private static string BuildUserPrompt(string template, BlueprintRequest request)
    {
        var sb = new StringBuilder(template);
        sb.Replace("{{industry}}", request.Industry);
        sb.Replace("{{business_capability}}", request.BusinessCapability);
        sb.Replace("{{business_goal}}", request.BusinessGoal);
        sb.Replace("{{business_requirements}}", request.BusinessRequirements);

        if (request.SourceSystems?.Count > 0)
        {
            var systems = string.Join("\n", request.SourceSystems.Select(s => $"- {s.Name} ({s.Type})"));
            sb.Replace("{{source_systems}}", systems);
        }
        else
        {
            sb.Replace("{{source_systems}}", "Not specified");
        }

        if (request.DatasetMetadata?.Count > 0)
        {
            var datasets = string.Join("\n", request.DatasetMetadata.Select(d =>
                $"- {d.Name} (grain: {d.Grain ?? "unknown"}): {string.Join(", ", d.Columns.Select(c => $"{c.Name}:{c.DataType}"))}"));
            sb.Replace("{{dataset_metadata}}", datasets);
        }
        else
        {
            sb.Replace("{{dataset_metadata}}", "Not specified");
        }

        return sb.ToString();
    }
}
