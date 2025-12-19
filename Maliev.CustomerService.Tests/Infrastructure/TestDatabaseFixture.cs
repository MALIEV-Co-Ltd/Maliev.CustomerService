using Maliev.CustomerService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;

namespace Maliev.CustomerService.Tests.Infrastructure;

/// <summary>
/// Manages PostgreSQL, Redis, and RabbitMQ test containers using Testcontainers
/// Provides clean infrastructure for each test class
/// </summary>
public class TestDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private RedisContainer? _redisContainer;
    private RabbitMqContainer? _rabbitmqContainer;
    public string ConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;
    public string RabbitMqConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Initialize containers
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("customer_test_db")
            .WithUsername("postgres")
            .WithPassword("test_password")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        _rabbitmqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:4.2.1-alpine")
            .Build();

        // Start all containers in parallel
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitmqContainer.StartAsync()
        );

        ConnectionString = _postgresContainer.GetConnectionString();
        RedisConnectionString = _redisContainer.GetConnectionString();
        RabbitMqConnectionString = _rabbitmqContainer.GetConnectionString();

        // Wait for Redis to be ready
        using (var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(RedisConnectionString))
        {
            await connection.GetDatabase().PingAsync();
        }

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
        if (_redisContainer != null)
        {
            await _redisContainer.DisposeAsync();
        }
        if (_rabbitmqContainer != null)
        {
            await _rabbitmqContainer.DisposeAsync();
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
    /// Uses EF Core methods instead of raw SQL
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        // Clear connection pool to ensure fresh connections
        Npgsql.NpgsqlConnection.ClearAllPools();

        await using var context = CreateDbContext();

        // Delete data using EF Core in correct FK order

        // Clear Identity-related data (preserve Roles)
        context.UserTokens.RemoveRange(await context.UserTokens.ToListAsync());
        context.UserLogins.RemoveRange(await context.UserLogins.ToListAsync());
        context.UserClaims.RemoveRange(await context.UserClaims.ToListAsync());
        context.UserRoles.RemoveRange(await context.UserRoles.ToListAsync());
        context.Users.RemoveRange(await context.Users.ToListAsync());

        // Clear application data
        context.InternalNotes.RemoveRange(await context.InternalNotes.ToListAsync());
        context.DocumentReferences.RemoveRange(await context.DocumentReferences.ToListAsync());
        context.NDARecords.RemoveRange(await context.NDARecords.ToListAsync());
        context.Addresses.RemoveRange(await context.Addresses.ToListAsync());
        context.Customers.RemoveRange(await context.Customers.IgnoreQueryFilters().ToListAsync());
        context.Companies.RemoveRange(await context.Companies.IgnoreQueryFilters().ToListAsync());
        context.AuditLogs.RemoveRange(await context.AuditLogs.ToListAsync());

        await context.SaveChangesAsync();

        // Clear EF Core change tracker
        context.ChangeTracker.Clear();

        // Clear connection pool after cleanup
        Npgsql.NpgsqlConnection.ClearAllPools();
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
