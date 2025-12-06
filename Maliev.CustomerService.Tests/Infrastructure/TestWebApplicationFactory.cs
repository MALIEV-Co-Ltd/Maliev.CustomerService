using Maliev.CustomerService.Api.Services.External;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Maliev.CustomerService.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// - Uses PostgreSQL test database via Testcontainers
/// - Mocks external services (UploadService, CountryService)
/// - Uses dynamic RSA keys for JWT authentication testing
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestDatabaseFixture _databaseFixture;
    private readonly RSA _testRsa;
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    public Mock<IUploadServiceClient> MockUploadService { get; }
    public Mock<ICountryServiceClient> MockCountryService { get; }

    public TestWebApplicationFactory(TestDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
        MockUploadService = new Mock<IUploadServiceClient>();
        MockCountryService = new Mock<ICountryServiceClient>();

        // Generate ephemeral RSA key for test JWT tokens
        _testRsa = RSA.Create(2048);
    }

    public Task InitializeAsync()
    {
        // Database is already initialized by the collection fixture
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        _testRsa.Dispose();
        // Database will be disposed by the collection fixture
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override connection string via configuration instead of removing services
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CustomerDbContext"] = _databaseFixture.ConnectionString,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Register DbContext (Program.cs skips this in Testing environment)
            services.AddDbContext<CustomerDbContext>(options =>
            {
                options.UseNpgsql(_databaseFixture.ConnectionString);
            });

            // PostConfigure JWT Bearer options to use our test RSA key
            services.PostConfigureAll<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new RsaSecurityKey(_testRsa),
                    ClockSkew = TimeSpan.Zero // No clock skew for tests
                };
            });

            // Replace external service clients with mocks
            services.RemoveAll<IUploadServiceClient>();
            services.AddSingleton(MockUploadService.Object);

            services.RemoveAll<ICountryServiceClient>();
            services.AddSingleton(MockCountryService.Object);
        });
    }

    /// <summary>
    /// Creates a test JWT token with specified claims for integration testing.
    /// </summary>
    /// <param name="userId">User ID claim</param>
    /// <param name="roles">User roles</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>JWT token string</returns>
    public string CreateTestJwtToken(string userId = "test-user", string[]? roles = null, Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles
        roles ??= new[] { "Admin" };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add additional claims
        if (additionalClaims != null)
        {
            foreach (var (key, value) in additionalClaims)
            {
                claims.Add(new Claim(key, value));
            }
        }

        var credentials = new SigningCredentials(
            new RsaSecurityKey(_testRsa),
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates HTTP client with JWT Bearer token authentication
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string[]? roles = null, Dictionary<string, string>? additionalClaims = null)
    {
        var client = CreateClient();
        var token = CreateTestJwtToken(userId, roles, additionalClaims);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates HTTP client with Employee role
    /// </summary>
    public HttpClient CreateEmployeeClient(string userId = "test-employee")
    {
        return CreateAuthenticatedClient(userId, new[] { "Employee" });
    }

    /// <summary>
    /// Creates HTTP client with Manager role
    /// </summary>
    public HttpClient CreateManagerClient(string userId = "test-manager")
    {
        return CreateAuthenticatedClient(userId, new[] { "Manager" });
    }

    /// <summary>
    /// Creates HTTP client with Admin role
    /// </summary>
    public HttpClient CreateAdminClient(string userId = "test-admin")
    {
        return CreateAuthenticatedClient(userId, new[] { "Admin" });
    }

    /// <summary>
    /// Creates HTTP client with Customer role
    /// </summary>
    public HttpClient CreateCustomerClient(string? customerId = null)
    {
        var claims = customerId != null ? new Dictionary<string, string> { ["customer_id"] = customerId } : null;
        return CreateAuthenticatedClient(customerId ?? "test-customer", new[] { "Customer" }, claims);
    }

    /// <summary>
    /// Gets DbContext for direct database operations in tests
    /// </summary>
    public CustomerDbContext GetDbContext()
    {
        return _databaseFixture.CreateDbContext();
    }

    /// <summary>
    /// Clears all data from test database
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        await _databaseFixture.ClearDatabaseAsync();
    }

    /// <summary>
    /// Seeds test data into database
    /// </summary>
    public async Task SeedTestDataAsync(Action<CustomerDbContext> seedAction)
    {
        await _databaseFixture.SeedTestDataAsync(seedAction);
    }
}
