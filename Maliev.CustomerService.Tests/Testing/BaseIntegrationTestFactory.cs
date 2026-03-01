using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Infrastructure.Persistence;
using Maliev.CustomerService.Infrastructure.Persistence.Interceptors;
using Maliev.CustomerService.Infrastructure.Security;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Maliev.CustomerService.Tests.Testing;

/// <summary>
/// Base integration test factory for CustomerService.
/// Provides PostgreSQL, Redis, and RabbitMQ containers with parallel startup.
/// </summary>
/// <typeparam name="TProgram">The Program class of the service being tested</typeparam>
/// <typeparam name="TDbContext">The DbContext type for the service</typeparam>
public class BaseIntegrationTestFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
    where TDbContext : DbContext
{
    private static PostgreSqlContainer? _postgresContainer;
    private static RedisContainer? _redisContainer;
    private static RabbitMqContainer? _rabbitmqContainer;
    private static bool _containersStarted;
    private static readonly SemaphoreSlim _initLock = new(1, 1);
    private static IEncryptionService? _encryptionService;
    private static EncryptionInterceptor? _encryptionInterceptor;

    private readonly RSA _testRsa;

    /// <summary>
    /// Override this property if your DbContext connection string has a different name.
    /// Defaults to the DbContext class name.
    /// </summary>
    protected virtual string DbConnectionStringName => typeof(TDbContext).Name;

    public BaseIntegrationTestFactory(string environment = "Testing")
    {
        _testRsa = RSA.Create(2048);

        // Set environment variable EARLY so Program.cs picks it up during WebApplication.CreateBuilder
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);

        // Also set JWT configuration for validation
        var publicKeyPem = _testRsa.ExportRSAPublicKeyPem();
        var publicKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem));
        Environment.SetEnvironmentVariable("Jwt__PublicKey", publicKeyBase64);
        Environment.SetEnvironmentVariable("Jwt__SecurityKey", "test-secret-key-at-least-32-characters-long");

        // Set Rate Limiting environment variables for tests
        Environment.SetEnvironmentVariable("RateLimiting__PermitLimit", "100");
        Environment.SetEnvironmentVariable("RateLimiting__public__PermitLimit", "50");
    }

    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (!_containersStarted)
            {
                _postgresContainer = new PostgreSqlBuilder("postgres:18-alpine")
                    .Build();

                _redisContainer = new RedisBuilder("redis:7.4-alpine")
                    .Build();

                _rabbitmqContainer = new RabbitMqBuilder("rabbitmq:4.0-alpine")
                    .Build();

                // Start all containers in parallel
                await Task.WhenAll(
                    _postgresContainer.StartAsync(),
                    _redisContainer.StartAsync(),
                    _rabbitmqContainer.StartAsync()
                );

                // Ensure PostgreSQL is fully ready and accepting connections
                var postgresReady = false;
                var retryCount = 0;
                const int maxRetries = 60;
                while (!postgresReady && retryCount < maxRetries)
                {
                    try
                    {
                        await using var conn = new Npgsql.NpgsqlConnection(_postgresContainer.GetConnectionString());
                        await conn.OpenAsync();
                        await using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT 1";
                        await cmd.ExecuteScalarAsync();
                        postgresReady = true;
                    }
                    catch
                    {
                        retryCount++;
                        await Task.Delay(1000);
                    }
                }

                if (!postgresReady)
                {
                    throw new InvalidOperationException("PostgreSQL Testcontainer failed to become ready after 60 seconds.");
                }

                // Apply database migrations
                await ApplyMigrationsAsync();

                _containersStarted = true;
            }
        }
        finally
        {
            _initLock.Release();
        }

        // Set environment variables immediately after containers start
        Environment.SetEnvironmentVariable($"ConnectionStrings__{DbConnectionStringName}", _postgresContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__redis", _redisContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", _rabbitmqContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("CORS_ALLOWED_ORIGINS", "http://localhost:3000");
        Environment.SetEnvironmentVariable("IAM__RegistrationDelaySeconds", "0");
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync(); // Stop the Host
        _testRsa.Dispose();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null); // Cleanup
        Environment.SetEnvironmentVariable("CORS_ALLOWED_ORIGINS", null);
        Environment.SetEnvironmentVariable("IAM__RegistrationDelaySeconds", null);
    }


    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure containers are started before creating host
        if (!_containersStarted)
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        // Export RSA public key for JWT validation
        var rsaParams = _testRsa.ExportParameters(false);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_MODULUS", Convert.ToBase64String(rsaParams.Modulus!));
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_EXPONENT", Convert.ToBase64String(rsaParams.Exponent!));

        // Also set the format expected by some Aspire helpers (raw base64 of public key info)
        var keyBytes = _testRsa.ExportSubjectPublicKeyInfo();
        Environment.SetEnvironmentVariable("Authentication__Jwt__PublicKey", Convert.ToBase64String(keyBytes));

        // Allow derived classes to set additional environment variables
        ConfigureEnvironmentVariables();

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecurityKey"] = "test-secret-key-at-least-32-characters-long",
                [$"ConnectionStrings:{DbConnectionStringName}"] = _postgresContainer!.GetConnectionString(),
                ["ConnectionStrings:redis"] = _redisContainer!.GetConnectionString(),
                ["ConnectionStrings:rabbitmq"] = _rabbitmqContainer!.GetConnectionString(),
                ["CORS_ALLOWED_ORIGINS"] = "http://localhost:3000",
                ["IAM:RegistrationDelaySeconds"] = "0"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Manual Redis registration
            var redisConnectionString = _redisContainer!.GetConnectionString();
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
            {
                return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
            });

            // Mock IAM service
            var iamMock = new Mock<Maliev.Aspire.ServiceDefaults.IAM.IIamServiceClient>();
            iamMock.Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            iamMock.Setup(x => x.GetUserPermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<string>());
            services.AddSingleton(iamMock.Object);

            var statusTracker = new Maliev.Aspire.ServiceDefaults.IAM.IAMRegistrationStatusTracker();
            statusTracker.MarkRegistered();
            services.AddSingleton(statusTracker);

            // Configure JWT Bearer authentication with test RSA key
            services.PostConfigureAll<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "test-issuer",
                    ValidAudience = "test-audience",
                    IssuerSigningKey = new RsaSecurityKey(_testRsa),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
                options.TokenValidationParameters.SignatureValidator = null;
            });

            // Add MassTransit test harness
            services.AddMassTransitTestHarness();

            // Disable background services
            var backgroundServicesToDisable = new[]
            {
                "NDAExpirationBackgroundService",
                "DocumentDeletionRetryBackgroundService",
                "BackgroundIAMRegistrationService"
            };

            var descriptors = services.Where(d =>
                d.ServiceType == typeof(IHostedService) &&
                backgroundServicesToDisable.Contains(d.ImplementationType?.Name)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Allow derived classes to add additional test services
            ConfigureAdditionalServices(services);
        });
    }

    /// <summary>
    /// Override this method to set additional environment variables before host creation.
    /// Called after standard environment variables are set.
    /// </summary>
    protected virtual void ConfigureEnvironmentVariables()
    {
        // Set dummy URLs for external services
        Environment.SetEnvironmentVariable("CountryService__BaseUrl", "http://localhost:5000");
        Environment.SetEnvironmentVariable("UploadService__BaseUrl", "http://localhost:5001");
    }

    /// <summary>
    /// Override this method to add additional test services to the DI container.
    /// </summary>
    protected virtual void ConfigureAdditionalServices(IServiceCollection services)
    {
        // Default mocks for common external services
        var mockCountryService = new Mock<Maliev.CustomerService.Api.Services.External.ICountryServiceClient>();
        mockCountryService.Setup(x => x.ValidateCountryIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);
        services.AddSingleton(mockCountryService.Object);

        var mockUploadService = new Mock<Maliev.CustomerService.Api.Services.External.IUploadServiceClient>();
        mockUploadService.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        mockUploadService.Setup(x => x.DeleteFileAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        services.AddSingleton(mockUploadService.Object);
    }

    /// <summary>
    /// Gets the DbContext from the service provider for use in tests.
    /// </summary>
    public TDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TDbContext>();
    }

    /// <summary>
    /// Creates a new DbContext instance for testing (not from DI container).
    /// </summary>
    public TDbContext CreateDbContext()
    {
        var connectionString = _postgresContainer!.GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Special handling for CustomerDbContext which requires IEncryptionService
        if (typeof(TDbContext) == typeof(CustomerDbContext))
        {
            // Create encryption service and interceptor once and reuse to avoid EF Core service provider bloat
            if (_encryptionService == null)
            {
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ASPNETCORE_ENVIRONMENT"] = "Testing"
                    })
                    .Build();
                _encryptionService = new EncryptionService(configuration);
                _encryptionInterceptor = new EncryptionInterceptor(_encryptionService);
            }

            return (TDbContext)(object)new CustomerDbContext(
                (DbContextOptions<CustomerDbContext>)(object)optionsBuilder.Options,
                _encryptionService,
                _encryptionInterceptor!);
        }

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options)!;
    }

    /// <summary>
    /// Applies all pending migrations to the test database.
    /// </summary>
    private async Task ApplyMigrationsAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Cleans all data from the database while preserving schema.
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        await using var context = CreateDbContext();

        var tableNames = await context.Database
            .SqlQueryRaw<string>(
                @"SELECT table_name
                  FROM information_schema.tables
                  WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE'
                  AND table_name != '__EFMigrationsHistory'
                  ORDER BY table_name")
            .ToListAsync();

        foreach (var tableName in tableNames)
        {
            try
            {
#pragma warning disable EF1002
                await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE");
#pragma warning restore EF1002
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
            }
        }
    }

    /// <summary>
    /// Alias for CleanDatabaseAsync to support different naming conventions.
    /// </summary>
    public Task ResetDatabaseAsync() => CleanDatabaseAsync();

    /// <summary>
    /// Alias for CleanDatabaseAsync to support different naming conventions.
    /// </summary>
    public Task ClearDatabaseAsync() => CleanDatabaseAsync();

    /// <summary>
    /// Clears the in-memory cache.
    /// </summary>
    public void ClearCache()
    {
        var memoryCache = Services.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        if (memoryCache is Microsoft.Extensions.Caching.Memory.MemoryCache cache)
        {
            cache.Compact(1.0);
        }
    }

    /// <summary>
    /// Creates a test JWT token for authentication in integration tests.
    /// </summary>
    public string CreateTestJwtToken(
        string userId = "test-user",
        string[]? roles = null,
        string[]? permissions = null,
        Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (roles != null) foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));
        if (permissions != null) foreach (var permission in permissions) claims.Add(new Claim("permissions", permission));

        if (additionalClaims != null)
        {
            foreach (var (key, value) in additionalClaims) claims.Add(new Claim(key, value));
        }

        var rsaSecurityKey = new RsaSecurityKey(_testRsa);
        var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Simplified JWT token generator with role parameter.
    /// </summary>
    public string GenerateTestToken(string userId = "test-user", string role = "admin")
    {
        return CreateTestJwtToken(userId, new[] { role });
    }

    /// <summary>
    /// Creates an HTTP client with authenticated user and specified roles and permissions.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string[]? roles = null, string[]? permissions = null)
    {
        var token = CreateTestJwtToken(userId, roles, permissions);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }
}
