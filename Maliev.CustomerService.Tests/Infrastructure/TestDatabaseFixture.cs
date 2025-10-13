using Maliev.CustomerService.Data;
using Microsoft.AspNetCore.Identity;
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

        // Seed ASP.NET Core Identity roles
        await SeedIdentityRolesAsync(context);
    }

    /// <summary>
    /// Seeds ASP.NET Core Identity roles required for testing
    /// </summary>
    private static async Task SeedIdentityRolesAsync(CustomerDbContext context)
    {
        var roles = new[] { "Customer", "Employee", "Manager", "Admin" };

        foreach (var roleName in roles)
        {
            var roleExists = await context.Roles.AnyAsync(r => r.Name == roleName);
            if (!roleExists)
            {
                var role = new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                context.Roles.Add(role);
            }
        }

        await context.SaveChangesAsync();
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
        // Force garbage collection to finalize undisposed DbContext instances
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Aggressively clear connection pool multiple times to ensure all connections are closed
        for (int i = 0; i < 3; i++)
        {
            Npgsql.NpgsqlConnection.ClearAllPools();
            await Task.Delay(200); // Increased delay to 200ms per iteration (600ms total)
        }

        // First context: terminate blocking connections
        var terminateOptions = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using (var terminateContext = new CustomerDbContext(terminateOptions))
        {
            // Force terminate any blocking connections
            await terminateContext.Database.ExecuteSqlRawAsync(@"
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = current_database()
                AND pid <> pg_backend_pid()
                AND state IN ('idle in transaction', 'active');
            ");
        }

        // Small delay after terminating connections
        await Task.Delay(100);

        // Second context: perform DELETE operations (fresh connection)
        var deleteOptions = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new CustomerDbContext(deleteOptions);

        // Delete data in correct order to avoid FK violations
        // Use try-catch in C# to handle missing tables (more reliable than DO blocks)
        async Task TryDeleteAsync(string tableName)
        {
            try
            {
#pragma warning disable EF1002 // tableName is controlled internally, not from user input
                await context.Database.ExecuteSqlRawAsync($"DELETE FROM {tableName}");
#pragma warning restore EF1002
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01") // undefined_table
            {
                // Table doesn't exist yet, ignore
            }
        }

        // Delete Identity-related data (preserve AspNetRoles)
        await TryDeleteAsync("\"AspNetUserTokens\"");
        await TryDeleteAsync("\"AspNetUserLogins\"");
        await TryDeleteAsync("\"AspNetUserClaims\"");
        await TryDeleteAsync("\"AspNetUserRoles\"");
        await TryDeleteAsync("\"AspNetUsers\"");

        // Delete application data
        await TryDeleteAsync("internal_notes");
        await TryDeleteAsync("document_references");
        await TryDeleteAsync("nda_records");
        await TryDeleteAsync("addresses");
        await TryDeleteAsync("customers");
        await TryDeleteAsync("companies");
        await TryDeleteAsync("audit_logs");

        // Ensure EF Core change tracker is cleared
        context.ChangeTracker.Clear();
        await context.Database.CloseConnectionAsync();

        // Final aggressive cleanup with retry to ensure DELETE is fully committed
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Npgsql.NpgsqlConnection.ClearAllPools();
            await Task.Delay(200);

            // Verify with fresh context
            var verifyOptions = new DbContextOptionsBuilder<CustomerDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            await using var verifyContext = new CustomerDbContext(verifyOptions);
            try
            {
                var remainingUsers = await verifyContext.Users.CountAsync();
                if (remainingUsers == 0)
                {
                    // Success - database is clean
                    break;
                }

                if (attempt == 2)
                {
                    // Last attempt - force delete again
                    await verifyContext.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\"");
                    await verifyContext.SaveChangesAsync();
                }
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
                // Table doesn't exist yet
                break;
            }
        }
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
