using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class CompanyControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public CompanyControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetById_ReturnsCompany()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.read" });

        var companyId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            Segment = "Enterprise",
            Tier = "Gold"
        });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/companies/{companyId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(result);
        Assert.Equal("Test Company", result.Name);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.read" });

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/customer/v1/companies/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsCompanies()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.read" });

        using var dbContext = _factory.GetDbContext();
        dbContext.Companies.Add(new Company { Name = "Company 1", Segment = "Retail" });
        dbContext.Companies.Add(new Company { Name = "Company 2", Segment = "Enterprise" });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync("/customer/v1/companies");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsForbidden_WhenMissingPermission()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null);

        var request = new CreateCompanyRequest
        {
            Name = "Test Company",
            Segment = "Retail"
        };

        // Act
        var response = await client.PostAsJsonAsync("/customer/v1/companies", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SearchWithAddress_ReturnsCompanies()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.read" });

        using var dbContext = _factory.GetDbContext();
        dbContext.Companies.Add(new Company { Name = "ABC Logistics", Segment = "Wholesale" });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync("/customer/v1/companies/search?query=ABC");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
