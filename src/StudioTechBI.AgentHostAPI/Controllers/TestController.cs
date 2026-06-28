using Microsoft.AspNetCore.Mvc;

namespace StudioTechBI.AgentHostAPI.Controllers;

/// <summary>Smoke-test endpoint — confirms the API is reachable.</summary>
[ApiController]
[Route("api/test")]
[Produces("application/json")]
public sealed class TestController : ControllerBase
{
    /// <summary>
    /// Confirms the Agent Host API is running and accepting requests.
    /// No AI provider is called — this is a pure infrastructure smoke test.
    /// </summary>
    /// <response code="200">API is running.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Test()
    {
        return Ok(new
        {
            success = true,
            message = "Agent Host API is running."
        });
    }
}
