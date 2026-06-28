using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostAI.Options;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostDomain.Exceptions;
using StudioTechBI.AgentHostDomain.ValueObjects;

namespace StudioTechBI.AgentHostAI.Providers.Claude;

/// <summary>
/// Calls the Anthropic Messages API. Body shape differs from OpenAI-compatible providers:
/// system prompt is a root field, not a message; response is in content[].text.
/// </summary>
public sealed class ClaudeBlueprintProvider : IBlueprintProvider
{
    private readonly HttpClient _http;
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeBlueprintProvider> _logger;

    public ProviderType Type => ProviderType.Claude;

    public ClaudeBlueprintProvider(
        HttpClient http,
        IOptions<AIProviderOptions> options,
        ILogger<ClaudeBlueprintProvider> logger)
    {
        _http = http;
        _options = options.Value.Claude;
        _logger = logger;
    }

    public async Task<ProviderResult> GenerateAsync(
        PromptBundle prompt,
        GenerationParameters parameters,
        CancellationToken ct = default)
    {
        var model = parameters.Model ?? _options.DefaultModel;
        var temperature = parameters.Temperature ?? _options.Temperature;
        var maxTokens = parameters.MaxTokens ?? _options.MaxTokens;

        // Anthropic requires the system prompt to return JSON — embed instruction in system content.
        var systemContent = prompt.System +
            "\n\nIMPORTANT: Respond with ONLY a valid JSON object. No markdown fences, no prose.";

        var body = new
        {
            model,
            max_tokens = maxTokens,
            temperature,
            system = systemContent,
            messages = new[]
            {
                new { role = "user", content = prompt.User }
            }
        };

        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _http.PostAsync("/v1/messages", content, ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Claude returned {Status}: {Error}", response.StatusCode, error);
                throw new ProviderUnavailableException(ProviderType.Claude, $"Claude returned {(int)response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);

            // Anthropic: { content: [{type:"text", text:"..."}], usage: {input_tokens, output_tokens} }
            var rawText = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            var usage = doc.RootElement.GetProperty("usage");
            var inputTokens = usage.GetProperty("input_tokens").GetInt32();
            var outputTokens = usage.GetProperty("output_tokens").GetInt32();
            var tokens = new TokenUsage(inputTokens, outputTokens, inputTokens + outputTokens);

            _logger.LogInformation(
                "Claude responded in {LatencyMs}ms | model={Model} | tokens={Total}",
                sw.ElapsedMilliseconds, model, tokens.TotalTokens);

            return new ProviderResult(
                rawText,
                new ModelDescriptor(ProviderType.Claude, model),
                tokens,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not ProviderUnavailableException)
        {
            sw.Stop();
            throw new ProviderUnavailableException(ProviderType.Claude, "Claude request failed", ex);
        }
    }
}
