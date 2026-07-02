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
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
            OperationCanceledException oce =>
                (StatusCodes.Status408RequestTimeout, "Request Timeout",
                 $"The request was cancelled or timed out. Token={oce.CancellationToken.IsCancellationRequested}"),

            ValidationException ve =>
                (StatusCodes.Status400BadRequest, "Validation Failed",
                 string.Join(" | ", ve.Errors.Select(e => e.ErrorMessage))),

            InsufficientCreditsException ice =>
                (StatusCodes.Status402PaymentRequired, "Insufficient Credits",
                 ice.Message),

            SubscriptionNotFoundException snfe =>
                (StatusCodes.Status404NotFound, "Subscription Not Found",
                 snfe.Message),

            ProviderUnavailableException pue =>
                (StatusCodes.Status424FailedDependency, "Provider Unavailable",
                 $"{pue.Message} | InnerType={pue.InnerException?.GetType().Name} | Inner={pue.InnerException?.Message}"),

            BlueprintParseException bpe =>
                (StatusCodes.Status502BadGateway, "Blueprint Parse Failed",
                 bpe.Message),

            InvalidOperationException ioe =>
                (StatusCodes.Status500InternalServerError, "Configuration or State Error",
                 ioe.Message),

            _ =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error",
                 $"[{ex.GetType().Name}] {ex.Message}")
        };

        // Log full exception chain for every error so Azure log stream is always diagnostic.
        var innerChain = BuildInnerChain(ex);
        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(ex,
                "UNHANDLED_EXCEPTION [{ExType}] TraceId={TraceId} | {Method} {Path} | Message={Message} | InnerChain={InnerChain} | Stack={Stack}",
                ex.GetType().FullName, traceId, context.Request.Method, context.Request.Path,
                ex.Message, innerChain, ex.StackTrace);
        else
            _logger.LogWarning(ex,
                "{Title} [{ExType}] | TraceId={TraceId} | {Method} {Path} | {Message} | InnerChain={InnerChain}",
                title, ex.GetType().Name, traceId, context.Request.Method, context.Request.Path,
                ex.Message, innerChain);

        static string BuildInnerChain(Exception? e)
        {
            var parts = new List<string>();
            var inner = e?.InnerException;
            while (inner != null)
            {
                parts.Add($"{inner.GetType().Name}: {inner.Message}");
                inner = inner.InnerException;
            }
            return parts.Count == 0 ? "(none)" : string.Join(" → ", parts);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = traceId;

        // Always include exception chain in the response during this diagnostic phase
        // so the caller can identify the root cause without needing Azure log access.
        problem.Extensions["exceptionType"] = ex.GetType().FullName;
        problem.Extensions["innerChain"] = innerChain;
        if (!_env.IsProduction())
            problem.Extensions["stackTrace"] = ex.StackTrace;

        if (context.Response.HasStarted) return;
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, _jsonOptions));
    }
}
