using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using StudioTechBI.AgentHostDomain.Exceptions;

namespace StudioTechBI.AgentHostAPI.Middleware;

/// <summary>
/// Global exception handler. Maps known domain and infrastructure exceptions to
/// RFC 7807 ProblemDetails with appropriate HTTP status codes.
/// All errors include a traceId so logs can be correlated from the client side.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
        var traceId = context.TraceIdentifier;

        var (status, title, detail) = ex switch
        {
            OperationCanceledException =>
                (StatusCodes.Status408RequestTimeout, "Request Timeout",
                 "The request was cancelled or timed out waiting for the AI provider."),

            ValidationException ve =>
                (StatusCodes.Status400BadRequest, "Validation Failed",
                 string.Join(" | ", ve.Errors.Select(e => e.ErrorMessage))),

            ProviderUnavailableException pue =>
                (StatusCodes.Status424FailedDependency, "Provider Unavailable",
                 pue.Message),

            BlueprintParseException =>
                (StatusCodes.Status502BadGateway, "Blueprint Parse Failed",
                 ex.Message),

            _ =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error",
                 "An unexpected error occurred. Check server logs with the provided traceId.")
        };

        // Only log full stack for unexpected errors; known errors at Warning.
        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception | TraceId={TraceId} | {Method} {Path}", traceId, context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(ex, "{Title} | TraceId={TraceId} | {Method} {Path}", title, traceId, context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = traceId;

        if (context.Response.HasStarted) return;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, _jsonOptions));
    }
}
