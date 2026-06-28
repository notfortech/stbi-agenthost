using Microsoft.AspNetCore.Mvc;

namespace StudioTechBI.AgentHostAPI.Controllers;

/// <summary>Service version information.</summary>
[ApiController]
[Route("api/version")]
[Produces("application/json")]
public sealed class VersionController : ControllerBase
{
    /// <summary>
    /// Returns the service name and assembly version.
    /// </summary>
    /// <response code="200">Version information.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetVersion()
    {
        var version = typeof(VersionController).Assembly
            .GetName()
            .Version
            ?.ToString(3) ?? "1.0.0";

        return Ok(new
        {
            service = "STBI Agent Host",
            version
        });
    }
}
