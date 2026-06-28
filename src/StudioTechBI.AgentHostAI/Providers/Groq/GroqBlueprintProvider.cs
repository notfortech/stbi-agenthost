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

namespace StudioTechBI.AgentHostAI.Providers.Groq;

public sealed class GroqBlueprintProvider : IBlueprintProvider
{
    private readonly HttpClient _http;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqBlueprintProvider> _logger;

    public ProviderType Type => ProviderType.Groq;

    public GroqBlueprintProvider(
        HttpClient http,
        IOptions<AIProviderOptions> options,
        ILogger<GroqBlueprintProvider> logger)
    {
        _http = http;
        _options = options.Value.Groq;
        _logger = logger;
    }

    public async Task<ProviderResult> GenerateAsync(PromptBundle prompt, GenerationParameters parameters, CancellationToken ct = default)
    {
        var model = parameters.Model ?? _options.DefaultModel;
        var temperature = parameters.Temperature ?? _options.Temperature;
        var maxTokens = parameters.MaxTokens ?? _options.MaxTokens;

        var body = new
        {
            model,
            temperature,
            max_tokens = maxTokens,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = prompt.System },
                new { role = "user", content = prompt.User }
            }
        };

        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _http.PostAsync("/openai/v1/chat/completions", content, ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Groq returned {Status}: {Error}", response.StatusCode, error);
                throw new ProviderUnavailableException(ProviderType.Groq, $"Groq returned {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var rawContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            var usage = doc.RootElement.GetProperty("usage");
            var tokens = new TokenUsage(
                usage.GetProperty("prompt_tokens").GetInt32(),
                usage.GetProperty("completion_tokens").GetInt32(),
                usage.GetProperty("total_tokens").GetInt32());

            return new ProviderResult(rawContent, new ModelDescriptor(ProviderType.Groq, model), tokens, sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not ProviderUnavailableException)
        {
            sw.Stop();
            throw new ProviderUnavailableException(ProviderType.Groq, "Groq request failed", ex);
        }
    }
}
