using Serilog;
using StudioTechBI.AgentHostAPI.Extensions;
using StudioTechBI.AgentHostAPI.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .WriteTo.Console());

    builder.Services.AddControllers();
    builder.Services.AddAgentHostServices(builder.Configuration);
    builder.Services.AddAgentHostSwagger();

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgentHost API v1"));

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
