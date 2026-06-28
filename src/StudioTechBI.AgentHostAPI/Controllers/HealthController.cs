using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace StudioTechBI.AgentHostAPI.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthChecks;

    public HealthController(HealthCheckService healthChecks)
    {
        _healthChecks = healthChecks;
    }

    /// <summary>Liveness + readiness check.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var report = await _healthChecks.CheckHealthAsync(ct);
        var result = new
        {
            status = report.Status.ToString(),
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), description = e.Value.Description, data = e.Value.Data })
        };

        return report.Status == HealthStatus.Unhealthy
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, result)
            : Ok(result);
    }
}
