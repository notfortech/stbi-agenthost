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

        _logger.LogInformation("Generating blueprint via {Provider}", provider.Type);

        var result = await provider.GenerateAsync(prompt, parameters, ct);

        _logger.LogInformation("Provider {Provider} responded in {LatencyMs}ms using {Tokens} tokens",
            provider.Type, result.LatencyMs, result.Tokens.TotalTokens);

        var validation = _validator.Validate(result.RawJson);
        var document = _parser.Parse(result.RawJson);

        return _mapper.ToResponse(document, result, validation, correlationId, attempts: 1, fallbackUsed: false);
    }

    public Task<ValidationReport> ValidateAsync(BlueprintRequest request, CancellationToken ct = default)
    {
        var report = _requestValidator.Validate(request);
        return Task.FromResult(report);
    }
}
