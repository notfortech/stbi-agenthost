using Microsoft.Extensions.Logging;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models.Requests;
using StudioTechBI.AgentHostApplication.Models.Responses;

namespace StudioTechBI.AgentHostApplication.Services;

public sealed class BlueprintGenerationService : IBlueprintGenerationService
{
    private readonly IProviderRouter _router;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IBlueprintJsonValidator _validator;
    private readonly IBlueprintResponseParser _parser;
    private readonly IBlueprintMapper _mapper;
    private readonly IBlueprintRequestValidator _requestValidator;
    private readonly ILogger<BlueprintGenerationService> _logger;

    public BlueprintGenerationService(
        IProviderRouter router,
        IPromptBuilder promptBuilder,
        IBlueprintJsonValidator validator,
        IBlueprintResponseParser parser,
        IBlueprintMapper mapper,
        IBlueprintRequestValidator requestValidator,
        ILogger<BlueprintGenerationService> logger)
    {
        _router = router;
        _promptBuilder = promptBuilder;
        _validator = validator;
        _parser = parser;
        _mapper = mapper;
        _requestValidator = requestValidator;
        _logger = logger;
    }

    public async Task<BlueprintResponse> GenerateAsync(BlueprintRequest request, CancellationToken ct = default)
    {
        var correlationId = request.RequestId.ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        _logger.LogInformation("Step 1: Building prompt | RequestId={RequestId}", correlationId);
        var prompt = await _promptBuilder.BuildAsync(request, ct);
        _logger.LogInformation("Step 2: Prompt built | SystemLen={SystemLen} UserLen={UserLen}", prompt.System.Length, prompt.User.Length);

        _logger.LogInformation("Step 3: Resolving provider");
        var provider = _router.Resolve(request);
        var parameters = new GenerationParameters(request.PreferredModel, request.Temperature, request.MaxTokens);
        _logger.LogInformation("Step 4: Provider resolved | Provider={Provider} Model={Model} Temperature={Temperature} MaxTokens={MaxTokens}",
            provider.Type, parameters.Model ?? "(default)", parameters.Temperature, parameters.MaxTokens);

        _logger.LogInformation("Step 5: Calling provider | Provider={Provider}", provider.Type);
        ProviderResult result;
        try
        {
            result = await provider.GenerateAsync(prompt, parameters, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Step 5 FAILED: Provider call failed | Provider={Provider} | ExType={ExType} | Message={Message} | InnerType={InnerType} | Inner={InnerMessage}",
                provider.Type, ex.GetType().Name, ex.Message,
                ex.InnerException?.GetType().Name, ex.InnerException?.Message);
            throw;
        }

        _logger.LogInformation("Step 6: Provider responded | Provider={Provider} LatencyMs={LatencyMs} Tokens={Tokens} ResponseLength={ResponseLength}",
            provider.Type, result.LatencyMs, result.Tokens.TotalTokens, result.RawJson?.Length ?? 0);

        _logger.LogInformation("Step 7: Validating JSON");
        var validation = _validator.Validate(result.RawJson!);
        _logger.LogInformation("Step 7 done | IsValid={IsValid} Errors={ErrorCount} Warnings={WarnCount}",
            validation.IsValid, validation.Errors.Count, validation.Warnings.Count);

        _logger.LogInformation("Step 8: Parsing and mapping response");
        try
        {
            var document = _parser.Parse(result.RawJson!);
            var response = _mapper.ToResponse(document, result, validation, correlationId, attempts: 1, fallbackUsed: false);
            _logger.LogInformation("Step 8 done: Response mapped successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Step 8 FAILED: Parse/map failed | ExType={ExType} | Message={Message} | RawLength={RawLength} | RawSnippet={Snippet}",
                ex.GetType().Name, ex.Message,
                result.RawJson?.Length ?? 0,
                result.RawJson?.Length > 200 ? result.RawJson[..200] : result.RawJson);
            throw;
        }
    }

    public Task<ValidationReport> ValidateAsync(BlueprintRequest request, CancellationToken ct = default)
    {
        var report = _requestValidator.Validate(request);
        return Task.FromResult(report);
    }
}
