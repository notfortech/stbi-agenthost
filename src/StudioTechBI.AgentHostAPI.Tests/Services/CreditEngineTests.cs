using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostDomain.Entities;
using StudioTechBI.AgentHostDomain.Enums;
using StudioTechBI.AgentHostInfrastructure.Services;
using Xunit;

namespace StudioTechBI.AgentHostAPI.Tests.Services;

/// <summary>
/// Covers the internal/QA tenant allowlist added to GetOrCreateSubscriptionAsync: a listed
/// tenant should always end up on InternalTenantPlanName, whether it's brand new or already has
/// a subscription on a different (e.g. exhausted Trial) plan. Non-listed tenants must be
/// completely unaffected — this is the regression guard for the pre-existing auto-registration
/// behaviour this change adds branching next to.
/// </summary>
public class CreditEngineTests
{
    private static readonly Plan TrialPlan = new()
    {
        Id = Guid.NewGuid(),
        Name = "Trial",
        PlanType = PlanType.Trial,
        CreditsPerCycle = 10,
        IsUnlimited = false,
        ResetFrequency = ResetFrequency.Monthly,
    };

    private static readonly Plan EnterprisePlan = new()
    {
        Id = Guid.NewGuid(),
        Name = "Enterprise",
        PlanType = PlanType.Enterprise,
        CreditsPerCycle = 0,
        IsUnlimited = true,
        ResetFrequency = ResetFrequency.Monthly,
    };

    private sealed class FakeSubscriptionRepository : ISubscriptionRepository
    {
        public Dictionary<string, TenantSubscription> Store { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<TenantSubscription?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default)
            => Task.FromResult(Store.TryGetValue(tenantId, out var sub) ? sub : null);

