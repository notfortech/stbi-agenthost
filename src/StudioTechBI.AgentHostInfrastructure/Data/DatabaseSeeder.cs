using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Models;
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
        logger.LogInformation("STEP 1");
    
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(ct);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }
    
        logger.LogInformation("STEP 2");
    
        try
        {
            logger.LogInformation("STEP 3 - Before querying Plans");
    
            var existingPlans = await db.Plans.ToListAsync(ct);
    
            logger.LogInformation("STEP 4 - Retrieved {Count} plans", existingPlans.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "FAILED querying Plans. Exception={Type} Message={Message}",
                ex.GetType().FullName,
                ex.Message);
    
            throw;
        }
    }

    internal static DateTimeOffset ComputeNextReset(DateTimeOffset from, ResetFrequency freq) => freq switch
    {
        ResetFrequency.Daily => from.AddDays(1),
        ResetFrequency.Weekly => from.AddDays(7),
        ResetFrequency.Monthly => from.AddMonths(1),
        ResetFrequency.Quarterly => from.AddMonths(3),
        ResetFrequency.Yearly => from.AddYears(1),
        _ => from.AddMonths(1),
    };
}
