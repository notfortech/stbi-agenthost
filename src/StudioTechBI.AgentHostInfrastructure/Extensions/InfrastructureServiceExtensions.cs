using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostInfrastructure.Data;
using StudioTechBI.AgentHostInfrastructure.Repositories;
using StudioTechBI.AgentHostInfrastructure.Services;

namespace StudioTechBI.AgentHostInfrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructurePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbProvider = configuration["Database:Provider"] ?? "InMemory";
        if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            // Accept connection string from either standard ASP.NET key or legacy Database:ConnectionString key.
            var connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? configuration["Database:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Database:Provider is SqlServer but no connection string is configured. " +
                    "Set ConnectionStrings__DefaultConnection in Azure App Settings (or the equivalent environment variable).");

            services.AddDbContext<AgentHostDbContext>(opt =>
                opt.UseSqlServer(connectionString));
        }
        else
        {
            services.AddDbContext<AgentHostDbContext>(opt =>
                opt.UseInMemoryDatabase("AgentHostDb"));
        }

        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUsageRepository, UsageRepository>();
        services.AddScoped<ICreditRepository, CreditRepository>();
        services.AddScoped<ICreditEngine, CreditEngine>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
