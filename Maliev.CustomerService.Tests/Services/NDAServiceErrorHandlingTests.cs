using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

[Collection("Database Collection")]
public class NDAServiceErrorHandlingTests
{
    private readonly TestWebApplicationFactory _fixture;
    private readonly Mock<ILogger<NDAService>> _mockLogger;
    private readonly Mock<MetricsService> _mockMetricsService;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;

    public NDAServiceErrorHandlingTests(TestWebApplicationFactory fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<NDAService>>();
        _mockMetricsService = new Mock<MetricsService>(MockBehavior.Loose, new object[] { Mock.Of<Microsoft.Extensions.Hosting.IHostEnvironment>() });
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
    }

    private NDAService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new NDAService(context, _mockLogger.Object, _mockMetricsService.Object, _mockPublishEndpoint.Object);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_WithNoNDAs_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var customerId = Guid.NewGuid();

        // Act
        var result = await service.GetByCustomerIdAsync(customerId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
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
    public async Task UpdateStatusAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "test",
            SignedAt = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateStatusAsync(Guid.NewGuid(), request, "actor", "Employee", "Actor Name"));
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid(), 1, "actor", "Employee", "Actor Name");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetHistoryAsync_WithNonExistentId_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var result = await service.GetHistoryAsync(Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CheckExpiredNDAsAsync_WithNoExpiredNDAs_ReturnsZero()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var result = await service.CheckExpiredNDAsAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task CheckUpcomingExpirationsAsync_WithNoUpcomingNDAs_ReturnsZero()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var result = await service.CheckUpcomingExpirationsAsync();

        // Assert
        Assert.Equal(0, result);
    }
}
