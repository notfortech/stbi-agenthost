using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostAI.Options;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostDomain.Exceptions;
using StudioTechBI.AgentHostDomain.ValueObjects;

namespace StudioTechBI.AgentHostAI.Providers.OpenAI;

public sealed class OpenAIBlueprintProvider : IBlueprintProvider
{
    private readonly HttpClient _http;
    private readonly OpenAIOptions _options;
    private readonly ILogger<OpenAIBlueprintProvider> _logger;

    public ProviderType Type => ProviderType.OpenAI;

    public OpenAIBlueprintProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AIProviderOptions> options,
        ILogger<OpenAIBlueprintProvider> logger)
    {
        _http = httpClientFactory.CreateClient("OpenAI");
        _options = options.Value.OpenAI;
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

        _logger.LogInformation(
            "OpenAI PRE-REQUEST | BaseAddress={BaseAddress} Model={Model} Temperature={Temperature} MaxTokens={MaxTokens} HasAuthHeader={HasAuthHeader} SystemLen={SystemLen} UserLen={UserLen}",
            _http.BaseAddress, model, temperature, maxTokens,
            _http.DefaultRequestHeaders.Authorization is not null,
            prompt.System.Length, prompt.User.Length);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _http.PostAsync("/v1/chat/completions", content, ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("OpenAI returned {Status}: {Error}", response.StatusCode, error);
                throw new ProviderUnavailableException(ProviderType.OpenAI,
                    $"OpenAI returned {(int)response.StatusCode} ({response.StatusCode}). Response: {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(responseJson);
            }
            catch (JsonException parseEx)
            {
                _logger.LogError(parseEx,
                    "OpenAI response JSON parse failed | RawLength={RawLength} | Raw={Raw}",
                    responseJson.Length,
                    responseJson.Length > 500 ? responseJson[..500] : responseJson);
                throw new ProviderUnavailableException(ProviderType.OpenAI, $"Failed to parse OpenAI response: {parseEx.Message}", parseEx);
            }
            using (doc)
            {
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

            return new ProviderResult(rawContent, new ModelDescriptor(ProviderType.OpenAI, model), tokens, sw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex) when (ex is not ProviderUnavailableException)
        {
            sw.Stop();
            throw new ProviderUnavailableException(ProviderType.OpenAI, "OpenAI request failed", ex);
        }
    }
}
