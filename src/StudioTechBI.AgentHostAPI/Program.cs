using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using StudioTechBI.AgentHostAPI.Extensions;
using StudioTechBI.AgentHostAPI.Middleware;
using StudioTechBI.AgentHostInfrastructure.Data;

// Bootstrap logger — used until full Serilog is configured from appsettings.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ────────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithProperty("Application", "STBI-AgentHost")
              .WriteTo.Console());

    // ── MVC + JSON ────────────────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            // camelCase JSON + ignore null values for clean blueprint output.
            o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // ── CORS — locked to Koru's frontend origin; dev origins in appsettings.Development.json ──
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()));

    // ── Application services ──────────────────────────────────────────────────
    try { builder.Services.AddAgentHostServices(builder.Configuration); }
    catch (Exception ex) { Log.Fatal(ex, "Startup failed during AddAgentHostServices"); throw; }

    try { builder.Services.AddAgentHostSwagger(); }
    catch (Exception ex) { Log.Fatal(ex, "Startup failed during AddAgentHostSwagger"); throw; }

    // ── Azure App Service: forwarded headers ──────────────────────────────────
    // Required so HTTPS detection and IP resolution work behind Azure's load balancer.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Trust all proxies when running in Azure (no explicit proxy IP known at build time).
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // ── Problem Details ───────────────────────────────────────────────────────
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // ── Seed database on startup ──────────────────────────────────────────────
    try { 
        using var scope = app.Services.CreateScope(); 
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>(); 
        await seeder.SeedAsync(); 
    } catch (Exception ex) 
        { 
            Log.Fatal(ex, "Startup failed during DatabaseSeeder.SeedAsync"); 
            throw; 
    }
    // ── Pipeline order matters ────────────────────────────────────────────────

    // 1. Forwarded headers first so subsequent middleware sees the real scheme/IP.
    app.UseForwardedHeaders();

    // 2. Global exception handler before everything else.
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // 3. Correlation ID — stamps every request/response with X-Correlation-Id.
    app.UseMiddleware<CorrelationIdMiddleware>();

    // 4. Bearer API key validation — Koru sends Authorization: Bearer <ApiKey>.
    app.UseMiddleware<ApiKeyAuthMiddleware>();

    // 5. Credit validation — intercepts POST /api/blueprints/generate before routing.
    app.UseMiddleware<CreditValidationMiddleware>();

    // 5. Swagger — enabled in Development always; in Production via Swagger:Enabled flag.
    app.UseAgentHostSwagger(builder.Configuration);

    // 5. Structured request logging (after correlation ID is attached).
    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
        {
            diagCtx.Set("CorrelationId", httpCtx.Items["CorrelationId"] ?? httpCtx.TraceIdentifier);
            diagCtx.Set("ClientIp", httpCtx.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
    });

    // 6. CORS before auth/routing.
    app.UseCors();

    // 7. HTTPS redirection (Azure termination is at the load balancer so this is a no-op there
    //    when ForwardedHeaders sets the proto correctly — but kept for direct-access safety).
    app.UseHttpsRedirection();

    // 8. Routing + controllers.
    app.MapControllers();

    // 9. ASP.NET built-in health endpoint (maps alongside our custom HealthController).
    //    /healthz is the Azure App Service default health-check path.
    app.MapHealthChecks("/healthz");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
