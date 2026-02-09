using Maliev.CustomerService.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

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
        _postgresContainer = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("customer_test_db")
            .WithUsername("postgres")
            .WithPassword("test_password")
            .Build();

        _redisContainer = new RedisBuilder("redis:8.4-alpine")
            .Build();

        _rabbitmqContainer = new RabbitMqBuilder("rabbitmq:4.2-alpine")
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
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        await using var context = CreateDbContext();

        // Dynamically get table names from model
        var tableNames = context.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(t => t != null)
            .Cast<string>()
            .ToList();

        // Truncate all tables
        foreach (var tableName in tableNames)
        {
            try
            {
                // Table names are from the model, not user input - safe from SQL injection
#pragma warning disable EF1002, EF1003
                await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE");
#pragma warning restore EF1002, EF1003
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
                // Table doesn't exist - ignore this error
            }
        }

        // Clear EF Core change tracker
        context.ChangeTracker.Clear();

        // Clear connection pool
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
