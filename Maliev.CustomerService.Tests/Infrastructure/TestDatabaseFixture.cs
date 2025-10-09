using Maliev.CustomerService.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Maliev.CustomerService.Tests.Infrastructure;

/// <summary>
/// Manages PostgreSQL test database lifecycle using Testcontainers
/// Provides clean database for each test class
/// </summary>
public class TestDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("customer_test_db")
            .WithUsername("postgres")
            .WithPassword("test_password")
            .Build();

        await _postgresContainer.StartAsync();
        ConnectionString = _postgresContainer.GetConnectionString();

        // Apply migrations to test database
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a new DbContext instance for the test database
    /// </summary>
    public CustomerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new CustomerDbContext(options);
    }

    /// <summary>
    /// Clears all data from the database (for test isolation)
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        await using var context = CreateDbContext();

        // Disable triggers and cascade deletes temporarily
        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica';");

        // Delete data from all tables in reverse dependency order (ignore errors if tables don't exist)
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM internal_notes;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM document_references;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM nda_records;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM addresses;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM customers;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM companies;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM audit_logs;"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUserRoles\";"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\";"); } catch { }
        try { await context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetRoles\";"); } catch { }

        // Re-enable triggers
        await context.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';");
    }

    /// <summary>
    /// Seeds test data for common scenarios
    /// </summary>
    public async Task SeedTestDataAsync(Action<CustomerDbContext> seedAction)
    {
        await using var context = CreateDbContext();
        seedAction(context);
        await context.SaveChangesAsync();
    }
}
