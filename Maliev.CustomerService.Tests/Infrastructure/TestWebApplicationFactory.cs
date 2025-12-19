using Maliev.CustomerService.Data;
using Maliev.CustomerService.Tests.Testing;
using Maliev.CustomerService.Api.Services.External;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maliev.CustomerService.Tests.Infrastructure;

public class TestWebApplicationFactory : BaseIntegrationTestFactory<Program, CustomerDbContext>
{
    public Mock<ICountryServiceClient> MockCountryService { get; } = new Mock<ICountryServiceClient>();

    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        // Replace ICountryServiceClient with mock
        services.AddSingleton(MockCountryService.Object);
    }


}
