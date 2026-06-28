using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace StudioTechBI.AgentHostInfrastructure.Health;

public sealed class ProviderHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;

    public ProviderHealthCheck(IConfiguration config)
    {
        _config = config;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var openAiEnabled = _config.GetValue<bool>("AIProviders:OpenAI:Enabled");
        var groqEnabled = _config.GetValue<bool>("AIProviders:Groq:Enabled");
        var openAiKey = _config["AIProviders:OpenAI:ApiKey"] ?? string.Empty;
        var groqKey = _config["AIProviders:Groq:ApiKey"] ?? string.Empty;
        var defaultProvider = _config["AIProviders:DefaultProvider"] ?? "Groq";

        var data = new Dictionary<string, object>
        {
            ["defaultProvider"] = defaultProvider,
            ["openAiEnabled"] = openAiEnabled,
            ["groqEnabled"] = groqEnabled
        };

        var anyEnabled = openAiEnabled || groqEnabled;
        var hasKeys = (!openAiEnabled || !string.IsNullOrEmpty(openAiKey))
                   && (!groqEnabled || !string.IsNullOrEmpty(groqKey));

        if (!anyEnabled)
            return Task.FromResult(HealthCheckResult.Unhealthy("No providers enabled", data: data));

        if (!hasKeys)
            return Task.FromResult(HealthCheckResult.Degraded("One or more provider API keys missing", data: data));

        return Task.FromResult(HealthCheckResult.Healthy("Providers configured", data));
    }
}
