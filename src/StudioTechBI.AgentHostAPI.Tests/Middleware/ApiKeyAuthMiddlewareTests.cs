using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostAPI.Middleware;
using StudioTechBI.AgentHostApplication.Models;
using Xunit;

namespace StudioTechBI.AgentHostAPI.Tests.Middleware;

public class ApiKeyAuthMiddlewareTests
{
    private static (ApiKeyAuthMiddleware Middleware, Func<bool> NextWasCalled) CreateMiddleware()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiKeyAuthMiddleware(next, NullLogger<ApiKeyAuthMiddleware>.Instance);
        return (middleware, () => nextCalled);
    }

    private static DefaultHttpContext CreateContext(string path, string? xApiKey = null, string? authorizationHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (xApiKey is not null)
            context.Request.Headers["X-Api-Key"] = xApiKey;

        if (authorizationHeader is not null)
            context.Request.Headers.Authorization = authorizationHeader;

        return context;
    }

    [Fact]
    public async Task InvokeAsync_NoKeyConfigured_BypassesValidation()
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/blueprints/generate");

        await middleware.InvokeAsync(context, Options.Create(new AuthOptions { ApiKey = "" }));

        Assert.True(nextWasCalled());
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ValidXApiKey_CallsNext()
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/blueprints/generate", xApiKey: "secret");

        await middleware.InvokeAsync(context, Options.Create(new AuthOptions { ApiKey = "secret" }));

        Assert.True(nextWasCalled());
    }

    [Fact]
    public async Task InvokeAsync_MissingXApiKey_Returns401()
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/blueprints/generate");

        await middleware.InvokeAsync(context, Options.Create(new AuthOptions { ApiKey = "secret" }));

        Assert.False(nextWasCalled());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidXApiKey_Returns401()
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/blueprints/generate", xApiKey: "wrong-key");

        await middleware.InvokeAsync(context, Options.Create(new AuthOptions { ApiKey = "secret" }));

        Assert.False(nextWasCalled());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/healthz")]
    [InlineData("/api/health")]
    [InlineData("/ping")]
    public async Task InvokeAsync_PublicPathPrefix_BypassesEvenWithKeyConfigured(string path)
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext(path);

        await middleware.InvokeAsync(context, Options.Create(new AuthOptions { ApiKey = "secret" }));

        Assert.True(nextWasCalled());
    }

    [Fact]
    public async Task InvokeAsync_AuthorizationBearerHeaderAlone_StillRejectedWithoutXApiKey()
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/blueprints/generate", authorizationHeader: "Bearer secret");

        await middleware.InvokeAsync(context, Options.Create(new AuthOptions { ApiKey = "secret" }));

        Assert.False(nextWasCalled());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }
}
