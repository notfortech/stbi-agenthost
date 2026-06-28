namespace StudioTechBI.AgentHostDomain.Exceptions;

public sealed class SubscriptionNotFoundException : Exception
{
    public string TenantId { get; }

    public SubscriptionNotFoundException(string tenantId)
        : base($"No active subscription found for tenant '{tenantId}'.")
    {
        TenantId = tenantId;
    }
}
