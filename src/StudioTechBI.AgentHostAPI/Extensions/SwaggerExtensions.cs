using Microsoft.OpenApi.Models;

namespace StudioTechBI.AgentHostAPI.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddAgentHostSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "StudioTech BI · AgentHost API",
                Version = "v1",
                Description = "Thin orchestration layer that wraps the Blueprint AI agent — provider-agnostic, strongly typed."
            });

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Name = "X-Api-Key",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Description = "API key header"
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
        });

        return services;
    }
}
