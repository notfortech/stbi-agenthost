using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostDomain.Exceptions;

public sealed class ProviderUnavailableException : Exception
{
    public ProviderType Provider { get; }

    public ProviderUnavailableException(ProviderType provider, string message, Exception? inner = null)
        : base(message, inner)
    {
        Provider = provider;
    }
}
