using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class NDAControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public NDAControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.create" });

        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/customer/v1/ndas", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<NDAResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNDA()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.read" });

        var ndaId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.NDARecords.Add(new NDARecord
        {
            Id = ndaId,
            CustomerId = Guid.NewGuid(),
            Status = NDAStatus.Draft,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/ndas/{ndaId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<NDAResponse>();
        Assert.NotNull(result);
        Assert.Equal(ndaId, result.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.read" });

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/customer/v1/ndas/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByCustomerId_ReturnsNDAs()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.read" });

        var customerId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.NDARecords.Add(new NDARecord
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = NDAStatus.Draft,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/ndas/customer/{customerId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<NDAResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsBadRequest_WhenInvalidTransition()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.update" });

        var ndaId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.NDARecords.Add(new NDARecord
        {
            Id = ndaId,
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            Status = NDAStatus.Draft,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            xmin = 1
        });
        await dbContext.SaveChangesAsync();

        // Try to directly transition from Draft to Expired - invalid
        var request = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Expired,
            xmin = 1
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/customer/v1/ndas/{ndaId}/status", request);

        // Assert - Invalid lifecycle transition should return 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_ReturnsAuditLogs()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.read" });

        var ndaId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.NDARecords.Add(new NDARecord
        {
            Id = ndaId,
            CustomerId = Guid.NewGuid(),
            Status = NDAStatus.Draft,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = "NDARecord",
            EntityId = ndaId.ToString(),
            Action = AuditAction.Create,
            ActorId = "test-actor",
            ActorType = "Employee",
            Timestamp = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/ndas/{ndaId}/history");

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

        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/customer/v1/ndas", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
