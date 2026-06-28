namespace StudioTechBI.AgentHostAPI.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId))
            correlationId = Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId.ToString();
        context.Response.Headers[HeaderName] = correlationId.ToString();

        await _next(context);
    }
}
