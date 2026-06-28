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
            services.AddDbContext<AgentHostDbContext>(opt =>
                opt.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
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
