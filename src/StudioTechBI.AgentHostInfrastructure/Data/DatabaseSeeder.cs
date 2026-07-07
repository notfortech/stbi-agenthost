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
     private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ILogger<DatabaseSeeder> logger)
    {
        _logger = logger;
    }
    
    public async Task SeedAsync(CancellationToken ct = default)
    {
         _logger.LogInformation("DatabaseSeeder reached.");
        return Task.CompletedTask;
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
