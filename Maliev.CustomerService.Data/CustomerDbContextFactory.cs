using Maliev.CustomerService.Data.Interceptors;
using Maliev.CustomerService.Data.Interfaces;
using Maliev.CustomerService.Data.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

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

        // Create a minimal configuration for design-time (migrations)
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            })
            .Build();

        IEncryptionService encryptionService = new EncryptionService(configuration);
        var encryptionInterceptor = new EncryptionInterceptor(encryptionService);

        return new CustomerDbContext(optionsBuilder.Options, encryptionService, encryptionInterceptor);
    }
}
