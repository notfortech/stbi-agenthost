using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostAI.Options;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models.Requests;
using StudioTechBI.AgentHostDomain.Exceptions;

namespace StudioTechBI.AgentHostAI.Routing;

public sealed class ProviderRouter : IProviderRouter
{
    private readonly IBlueprintProviderFactory _factory;
    private readonly AIProviderOptions _options;

    public ProviderRouter(IBlueprintProviderFactory factory, IOptions<AIProviderOptions> options)
    {
        _factory = factory;
        _options = options.Value;
    }

    public IBlueprintProvider Resolve(BlueprintRequest request)
    {
        var preferred = request.PreferredProvider ?? _options.DefaultProvider;

        try
        {
            return _factory.Create(preferred);
        }
        catch
        {
            foreach (var fallback in _options.FallbackOrder)
            {
                if (fallback == preferred) continue;
                try { return _factory.Create(fallback); } catch { }
            }

            throw new ProviderUnavailableException(preferred, "All providers are unavailable");
        }
    }
}
