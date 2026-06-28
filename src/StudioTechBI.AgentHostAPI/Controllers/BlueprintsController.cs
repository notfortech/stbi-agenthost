using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostApplication.Models.Requests;
using StudioTechBI.AgentHostApplication.Models.Responses;
using StudioTechBI.AgentHostAPI.Middleware;
using StudioTechBI.AgentHostDomain.Entities;
using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostAPI.Controllers;

/// <summary>Blueprint generation endpoints.</summary>
[ApiController]
[Route("api/blueprints")]
[Produces("application/json")]
public sealed class BlueprintsController : ControllerBase
{
    private readonly IBlueprintGenerationService _service;
    private readonly IBlueprintPersistenceService _persistence;
    private readonly ICreditEngine _creditEngine;
    private readonly AgentOptions _agentOptions;
    private readonly ILogger<BlueprintsController> _logger;

    public BlueprintsController(
        IBlueprintGenerationService service,
        IBlueprintPersistenceService persistence,
        ICreditEngine creditEngine,
        IOptions<AgentOptions> agentOptions,
        ILogger<BlueprintsController> logger)
    {
        _service = service;
        _persistence = persistence;
        _creditEngine = creditEngine;
        _agentOptions = agentOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Generate an Analytics Deployment Contract blueprint from business requirements.
    /// </summary>
    /// <remarks>
    /// Calls the configured AI provider, validates the output, and returns a strongly-typed blueprint.
    /// When <c>SaveBlueprints</c> is enabled in configuration the blueprint is also saved locally as JSON.
    ///
    /// Example request:
    /// <code>
    /// {
    ///   "businessRequirement": "Create an Executive Dashboard for Property Management.",
    ///   "industry": "Property Management",
    ///   "existingSchema": null
    /// }
    /// </code>
    /// </remarks>
    /// <param name="request">Business context for blueprint generation.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>Wrapped blueprint generation result including the Analytics Deployment Contract.</returns>
    /// <response code="200">Blueprint generated successfully.</response>
    /// <response code="400">Request validation failed.</response>
    /// <response code="402">Tenant has insufficient credits to process this request.</response>
    /// <response code="408">Request timed out waiting for provider.</response>
    /// <response code="424">AI provider unavailable after all fallbacks exhausted.</response>
    /// <response code="502">Provider returned a response that could not be parsed as a blueprint.</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(BlueprintGenerationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status424FailedDependency)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateBlueprintRequest request,
        CancellationToken ct)
    {
        var requestId = Guid.NewGuid();
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Blueprint generation started | RequestId={RequestId} | Industry={Industry} | Provider={Provider} | Model={Model}",
            requestId, request.Industry, _agentOptions.Provider, _agentOptions.Model);

        // Map simple public request → internal full request, honouring AgentOptions defaults.
        var providerType = Enum.TryParse<ProviderType>(_agentOptions.Provider, ignoreCase: true, out var pt)
            ? pt
            : ProviderType.Claude;

        var internalRequest = new BlueprintRequest
        {
            RequestId = requestId,
            Industry = request.Industry,
            BusinessCapability = request.Industry,    // default to industry when not separately provided
            BusinessGoal = request.BusinessRequirement,
            BusinessRequirements = BuildFullRequirements(request),
            PreferredProvider = providerType,
            PreferredModel = _agentOptions.Model
        };

        var response = await _service.GenerateAsync(internalRequest, ct);
        sw.Stop();

        _logger.LogInformation(
            "Blueprint generation completed | RequestId={RequestId} | Status={Status} | Provider={Provider} | Model={Model} | ElapsedMs={ElapsedMs} | Tokens={Tokens}",
            requestId,
            response.Diagnostics.Status,
            response.Provider,
            response.Model,
            sw.ElapsedMilliseconds,
            response.Diagnostics.Tokens.TotalTokens);

        string? savedFilePath = null;
        if (_agentOptions.SaveBlueprints)
        {
            try
            {
                savedFilePath = await _persistence.SaveAsync(requestId, response.Blueprint, ct);
            }
            catch (Exception ex)
            {
                // Non-fatal — log and continue; don't fail the response over a disk write.
                _logger.LogWarning(ex, "Failed to save blueprint for RequestId={RequestId}", requestId);
            }
        }

        // Deduct credits for the successful generation — subscription was pre-loaded by middleware.
        CreditDeductionResult? creditResult = null;
        if (HttpContext.Items[CreditValidationMiddleware.SubscriptionItemKey] is TenantSubscription subscription)
        {
            try
            {
                creditResult = await _creditEngine.DeductAsync(
                    subscription,
                    requestId,
                    Guid.NewGuid(),
                    response.Diagnostics.Tokens.TotalTokens,
                    sw.ElapsedMilliseconds,
                    response.Provider,
                    response.Model,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Credit deduction failed for RequestId={RequestId} — continuing without deduction", requestId);
            }
        }

        var result = new BlueprintGenerationResult
        {
            RequestId = requestId,
            Status = response.Diagnostics.Status switch
            {
                GenerationStatus.Succeeded => "Completed",
                GenerationStatus.PartiallyValid => "PartiallyValid",
                _ => "Failed"
            },
            Provider = response.Provider,
            Model = response.Model,
            ProcessingTimeMs = sw.ElapsedMilliseconds,
            Confidence = (int)(response.Confidence * 100),
            Blueprint = response.Blueprint,
            Warnings = response.Warnings,
            SavedFilePath = savedFilePath,
            CreditsRemaining = creditResult?.IsUnlimited == true ? null : creditResult?.CreditsRemaining,
            CreditsConsumed = creditResult?.CreditsConsumed,
            ResetDate = creditResult?.ResetDate,
            SubscriptionPlan = creditResult?.PlanName
        };

        return Ok(result);
    }

    /// <summary>
    /// Dry-run validation — checks the request without calling the AI provider or spending tokens.
    /// </summary>
    /// <param name="request">Request to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation report with errors and warnings.</returns>
    /// <response code="200">Validation report (may contain errors).</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> Validate([FromBody] BlueprintRequest request, CancellationToken ct)
    {
        var report = await _service.ValidateAsync(request, ct);
        return Ok(report);
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private static string BuildFullRequirements(GenerateBlueprintRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ExistingSchema))
            return request.BusinessRequirement;

        return $"""
                {request.BusinessRequirement}

                Existing Schema (column names and types only — no sample data):
                {request.ExistingSchema}
                """;
    }
}
