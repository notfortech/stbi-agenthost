using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostAI.Options;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models.Requests;
using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostDomain.Exceptions;

namespace StudioTechBI.AgentHostAI.Routing;

public sealed class ProviderRouter : IProviderRouter
{
    private readonly IBlueprintProviderFactory _factory;
    private readonly AIProviderOptions _options;
    private readonly ILogger<ProviderRouter> _logger;

    public ProviderRouter(IBlueprintProviderFactory factory, IOptions<AIProviderOptions> options, ILogger<ProviderRouter> logger)
    {
        _factory = factory;
        _options = options.Value;
        _logger = logger;
    }

    public IBlueprintProvider Resolve(BlueprintRequest request)
    {
        var preferred = request.PreferredProvider ?? _options.DefaultProvider;

        if (IsEnabled(preferred))
        {
            try { return _factory.Create(preferred); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to construct provider {Provider}", preferred); }
        }

        foreach (var fallback in _options.FallbackOrder)
        {
            if (fallback == preferred) continue;
            if (!IsEnabled(fallback)) continue;
            try { return _factory.Create(fallback); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to construct fallback provider {Provider}", fallback); }
        }

        throw new ProviderUnavailableException(preferred, "No enabled provider is available");
    }

    private bool IsEnabled(ProviderType type) => type switch
    {
        ProviderType.OpenAI => _options.OpenAI.Enabled,
        ProviderType.Claude => _options.Claude.Enabled,
        ProviderType.Groq   => _options.Groq.Enabled,
        _ => false
    };
}
