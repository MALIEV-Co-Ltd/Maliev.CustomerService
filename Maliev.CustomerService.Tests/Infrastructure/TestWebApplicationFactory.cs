using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Api.Services.External;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maliev.CustomerService.Tests.Infrastructure;

public class TestWebApplicationFactory : BaseIntegrationTestFactory<Program, CustomerDbContext>
{
    public Mock<ICountryServiceClient> MockCountryService { get; } = new Mock<ICountryServiceClient>();
    public Mock<IIAMClient> MockIAMClient { get; } = new Mock<IIAMClient>();
    public Dictionary<string, string?> ConfigOverrides { get; } = new Dictionary<string, string?>();

    public TestWebApplicationFactory()
    {
        ConfigOverrides["Features:PrincipalBasedAuthEnabled"] = "true";
        ConfigOverrides["Features:PermissionBasedAuthEnabled"] = "false"; // Disable IAM registration in tests

        SetupDefaults();
    }

    public void SetupDefaults()
    {
        MockIAMClient.Reset();
        MockIAMClient.Setup(x => x.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new CreatePrincipalResponse { PrincipalId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });

        MockCountryService.Reset();
        MockCountryService.Setup(x => x.ValidateCountryIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);
    }

    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        base.ConfigureAdditionalServices(services);

        // Replace external services with mocks
        services.AddSingleton(MockCountryService.Object);
        services.AddSingleton(MockIAMClient.Object);
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            if (ConfigOverrides.Count > 0)
            {
                config.AddInMemoryCollection(ConfigOverrides);
            }
        });
    }

    public TestWebApplicationFactory WithConfiguration(Dictionary<string, string?> configuration)
    {
        foreach (var (key, value) in configuration)
        {
            ConfigOverrides[key] = value;
        }
        return this;
    }
}
