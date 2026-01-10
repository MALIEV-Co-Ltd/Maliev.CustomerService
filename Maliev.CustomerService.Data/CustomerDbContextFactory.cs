using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.CustomerService.Data;

/// <summary>
/// Design-time factory for EF Core migrations
/// </summary>
public class CustomerDbContextFactory : IDesignTimeDbContextFactory<CustomerDbContext>
{
    /// <inheritdoc />
    public CustomerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CustomerDbContext>();

        // Use connection string from environment variable for migrations
        // Fallback to placeholder if not set (for local development)
        var connectionString = Environment.GetEnvironmentVariable("CustomerDbContext")
            ?? "Host=localhost;Port=5432;Database=customer_app_db;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new CustomerDbContext(optionsBuilder.Options);
    }
}
