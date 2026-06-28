using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudioTechBI.AgentHostDomain.Exceptions;

namespace StudioTechBI.AgentHostAPI.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        var (status, title) = ex switch
        {
            ProviderUnavailableException => (StatusCodes.Status424FailedDependency, "Provider Unavailable"),
            BlueprintParseException => (StatusCodes.Status502BadGateway, "Blueprint Parse Failed"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
