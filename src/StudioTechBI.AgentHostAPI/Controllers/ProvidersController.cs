using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostAI.Options;

namespace StudioTechBI.AgentHostAPI.Controllers;

[ApiController]
[Route("api/providers")]
public sealed class ProvidersController : ControllerBase
{
    private readonly AIProviderOptions _options;

    public ProvidersController(IOptions<AIProviderOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>List configured providers and their available models.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProviderInfo>), StatusCodes.Status200OK)]
    public IActionResult GetProviders()
    {
        var providers = new List<ProviderInfo>();

        if (_options.OpenAI.Enabled)
            providers.Add(new ProviderInfo("OpenAI", _options.OpenAI.DefaultModel, _options.OpenAI.Models, _options.DefaultProvider.ToString() == "OpenAI", true));

        if (_options.Groq.Enabled)
            providers.Add(new ProviderInfo("Groq", _options.Groq.DefaultModel, _options.Groq.Models, _options.DefaultProvider.ToString() == "Groq", true));

        return Ok(providers);
    }
}

public sealed record ProviderInfo(string Name, string DefaultModel, IReadOnlyList<string> Models, bool IsDefault, bool Enabled);
