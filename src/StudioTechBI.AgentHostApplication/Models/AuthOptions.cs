namespace StudioTechBI.AgentHostApplication.Models;

/// <summary>
/// Shared-secret API key that Koru sends as the `X-Api-Key` header (see ApiKeyAuthMiddleware).
/// Must be set via environment variable or Azure Key Vault in production — never in appsettings.json.
/// When empty, key validation is bypassed (dev/internal-VNet mode only).
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string ApiKey { get; init; } = string.Empty;
}
