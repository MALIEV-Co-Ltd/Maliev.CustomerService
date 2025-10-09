using Maliev.CustomerService.Api.Services.External;
using Maliev.CustomerService.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Maliev.CustomerService.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// - Uses PostgreSQL test database via Testcontainers
/// - Mocks external services (UploadService, CountryService)
/// - Uses fake authentication for testing authorized endpoints
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestDatabaseFixture _databaseFixture;
    public Mock<IUploadServiceClient> MockUploadService { get; }
    public Mock<ICountryServiceClient> MockCountryService { get; }

    public TestWebApplicationFactory()
    {
        _databaseFixture = new TestDatabaseFixture();
        MockUploadService = new Mock<IUploadServiceClient>();
        MockCountryService = new Mock<ICountryServiceClient>();
    }

    public async Task InitializeAsync()
    {
        await _databaseFixture.InitializeAsync();
    }

    public new async Task DisposeAsync()
    {
        await _databaseFixture.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<CustomerDbContext>>();
            services.RemoveAll<CustomerDbContext>();

            // Add test database context
            services.AddDbContext<CustomerDbContext>(options =>
            {
                options.UseNpgsql(_databaseFixture.ConnectionString);
            });

            // Remove real authentication and add fake authentication
            services.RemoveAll<AuthenticationOptions>();
            services.AddAuthentication(FakeAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>(
                    FakeAuthenticationHandler.SchemeName,
                    options => { });

            // Replace external service clients with mocks
            services.RemoveAll<IUploadServiceClient>();
            services.AddSingleton(MockUploadService.Object);

            services.RemoveAll<ICountryServiceClient>();
            services.AddSingleton(MockCountryService.Object);
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates HTTP client with custom authentication headers
    /// </summary>
    public HttpClient CreateAuthenticatedClient(Dictionary<string, string> authHeaders)
    {
        var client = CreateClient();
        foreach (var (key, value) in authHeaders)
        {
            client.DefaultRequestHeaders.Add(key, value);
        }
        return client;
    }

    /// <summary>
    /// Creates HTTP client with Employee role
    /// </summary>
    public HttpClient CreateEmployeeClient()
    {
        return CreateAuthenticatedClient(TestAuthenticationHelper.CreateEmployeeHeaders());
    }

    /// <summary>
    /// Creates HTTP client with Manager role
    /// </summary>
    public HttpClient CreateManagerClient()
    {
        return CreateAuthenticatedClient(TestAuthenticationHelper.CreateManagerHeaders());
    }

    /// <summary>
    /// Creates HTTP client with Admin role
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        return CreateAuthenticatedClient(TestAuthenticationHelper.CreateAdminHeaders());
    }

    /// <summary>
    /// Creates HTTP client with Customer role
    /// </summary>
    public HttpClient CreateCustomerClient(string? customerId = null)
    {
        return CreateAuthenticatedClient(TestAuthenticationHelper.CreateCustomerHeaders(customerId));
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