        public Task<IReadOnlyList<TenantSubscription>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<TenantSubscription>)Store.Values.ToList());

        public Task<TenantSubscription> AddAsync(TenantSubscription subscription, CancellationToken ct = default)
        {
            Store[subscription.TenantId] = subscription;
            return Task.FromResult(subscription);
        }

        public Task UpdateAsync(TenantSubscription subscription, CancellationToken ct = default)
        {
            Store[subscription.TenantId] = subscription;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlanRepository : IPlanRepository
    {
        public Dictionary<string, Plan> ByName { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Trial"] = TrialPlan,
            ["Enterprise"] = EnterprisePlan,
        };

        public Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(ByName.Values.FirstOrDefault(p => p.Id == id));

        public Task<Plan?> GetByNameAsync(string name, CancellationToken ct = default)
            => Task.FromResult(ByName.TryGetValue(name, out var plan) ? plan : null);

        public Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<Plan>)ByName.Values.Where(p => p.IsActive).ToList());

        public Task UpdateAsync(Plan plan, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoOpCreditRepository : ICreditRepository
    {
        public Task AddTransactionAsync(CreditTransaction transaction, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<CreditTransaction>> GetByTenantAsync(string tenantId, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<CreditTransaction>)Array.Empty<CreditTransaction>());
    }

    private sealed class NoOpUsageRepository : IUsageRepository
    {
        public Task AddAsync(UsageRecord record, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<UsageRecord>> GetByTenantAsync(string tenantId, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<UsageRecord>)Array.Empty<UsageRecord>());

        public Task<IReadOnlyList<UsageRecord>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<UsageRecord>)Array.Empty<UsageRecord>());

        public Task<int> GetTotalCountAsync(string? tenantId = null, CancellationToken ct = default) => Task.FromResult(0);
    }

    private static CreditEngine CreateEngine(
        FakeSubscriptionRepository subscriptionRepo,
        FakePlanRepository planRepo,
        HashSet<string>? internalTenantIds = null)
    {
        var defaults = new SubscriptionDefaults
        {
            DefaultPlanName = "Trial",
            InternalTenantPlanName = "Enterprise",
            InternalTenantIds = internalTenantIds ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AutoRegisterNewTenants = true,
        };

        return new CreditEngine(
            subscriptionRepo,
            planRepo,
            new NoOpCreditRepository(),
            new NoOpUsageRepository(),
            Options.Create(defaults),
            NullLogger<CreditEngine>.Instance);
    }

    [Fact]
    public async Task GetOrCreateSubscriptionAsync_NewInternalTenant_ProvisionsOnInternalPlan_NotDefaultPlan()
    {
        var subscriptionRepo = new FakeSubscriptionRepository();
        var engine = CreateEngine(subscriptionRepo, new FakePlanRepository(),
            internalTenantIds: ["b51042ae-3151-4f41-8b63-4a32c21a0dad"]);

        var sub = await engine.GetOrCreateSubscriptionAsync("b51042ae-3151-4f41-8b63-4a32c21a0dad", "AU-004");

        Assert.Equal("Enterprise", sub.Plan.Name);
        Assert.True(sub.Plan.IsUnlimited);
        Assert.Equal(int.MaxValue, sub.CreditsRemaining);
    }

    [Fact]
    public async Task GetOrCreateSubscriptionAsync_NewNonInternalTenant_StillProvisionsOnDefaultPlan()
    {
        var subscriptionRepo = new FakeSubscriptionRepository();
        var engine = CreateEngine(subscriptionRepo, new FakePlanRepository());

        var sub = await engine.GetOrCreateSubscriptionAsync("some-other-tenant", "Some Other Client");

        Assert.Equal("Trial", sub.Plan.Name);
        Assert.False(sub.Plan.IsUnlimited);
        Assert.Equal(10, sub.CreditsRemaining);
    }

    [Fact]
    public async Task GetOrCreateSubscriptionAsync_ExistingInternalTenantOnTrial_SelfHealsToInternalPlan()
    {
        var subscriptionRepo = new FakeSubscriptionRepository();
        const string tenantId = "b51042ae-3151-4f41-8b63-4a32c21a0dad";
        subscriptionRepo.Store[tenantId] = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantName = "AU-004",
            PlanId = TrialPlan.Id,
            Plan = TrialPlan,
            CreditsRemaining = 0, // exhausted, matching the reported symptom
            CurrentCycleStart = DateTimeOffset.UtcNow,
            NextResetDate = DateTimeOffset.UtcNow.AddDays(20), // not due yet
        };

        var engine = CreateEngine(subscriptionRepo, new FakePlanRepository(),
            internalTenantIds: [tenantId]);

        var sub = await engine.GetOrCreateSubscriptionAsync(tenantId, "AU-004");

        Assert.Equal("Enterprise", sub.Plan.Name);
        Assert.True(sub.Plan.IsUnlimited);
        Assert.Equal(int.MaxValue, sub.CreditsRemaining);
    }

    [Fact]
    public async Task GetOrCreateSubscriptionAsync_ExistingNonInternalTenant_UnaffectedByAllowlist()
    {
        var subscriptionRepo = new FakeSubscriptionRepository();
        const string tenantId = "regular-tenant";
        subscriptionRepo.Store[tenantId] = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantName = "Regular Client",
            PlanId = TrialPlan.Id,
            Plan = TrialPlan,
            CreditsRemaining = 0,
            CurrentCycleStart = DateTimeOffset.UtcNow,
            NextResetDate = DateTimeOffset.UtcNow.AddDays(20),
        };

        // Allowlist has a different tenant on it -- this one must stay untouched, still on Trial,
        // still at 0 credits (the pre-existing "insufficient credits" behaviour is not changed).
        var engine = CreateEngine(subscriptionRepo, new FakePlanRepository(),
            internalTenantIds: ["some-other-internal-tenant"]);

        var sub = await engine.GetOrCreateSubscriptionAsync(tenantId, "Regular Client");

        Assert.Equal("Trial", sub.Plan.Name);
        Assert.Equal(0, sub.CreditsRemaining);
    }

    [Fact]
    public async Task GetOrCreateSubscriptionAsync_ExistingInternalTenantAlreadyOnInternalPlan_ReturnsAsIsWithoutRedundantChangePlanCall()
    {
        var subscriptionRepo = new FakeSubscriptionRepository();
        const string tenantId = "b51042ae-3151-4f41-8b63-4a32c21a0dad";
        subscriptionRepo.Store[tenantId] = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantName = "AU-004",
            PlanId = EnterprisePlan.Id,
            Plan = EnterprisePlan,
            CreditsRemaining = int.MaxValue,
            CurrentCycleStart = DateTimeOffset.UtcNow,
            NextResetDate = DateTimeOffset.UtcNow.AddDays(20),
        };

        var engine = CreateEngine(subscriptionRepo, new FakePlanRepository(),
            internalTenantIds: [tenantId]);

        var sub = await engine.GetOrCreateSubscriptionAsync(tenantId, "AU-004");

        Assert.Equal("Enterprise", sub.Plan.Name);
        Assert.Equal(int.MaxValue, sub.CreditsRemaining);
    }
}
