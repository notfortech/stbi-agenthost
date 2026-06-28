using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Models;

namespace StudioTechBI.AgentHostAPI.Controllers;

/// <summary>Service health and readiness.</summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthChecks;
    private readonly AgentOptions _agentOptions;

    public HealthController(HealthCheckService healthChecks, IOptions<AgentOptions> agentOptions)
    {
        _healthChecks = healthChecks;
        _agentOptions = agentOptions.Value;
    }

    /// <summary>
    /// Liveness and readiness check.
    /// Returns 200 when healthy or degraded, 503 when unhealthy.
    /// </summary>
    /// <response code="200">Service is healthy or degraded.</response>
    /// <response code="503">Service is unhealthy.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var report = await _healthChecks.CheckHealthAsync(ct);

        var result = new
        {
            status = report.Status == HealthStatus.Healthy ? "Healthy"
                   : report.Status == HealthStatus.Degraded ? "Degraded"
                   : "Unhealthy",
            provider = _agentOptions.Provider,
            version = GetVersion()
        };

        return report.Status == HealthStatus.Unhealthy
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, result)
            : Ok(result);
    }

    private static string GetVersion() =>
        typeof(HealthController).Assembly
            .GetName()
            .Version
            ?.ToString(3) ?? "1.0.0";
}
