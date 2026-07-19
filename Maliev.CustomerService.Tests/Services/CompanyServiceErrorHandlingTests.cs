using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

[Collection("Database Collection")]
public class CompanyServiceErrorHandlingTests
{
    private readonly TestWebApplicationFactory _fixture;
    private readonly Mock<ILogger<CompanyService>> _mockLogger;

    public CompanyServiceErrorHandlingTests(TestWebApplicationFactory fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<CompanyService>>();
    }

    private CompanyService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new CompanyService(context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new UpdateCompanyRequest
        {
            Name = "Updated Name"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), request, "actor", "Employee"));
    }

    [Fact]
    public async Task SearchWithAddressAsync_WithNoMatchingCriteria_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var result = await service.SearchWithAddressAsync("NonExistentCompany");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithNoCompanies_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var result = await service.GetAllAsync(1, 10);

        // Assert
        Assert.Empty(result.Companies);
    }

    [Fact]
    public async Task GetWithCustomersAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var result = await service.GetWithCustomersAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}
