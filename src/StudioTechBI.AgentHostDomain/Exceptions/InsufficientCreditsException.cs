namespace StudioTechBI.AgentHostDomain.Exceptions;

public sealed class InsufficientCreditsException : Exception
{
    public string TenantId { get; }
    public int CreditsRemaining { get; }
    public DateTimeOffset? ResetDate { get; }
    public string PlanName { get; }

    public InsufficientCreditsException(string tenantId, int creditsRemaining, DateTimeOffset? resetDate, string planName)
        : base($"Tenant '{tenantId}' has insufficient credits ({creditsRemaining} remaining). " +
               (resetDate.HasValue ? $"Credits reset on {resetDate:O}." : "No automatic reset scheduled."))
    {
        TenantId = tenantId;
        CreditsRemaining = creditsRemaining;
        ResetDate = resetDate;
        PlanName = planName;
    }
}
