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

        // Claude (Anthropic)
        services.AddKeyedScoped<IBlueprintProvider, ClaudeBlueprintProvider>(ProviderType.Claude);
        services.AddHttpClient("Claude", client =>
        {
            client.BaseAddress = new Uri(providerOptions.Claude.BaseUrl);
            client.DefaultRequestHeaders.Add("x-api-key", providerOptions.Claude.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", providerOptions.Claude.AnthropicVersion);
            client.Timeout = TimeSpan.FromSeconds(providerOptions.Claude.TimeoutSeconds);
        }).AddStandardResilienceHandler();

        // OpenAI
        services.AddKeyedScoped<IBlueprintProvider, OpenAIBlueprintProvider>(ProviderType.OpenAI);
        services.AddHttpClient("OpenAI", client =>
        {
            client.BaseAddress = new Uri(providerOptions.OpenAI.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {providerOptions.OpenAI.ApiKey}");
            client.Timeout = TimeSpan.FromSeconds(providerOptions.OpenAI.TimeoutSeconds);
        }).AddStandardResilienceHandler();

        // Groq (OpenAI-compatible)
        services.AddKeyedScoped<IBlueprintProvider, GroqBlueprintProvider>(ProviderType.Groq);
        services.AddHttpClient("Groq", client =>
        {
            client.BaseAddress = new Uri(providerOptions.Groq.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {providerOptions.Groq.ApiKey}");
            client.Timeout = TimeSpan.FromSeconds(providerOptions.Groq.TimeoutSeconds);
        }).AddStandardResilienceHandler();

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
