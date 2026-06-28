using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StudioTechBI.AgentHostAPI.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddAgentHostSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "StudioTech BI Agent Host",
                Version = "v1",
                Description = """
                    **StudioTech BI · Agent Host API**

                    Exposes the AI Dashboard Blueprint Generator as a REST API.
                    Submit business requirements and receive a fully structured
                    Analytics Deployment Contract (ADC) blueprint in JSON.

                    Providers: Claude · OpenAI · Groq (configured via appsettings / environment variables)
                    """,
                Contact = new OpenApiContact { Name = "StudioTech BI" }
            });

            // ── Auth: JWT Bearer (placeholder for future auth middleware) ──────
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token. Example: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            // ── XML comments ─────────────────────────────────────────────────
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            // ── Operation filter: tag each operation with its controller name ─
            options.TagActionsBy(api => [api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "API"]);
            options.DocInclusionPredicate((_, _) => true);

            // ── Schema filter: mark required properties ───────────────────────
            options.SupportNonNullableReferenceTypes();
        });

        return services;
    }

    public static IApplicationBuilder UseAgentHostSwagger(this IApplicationBuilder app, IConfiguration configuration)
    {
        var swaggerEnabled = configuration.GetValue<bool>("Swagger:Enabled", defaultValue: true);
        var isDevEnvironment = app is WebApplication wa && wa.Environment.IsDevelopment();

        if (!swaggerEnabled && !isDevEnvironment)
            return app;

        app.UseSwagger(c =>
        {
            c.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "StudioTech BI Agent Host v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "StudioTech BI Agent Host";
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.DefaultModelsExpandDepth(2);
            c.DefaultModelExpandDepth(3);
        });

        return app;
    }
}
