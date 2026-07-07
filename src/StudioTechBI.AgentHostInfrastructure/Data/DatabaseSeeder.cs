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
         _logger.LogInformation("DatabaseSeeder reached.");
        return Task.CompletedTask;
    }    
}
