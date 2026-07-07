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
            logger.LogInformation("STEP 2 - Relational database");
    
            var cs = db.Database.GetConnectionString();
    
            logger.LogInformation("STEP 3 - Connection string exists: {HasConnectionString}",
                !string.IsNullOrWhiteSpace(cs));
    
            if (string.IsNullOrWhiteSpace(cs))
            {
                throw new InvalidOperationException(
                    "Database connection string is missing.");
            }
    
            logger.LogInformation("STEP 4 - BEFORE MIGRATE");
    
            await db.Database.MigrateAsync(ct);
    
            logger.LogInformation("STEP 5 - AFTER MIGRATE");
        }
        else
        {
            logger.LogInformation("STEP 6 - InMemory");
    
            await db.Database.EnsureCreatedAsync(ct);
    
            logger.LogInformation("STEP 7 - AFTER ENSURE CREATED");
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
