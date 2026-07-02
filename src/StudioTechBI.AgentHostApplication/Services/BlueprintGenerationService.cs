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

        var prompt = await _promptBuilder.BuildAsync(request, ct);
        var provider = _router.Resolve(request);
        var parameters = new GenerationParameters(request.PreferredModel, request.Temperature, request.MaxTokens);

        _logger.LogInformation(
            "Calling provider={Provider} model={Model} systemLen={SystemLen} userLen={UserLen}",
            provider.Type, parameters.Model ?? "(default)", prompt.System.Length, prompt.User.Length);

        ProviderResult result;
        try
        {
            result = await provider.GenerateAsync(prompt, parameters, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Provider call failed | Provider={Provider} | ExType={ExType} | Message={Message} | InnerType={InnerType} | Inner={InnerMessage}",
                provider.Type, ex.GetType().Name, ex.Message,
                ex.InnerException?.GetType().Name, ex.InnerException?.Message);
            throw;
        }

        _logger.LogInformation(
            "Provider {Provider} responded | LatencyMs={LatencyMs} | Tokens={Tokens} | ResponseLength={ResponseLength}",
            provider.Type, result.LatencyMs, result.Tokens.TotalTokens, result.RawJson?.Length ?? 0);

        var validation = _validator.Validate(result.RawJson!);
        _logger.LogInformation(
            "JSON validation | IsValid={IsValid} | Errors={ErrorCount} | Warnings={WarnCount}",
            validation.IsValid, validation.Errors.Count, validation.Warnings.Count);

        try
        {
            var document = _parser.Parse(result.RawJson);
            return _mapper.ToResponse(document, result, validation, correlationId, attempts: 1, fallbackUsed: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Parse/map failed | ExType={ExType} | Message={Message} | RawLength={RawLength} | RawSnippet={Snippet}",
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
