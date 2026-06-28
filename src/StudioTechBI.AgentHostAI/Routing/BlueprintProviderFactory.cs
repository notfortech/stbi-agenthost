using Microsoft.Extensions.DependencyInjection;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostDomain.Exceptions;

namespace StudioTechBI.AgentHostAI.Routing;

public sealed class BlueprintProviderFactory : IBlueprintProviderFactory
{
    private readonly IServiceProvider _services;

    public BlueprintProviderFactory(IServiceProvider services)
    {
        _services = services;
    }

    public IBlueprintProvider Create(ProviderType type)
    {
        var provider = _services.GetKeyedService<IBlueprintProvider>(type)
            ?? throw new ProviderUnavailableException(type, $"No provider registered for {type}");
        return provider;
    }
}
