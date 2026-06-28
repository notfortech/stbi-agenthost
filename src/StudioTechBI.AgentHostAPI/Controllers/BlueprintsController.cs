using Microsoft.AspNetCore.Mvc;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models.Requests;
using StudioTechBI.AgentHostApplication.Models.Responses;

namespace StudioTechBI.AgentHostAPI.Controllers;

[ApiController]
[Route("api/blueprints")]
public sealed class BlueprintsController : ControllerBase
{
    private readonly IBlueprintGenerationService _service;

    public BlueprintsController(IBlueprintGenerationService service)
    {
        _service = service;
    }

    /// <summary>Generate an analytics blueprint from requirements.</summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(BlueprintResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status424FailedDependency)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Generate([FromBody] BlueprintRequest request, CancellationToken ct)
    {
        var response = await _service.GenerateAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Validate a blueprint request without spending tokens.</summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationReport), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validate([FromBody] BlueprintRequest request, CancellationToken ct)
    {
        var report = await _service.ValidateAsync(request, ct);
        return Ok(report);
    }
}
