using FluentValidation;
using Microsoft.Extensions.Http.Resilience;
using StudioTechBI.AgentHostAI.Mapping;
using StudioTechBI.AgentHostAI.Options;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostAI.Parsing;
using StudioTechBI.AgentHostAI.Prompting;
using StudioTechBI.AgentHostAI.Providers.Groq;
using StudioTechBI.AgentHostAI.Providers.OpenAI;
using StudioTechBI.AgentHostAI.Routing;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models.Requests;
using StudioTechBI.AgentHostApplication.Services;
using StudioTechBI.AgentHostApplication.Validation;
using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostInfrastructure.Health;
using StudioTechBI.AgentHostInfrastructure.Prompting;

namespace StudioTechBI.AgentHostAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Options
        services.Configure<AIProviderOptions>(configuration.GetSection(AIProviderOptions.SectionName));
        services.Configure<StudioTechBI.AgentHostApplication.Models.PromptOptions>(configuration.GetSection(StudioTechBI.AgentHostApplication.Models.PromptOptions.SectionName));

        // Caching
        services.AddMemoryCache();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<BlueprintRequestValidator>();
        services.AddScoped<IBlueprintRequestValidator, BlueprintRequestValidatorAdapter>();

        // Application
        services.AddScoped<IBlueprintGenerationService, BlueprintGenerationService>();

        // Infrastructure
        services.AddScoped<IPromptResourceProvider, FilePromptResourceProvider>();

        // AI — providers (keyed)
        var providerOptions = configuration.GetSection(AIProviderOptions.SectionName).Get<AIProviderOptions>() ?? new();

        services.AddKeyedScoped<IBlueprintProvider, OpenAIBlueprintProvider>(ProviderType.OpenAI);
        services.AddHttpClient<OpenAIBlueprintProvider>(client =>
        {
            client.BaseAddress = new Uri(providerOptions.OpenAI.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {providerOptions.OpenAI.ApiKey}");
            client.Timeout = TimeSpan.FromSeconds(providerOptions.OpenAI.TimeoutSeconds);
        }).AddStandardResilienceHandler();

        services.AddKeyedScoped<IBlueprintProvider, GroqBlueprintProvider>(ProviderType.Groq);
        services.AddHttpClient<GroqBlueprintProvider>(client =>
        {
            client.BaseAddress = new Uri(providerOptions.Groq.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {providerOptions.Groq.ApiKey}");
            client.Timeout = TimeSpan.FromSeconds(providerOptions.Groq.TimeoutSeconds);
        }).AddStandardResilienceHandler();

        // AI — routing + parsing + mapping
        services.AddScoped<IBlueprintProviderFactory, BlueprintProviderFactory>();
        services.AddScoped<IProviderRouter, ProviderRouter>();
        services.AddScoped<IPromptBuilder, PromptBuilder>();
        services.AddScoped<IBlueprintJsonValidator, BlueprintJsonValidator>();
        services.AddScoped<IBlueprintResponseParser, BlueprintResponseParser>();
        services.AddScoped<IBlueprintMapper, BlueprintMapper>();

        // Health checks
        services.AddHealthChecks()
            .AddCheck<ProviderHealthCheck>("providers");

        return services;
    }
}
