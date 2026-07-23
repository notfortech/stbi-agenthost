using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostAPI.Middleware;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostDomain.Entities;
using Xunit;

namespace StudioTechBI.AgentHostAPI.Tests.Middleware;

public class CreditValidationMiddlewareTests
{
    private sealed class FakeCreditEngine : ICreditEngine
    {
        public List<string> ResolvedTenantIds { get; } = [];
        public TenantSubscription SubscriptionToReturn { get; set; } = CreateSubscription();

        public Task<TenantSubscription> GetOrCreateSubscriptionAsync(string tenantId, string tenantName, CancellationToken ct = default)
        {
            ResolvedTenantIds.Add(tenantId);
            return Task.FromResult(SubscriptionToReturn);
        }

        public Task<bool> CheckAndResetIfNeededAsync(TenantSubscription subscription, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<CreditDeductionResult> DeductAsync(
            TenantSubscription subscription, Guid requestId, Guid blueprintId, int tokensUsed,
            long executionTimeMs, string provider, string model, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TenantSubscription> ForceResetAsync(string tenantId, string changedBy, string? reason, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<TenantSubscription> ChangePlanAsync(string tenantId, Guid newPlanId, string changedBy, string? notes, CancellationToken ct = default)
            => throw new NotImplementedException();

        public static TenantSubscription CreateSubscription(int creditsRemaining = 10, bool isUnlimited = false) => new()
        {
            TenantId = "test-tenant",
            TenantName = "Test",
            Plan = new Plan { Name = "Trial", IsUnlimited = isUnlimited },
            CreditsRemaining = creditsRemaining,
        };
    }

    private static (CreditValidationMiddleware Middleware, Func<bool> NextWasCalled) CreateMiddleware()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        return (new CreditValidationMiddleware(next, NullLogger<CreditValidationMiddleware>.Instance), () => nextCalled);
    }

    private static DefaultHttpContext CreateContext(string path, string method = "POST", string? tenantIdHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();

        if (tenantIdHeader is not null)
            context.Request.Headers["X-Tenant-Id"] = tenantIdHeader;

        return context;
    }

    private static IOptions<SubscriptionDefaults> Defaults(string fallbackTenantId = "demo-tenant") =>
        Options.Create(new SubscriptionDefaults { FallbackTenantId = fallbackTenantId });

    [Fact]
    public async Task InvokeAsync_NonGeneratePath_SkipsTenantResolution()
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/something-else");
        var creditEngine = new FakeCreditEngine();

        await middleware.InvokeAsync(context, creditEngine, Defaults());

        Assert.True(nextWasCalled());
        Assert.Empty(creditEngine.ResolvedTenantIds);
    }

    [Fact]
    public async Task InvokeAsync_TenantHeaderPresent_ResolvesAndCallsNext()
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/blueprints/generate", tenantIdHeader: "acme-corp");
        var creditEngine = new FakeCreditEngine();

        await middleware.InvokeAsync(context, creditEngine, Defaults());

        Assert.True(nextWasCalled());
        Assert.Equal(["acme-corp"], creditEngine.ResolvedTenantIds);
    }

    [Fact]
    public async Task InvokeAsync_NoHeader_NonEmptyFallback_FallsBackToDefaultTenant()
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/blueprints/generate");
        var creditEngine = new FakeCreditEngine();

        await middleware.InvokeAsync(context, creditEngine, Defaults(fallbackTenantId: "demo-tenant"));

        Assert.True(nextWasCalled());
        Assert.Equal(["demo-tenant"], creditEngine.ResolvedTenantIds);
    }

    [Fact]
    public async Task InvokeAsync_NoHeader_BlankFallback_Returns401()
    {
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/blueprints/generate");
        var creditEngine = new FakeCreditEngine();

        await middleware.InvokeAsync(context, creditEngine, Defaults(fallbackTenantId: ""));

        Assert.False(nextWasCalled());
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Empty(creditEngine.ResolvedTenantIds);
    }

    [Fact]
    public async Task InvokeAsync_InsufficientCredits_ConfigBypassOff_HardcodedFailSafeStillAllows()
    {
        // SubscriptionDefaults.ForceCreditBypass is a hardcoded fail-safe on top of the
        // config-bound BypassCreditLimit flag (see SubscriptionOptions.cs) — added after a stray
        // Azure App Service setting silently pinned BypassCreditLimit back to false in production
        // despite appsettings.json defaulting it to true. Even with BypassCreditLimit explicitly
        // off here, the request must still be allowed through.
        var (middleware, nextWasCalled) = CreateMiddleware();
        var context = CreateContext("/api/blueprints/generate", tenantIdHeader: "acme-corp");
        var creditEngine = new FakeCreditEngine
        {
            SubscriptionToReturn = FakeCreditEngine.CreateSubscription(creditsRemaining: 0, isUnlimited: false)
        };
        var defaults = Options.Create(new SubscriptionDefaults { BypassCreditLimit = false });

        await middleware.InvokeAsync(context, creditEngine, defaults);

        Assert.True(nextWasCalled());
    }
}
