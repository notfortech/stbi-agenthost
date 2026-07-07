using System.Net.Http.Headers;
using FluentValidation;
using Microsoft.Extensions.Http.Resilience;
using StudioTechBI.AgentHostAI.Mapping;
using StudioTechBI.AgentHostAI.Options;
using StudioTechBI.AgentHostAI.Parsing;
using StudioTechBI.AgentHostAI.Prompting;
using StudioTechBI.AgentHostAI.Providers.Claude;
using StudioTechBI.AgentHostAI.Providers.Groq;
using StudioTechBI.AgentHostAI.Providers.OpenAI;
using StudioTechBI.AgentHostAI.Routing;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostApplication.Models.Requests;
using StudioTechBI.AgentHostApplication.Services;
using StudioTechBI.AgentHostApplication.Validation;
using StudioTechBI.AgentHostAPI.Filters;
using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostInfrastructure.Extensions;
using StudioTechBI.AgentHostInfrastructure.Health;
using StudioTechBI.AgentHostInfrastructure.Persistence;
using StudioTechBI.AgentHostInfrastructure.Prompting;
using IBlueprintPdfService = StudioTechBI.AgentHostApplication.Abstractions.IBlueprintPdfService;

namespace StudioTechBI.AgentHostAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Strongly typed options ────────────────────────────────────────────
        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));
        services.Configure<AIProviderOptions>(configuration.GetSection(AIProviderOptions.SectionName));
        services.Configure<PromptOptions>(configuration.GetSection(PromptOptions.SectionName));
        services.Configure<SubscriptionDefaults>(configuration.GetSection(SubscriptionDefaults.SectionName));
        services.Configure<AgentLimits>(configuration.GetSection(AgentLimits.SectionName));
        services.Configure<AdminSettings>(configuration.GetSection(AdminSettings.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        // ── Database, repositories, credit engine (Infrastructure layer) ─────
        services.AddInfrastructurePersistence(configuration);

        // ── Admin filter (registered as service for ServiceFilter attribute) ──
        services.AddScoped<AdminAuthFilter>();

        // ── Caching (prompt templates) ────────────────────────────────────────
        services.AddMemoryCache();

        // ── FluentValidation ──────────────────────────────────────────────────
        services.AddValidatorsFromAssemblyContaining<BlueprintRequestValidator>();
        services.AddScoped<IBlueprintRequestValidator, BlueprintRequestValidatorAdapter>();

        // ── Application use-case services ─────────────────────────────────────
        services.AddScoped<IBlueprintGenerationService, BlueprintGenerationService>();

        // ── Infrastructure ────────────────────────────────────────────────────
        services.AddScoped<IPromptResourceProvider, FilePromptResourceProvider>();
        services.AddScoped<IBlueprintPersistenceService, BlueprintPersistenceService>();
        services.AddScoped<IBlueprintPdfService, BlueprintPdfService>();

        // ── AI providers — keyed by ProviderType ──────────────────────────────
        var providerOptions = configuration
            .GetSection(AIProviderOptions.SectionName)
            .Get<AIProviderOptions>() ?? new();

        Serilog.Log.Information(
            "AI provider config — Claude: enabled={Ce} url={Cu} | OpenAI: enabled={Oe} url={Ou} | Groq: enabled={Ge} url={Gu}",
            providerOptions.Claude.Enabled, providerOptions.Claude.BaseUrl,
            providerOptions.OpenAI.Enabled, providerOptions.OpenAI.BaseUrl,
            providerOptions.Groq.Enabled, providerOptions.Groq.BaseUrl);

        Serilog.Log.Information(
            "OpenAI diagnostic — DefaultProvider={DefaultProvider} Enabled={Enabled} BaseUrl={BaseUrl} DefaultModel={DefaultModel} ApiKeyPresent={ApiKeyPresent}",
            providerOptions.DefaultProvider, providerOptions.OpenAI.Enabled,
            providerOptions.OpenAI.BaseUrl, providerOptions.OpenAI.DefaultModel,
            !string.IsNullOrEmpty(providerOptions.OpenAI.ApiKey));

        static Uri? SafeUri(string? raw, string providerName)
        {
            if (Uri.TryCreate(raw, UriKind.Absolute, out var uri)) return uri;
            Serilog.Log.Warning(
                "AI provider {Provider} BaseUrl '{Url}' is missing or not an absolute URI — provider will fail at runtime if used",
                providerName, raw);
            return null;
        }

        // Claude (Anthropic)
        services.AddKeyedScoped<IBlueprintProvider, ClaudeBlueprintProvider>(ProviderType.Claude);
        var claudeUri = SafeUri(providerOptions.Claude.BaseUrl, "Claude");
        services.AddHttpClient("Claude", client =>
        {
            if (claudeUri != null) client.BaseAddress = claudeUri;
            client.DefaultRequestHeaders.Add("x-api-key", providerOptions.Claude.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", providerOptions.Claude.AnthropicVersion);
            // Use the configured timeout directly — AddStandardResilienceHandler's default
            // 10-second attempt timeout kills legitimate long AI calls (blueprints take 15-30s).
            client.Timeout = TimeSpan.FromSeconds(providerOptions.Claude.TimeoutSeconds);
        });

        // OpenAI
        services.AddKeyedScoped<IBlueprintProvider, OpenAIBlueprintProvider>(ProviderType.OpenAI);
        var openAiUri = SafeUri(providerOptions.OpenAI.BaseUrl, "OpenAI");
        services.AddHttpClient("OpenAI", client =>
        {
            if (openAiUri != null) client.BaseAddress = openAiUri;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", providerOptions.OpenAI.ApiKey);
            client.Timeout = TimeSpan.FromSeconds(providerOptions.OpenAI.TimeoutSeconds);
        });

        // Groq (OpenAI-compatible)
        services.AddKeyedScoped<IBlueprintProvider, GroqBlueprintProvider>(ProviderType.Groq);
        var groqUri = SafeUri(providerOptions.Groq.BaseUrl, "Groq");
        services.AddHttpClient("Groq", client =>
        {
            if (groqUri != null) client.BaseAddress = groqUri;
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {providerOptions.Groq.ApiKey}");
            client.Timeout = TimeSpan.FromSeconds(providerOptions.Groq.TimeoutSeconds);
        });

        // ── AI pipeline: routing, prompting, parsing, mapping ─────────────────
        services.AddScoped<IBlueprintProviderFactory, BlueprintProviderFactory>();
        services.AddScoped<IProviderRouter, ProviderRouter>();
        services.AddScoped<IPromptBuilder, PromptBuilder>();
        services.AddScoped<IBlueprintJsonValidator, BlueprintJsonValidator>();
        services.AddScoped<IBlueprintResponseParser, BlueprintResponseParser>();
        services.AddScoped<IBlueprintMapper, BlueprintMapper>();

        // ── Health checks ─────────────────────────────────────────────────────
        services.AddHealthChecks()
            .AddCheck<ProviderHealthCheck>("providers");

        return services;
    }
}
