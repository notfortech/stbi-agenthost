using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostDomain.ValueObjects;

public sealed record ModelDescriptor(ProviderType Provider, string ModelId);
