namespace StudioTechBI.AgentHostDomain.Enums;

public enum UsageStatus
{
    Succeeded,
    Failed,
    Rejected   // Rejected before AI call (e.g. insufficient credits)
}
