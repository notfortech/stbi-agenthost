using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostDomain.Entities;
using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostInfrastructure.Data;

public sealed class DatabaseSeeder(
    AgentHostDbContext db,
    IOptions<AgentLimits> agentLimits,
    IOptions<SubscriptionDefaults> subscriptionDefaults,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (db.Database.IsRelational())
        {
            var cs = db.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException(
                    "Database:Provider is set to a relational provider but no connection string is configured. " +
                    "Set ConnectionStrings__DefaultConnection in Azure App Settings.");
            await db.Database.MigrateAsync(ct);
        }
        else
            await db.Database.EnsureCreatedAsync(ct);

        var limits = agentLimits.Value;
        var defaults = subscriptionDefaults.Value;

        var existingPlans = await db.Plans.ToListAsync(ct);

        var planDefs = new[]
        {
            (Name: "Trial",        Type: PlanType.Trial,        Config: limits.Trial),
            (Name: "Starter",      Type: PlanType.Starter,      Config: limits.Starter),
            (Name: "Professional", Type: PlanType.Professional,  Config: limits.Professional),
            (Name: "Enterprise",   Type: PlanType.Enterprise,    Config: limits.Enterprise),
        };

        var planMap = new Dictionary<string, Plan>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, planType, config) in planDefs)
        {
            var existing = existingPlans.FirstOrDefault(p => p.Name == name);
            if (existing is null)
            {
                var resetFreq = Enum.TryParse<ResetFrequency>(config.ResetFrequency, true, out var rf)
                    ? rf : ResetFrequency.Monthly;

                existing = new Plan
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    PlanType = planType,
                    CreditsPerCycle = config.CreditsPerCycle,
                    IsUnlimited = config.IsUnlimited,
                    ResetFrequency = resetFreq,
                    MaximumRequestsPerMinute = config.MaxRequestsPerMinute,
                    MaximumConcurrentRequests = config.MaxConcurrentRequests,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                db.Plans.Add(existing);
                logger.LogInformation("Seeded plan {PlanName}", name);
            }
            planMap[name] = existing;
        }

        await db.SaveChangesAsync(ct);

        // Seed demo-tenant subscription for Swagger testing
        var demoTenantId = defaults.FallbackTenantId;
        if (!string.IsNullOrEmpty(demoTenantId))
        {
            var exists = await db.TenantSubscriptions
                .AnyAsync(s => s.TenantId == demoTenantId, ct);

            if (!exists && planMap.TryGetValue(defaults.DefaultPlanName, out var defaultPlan))
            {
                var now = DateTimeOffset.UtcNow;
                var sub = new TenantSubscription
                {
                    Id = Guid.NewGuid(),
                    TenantId = demoTenantId,
                    TenantName = "Demo Tenant (Swagger Testing)",
                    PlanId = defaultPlan.Id,
                    CreditsRemaining = defaultPlan.IsUnlimited ? int.MaxValue : defaultPlan.CreditsPerCycle,
                    CreditsUsedThisCycle = 0,
                    CurrentCycleStart = now,
                    NextResetDate = defaultPlan.ResetFrequency == ResetFrequency.Never
                        ? null
                        : ComputeNextReset(now, defaultPlan.ResetFrequency),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                db.TenantSubscriptions.Add(sub);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Seeded demo-tenant subscription on plan {Plan}", defaultPlan.Name);
            }
        }
    }

    internal static DateTimeOffset ComputeNextReset(DateTimeOffset from, ResetFrequency freq) => freq switch
    {
        ResetFrequency.Daily     => from.AddDays(1),
        ResetFrequency.Weekly    => from.AddDays(7),
        ResetFrequency.Monthly   => from.AddMonths(1),
        ResetFrequency.Quarterly => from.AddMonths(3),
        ResetFrequency.Yearly    => from.AddYears(1),
        _                        => from.AddMonths(1),
    };
}
