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
[Route("api/blueprint")]
[Produces("application/json")]
public sealed class BlueprintsController : ControllerBase
{
    private readonly IBlueprintGenerationService _service;
    private readonly IBlueprintPersistenceService _persistence;
    private readonly IBlueprintPdfService _pdfService;
    private readonly ICreditEngine _creditEngine;
    private readonly IUsageRepository _usageRepository;
    private readonly AgentOptions _agentOptions;
    private readonly ILogger<BlueprintsController> _logger;

    public BlueprintsController(
        IBlueprintGenerationService service,
        IBlueprintPersistenceService persistence,
        IBlueprintPdfService pdfService,
        ICreditEngine creditEngine,
        IUsageRepository usageRepository,
        IOptions<AgentOptions> agentOptions,
        ILogger<BlueprintsController> logger)
    {
        _service = service;
        _persistence = persistence;
        _pdfService = pdfService;
        _creditEngine = creditEngine;
        _usageRepository = usageRepository;
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
        if (string.IsNullOrWhiteSpace(request.BusinessRequirement))
            return BadRequest(new { status = 400, title = "Validation Failed", detail = "businessRequirement is required." });

        if (string.IsNullOrWhiteSpace(request.Industry))
            return BadRequest(new { status = 400, title = "Validation Failed", detail = "industry is required." });

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
        string? pdfDownloadUrl = null;
        if (_agentOptions.SaveBlueprints)
        {
            try
            {
                savedFilePath = await _persistence.SaveAsync(requestId, response.Blueprint, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save blueprint JSON for RequestId={RequestId}", requestId);
            }

            try
            {
                await _pdfService.SaveAsync(requestId, response.Blueprint, ct);
                pdfDownloadUrl = $"{Request.Scheme}://{Request.Host}/api/blueprints/{requestId}/pdf";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save blueprint PDF for RequestId={RequestId}", requestId);
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
            PdfDownloadUrl = pdfDownloadUrl,
            CreditsRemaining = creditResult?.IsUnlimited == true ? null : creditResult?.CreditsRemaining,
            CreditsConsumed = creditResult?.CreditsConsumed,
            ResetDate = creditResult?.ResetDate,
            SubscriptionPlan = creditResult?.PlanName
        };

        return Ok(result);
    }

    /// <summary>Returns the credit balance for a client/tenant.</summary>
    /// <param name="clientCode">Client code used as the tenant identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("credits")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCredits([FromQuery] string clientCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(clientCode))
            return BadRequest(new { status = 400, title = "Validation Failed", detail = "clientCode query parameter is required." });

        var subscription = await _creditEngine.GetOrCreateSubscriptionAsync(clientCode, clientCode, ct);
        await _creditEngine.CheckAndResetIfNeededAsync(subscription, ct);

        return Ok(new
        {
            clientCode,
            plan = subscription.Plan.Name,
            creditsRemaining = subscription.Plan.IsUnlimited ? (int?)null : subscription.CreditsRemaining,
            creditsUsedThisCycle = subscription.CreditsUsedThisCycle,
            isUnlimited = subscription.Plan.IsUnlimited,
            nextResetDate = subscription.NextResetDate,
            currentCycleStart = subscription.CurrentCycleStart
        });
    }

    /// <summary>Returns the generation request history for a client/tenant.</summary>
    /// <param name="clientCode">Client code used as the tenant identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Results per page (max 100).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string clientCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientCode))
            return BadRequest(new { status = 400, title = "Validation Failed", detail = "clientCode query parameter is required." });

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var records = await _usageRepository.GetByTenantAsync(clientCode, page, pageSize, ct);
        var total = await _usageRepository.GetTotalCountAsync(clientCode, ct);

        return Ok(new
        {
            clientCode,
            page,
            pageSize,
            totalCount = total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            items = records.Select(r => new
            {
                requestId = r.RequestId,
                provider = r.Provider,
                model = r.Model,
                tokensUsed = r.TokensUsed,
                creditsConsumed = r.CreditsConsumed,
                executionTimeMs = r.ExecutionTimeMs,
                status = r.Status.ToString(),
                timestamp = r.Timestamp
            })
        });
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

    /// <summary>
    /// Download the PDF report for a previously generated blueprint.
    /// </summary>
    /// <param name="requestId">The request ID returned by the generate endpoint.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PDF file download.</returns>
    /// <response code="200">PDF file stream.</response>
    /// <response code="404">PDF not found — blueprint may not have been saved or PDF generation failed.</response>
    [HttpGet("{requestId:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult DownloadPdf(Guid requestId, CancellationToken ct)
    {
        var filePath = _pdfService.GetFilePath(requestId);

        // GetFilePath uses today's date prefix — scan the folder for any file matching the requestId
        var folder = Path.GetDirectoryName(filePath)!;
        if (Directory.Exists(folder))
        {
            var match = Directory.EnumerateFiles(folder, $"*_{requestId:N}.pdf").FirstOrDefault();
            if (match is not null)
                return PhysicalFile(match, "application/pdf", $"blueprint-{requestId}.pdf", enableRangeProcessing: true);
        }

        return Problem(
            title: "PDF Not Found",
            detail: $"No PDF found for request {requestId}. Either SaveBlueprints is disabled or PDF generation failed.",
            statusCode: StatusCodes.Status404NotFound);
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
