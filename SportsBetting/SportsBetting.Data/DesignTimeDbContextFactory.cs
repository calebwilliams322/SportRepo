using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SportsBetting.Data;

/// <summary>
/// Design-time factory for creating SportsBettingDbContext during migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SportsBettingDbContext>
{
    public SportsBettingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SportsBettingDbContext>();

        // Use PostgreSQL for migrations
        // Default connection string for development - override with environment variable if needed
        var connectionString = Environment.GetEnvironmentVariable("SPORTSBETTING_DB")
            ?? "Host=localhost;Database=sportsbetting;Username=calebwilliams";

        optionsBuilder.UseNpgsql(connectionString);

        return new SportsBettingDbContext(optionsBuilder.Options);
    }
}
