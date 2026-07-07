using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace StudioTechBI.AgentHostInfrastructure.Data;

/// <summary>
/// Allows EF Core CLI tools (dotnet ef migrations add / database update) to instantiate
/// AgentHostDbContext without running the full ASP.NET Core host.
///
/// Resolution order for the connection string:
///   1. ConnectionStrings__DefaultConnection environment variable
///   2. Database__ConnectionString environment variable
///   3. appsettings.json (loaded relative to the API project output directory)
///
/// Before running EF tools locally, set:
///   $env:ConnectionStrings__DefaultConnection = "Server=...;Database=...;..."
/// </summary>
public sealed class AgentHostDbContextFactory : IDesignTimeDbContextFactory<AgentHostDbContext>
{
    public AgentHostDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                optional: true)
            .AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"),
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            config.GetConnectionString("DefaultConnection")
            ?? config["Database:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "EF design-time: no connection string found. " +
                "Set the ConnectionStrings__DefaultConnection environment variable before running dotnet ef commands.");

        var optionsBuilder = new DbContextOptionsBuilder<AgentHostDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new AgentHostDbContext(optionsBuilder.Options);
    }
}
