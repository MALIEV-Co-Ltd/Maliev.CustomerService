using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Api.Services.External;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Unit tests for AddressService using real PostgreSQL database
/// Tests business logic, validation, country service integration, and audit logging
/// </summary>
[Collection("Database Collection")]
public class AddressServiceTests
{
    private readonly TestWebApplicationFactory _fixture;
    private readonly Mock<ILogger<AddressService>> _mockLogger;
    private readonly Mock<ICountryServiceClient> _mockCountryServiceClient;

    public AddressServiceTests(TestWebApplicationFactory fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<AddressService>>();
        _mockCountryServiceClient = new Mock<ICountryServiceClient>();
    }


    private AddressService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new AddressService(context, _mockCountryServiceClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsAddressResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(countryId))
            .ReturnsAsync(true);

        var request = new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            AddressLine2 = "Suite 100",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = countryId
        };

        // Act
        var result = await service.CreateAsync(request, "test-actor", "Employee");

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Customer", result.OwnerType);
        Assert.Equal(request.OwnerId, result.OwnerId);
        Assert.Equal("Billing", result.Type);
        Assert.Equal("123 Main Street", result.AddressLine1);
        Assert.Equal("Suite 100", result.AddressLine2);
        Assert.Equal("Bangkok", result.City);
        Assert.Equal("Bangkok", result.Province);
        Assert.Equal("10110", result.PostalCode);
        Assert.Equal(countryId, result.CountryId);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5) && result.CreatedAt <= DateTime.UtcNow.AddSeconds(5));
        _mockCountryServiceClient.Verify(x => x.ValidateCountryIdAsync(countryId), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCountryId_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var invalidCountryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(invalidCountryId))
            .ReturnsAsync(false);

        var request = new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = invalidCountryId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateAsync(request, "test-actor", "Employee"));

        Assert.Contains("not valid", exception.Message);
        _mockCountryServiceClient.Verify(x => x.ValidateCountryIdAsync(invalidCountryId), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithCountryServiceUnavailable_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(countryId))
            .ThrowsAsync(new InvalidOperationException("Country Service unavailable"));

        var request = new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = countryId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateAsync(request, "test-actor", "Employee"));

        Assert.Contains("unavailable", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(countryId))
            .ReturnsAsync(true);

        var request = new CreateAddressRequest
        {
            OwnerType = "Company",
            OwnerId = Guid.NewGuid(),
            Type = "Shipping",
            AddressLine1 = "456 Business Rd",
            City = "Chiang Mai",
            Province = "Chiang Mai",
            PostalCode = "50000",
            CountryId = countryId
        };

        // Act
        var result = await service.CreateAsync(request, "employee-123", "Employee");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLog = await context.AuditLogs
            .Where(a => a.EntityId == result.Id.ToString())
            .FirstOrDefaultAsync();

        Assert.NotNull(auditLog);
        Assert.Equal("employee-123", auditLog!.ActorId);
        Assert.Equal("Employee", auditLog.ActorType);
        Assert.Equal(AuditAction.Create, auditLog.Action);
        Assert.Equal("Address", auditLog.EntityType);
    }

    [Fact]
    public async Task GetByOwnerAsync_WithExistingAddresses_ReturnsAddressList()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var ownerId = Guid.NewGuid();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(countryId))
            .ReturnsAsync(true);

        // Create two addresses for the same owner
        await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = ownerId,
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = countryId
        }, "test-actor", "Employee");

        await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = ownerId,
            Type = "Shipping",
            AddressLine1 = "456 Business Rd",
            City = "Chiang Mai",
            Province = "Chiang Mai",
            PostalCode = "50000",
            CountryId = countryId
        }, "test-actor", "Employee");

        // Act
        var result = await service.GetByOwnerAsync("Customer", ownerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal("Customer", a.OwnerType));
        Assert.All(result, a => Assert.Equal(ownerId, a.OwnerId));
    }

    [Fact]
    public async Task GetByOwnerAsync_WithNoAddresses_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var ownerId = Guid.NewGuid();

        // Act
        var result = await service.GetByOwnerAsync("Customer", ownerId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByOwnerAsync_FiltersByOwnerType_ReturnsCorrectAddresses()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var customerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(countryId))
            .ReturnsAsync(true);

        // Create address for customer
        await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = countryId
        }, "test-actor", "Employee");

        // Create address for company
        await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Company",
            OwnerId = companyId,
            Type = "Billing",
            AddressLine1 = "789 Corporate Ave",
            City = "Phuket",
            Province = "Phuket",
            PostalCode = "83000",
            CountryId = countryId
        }, "test-actor", "Employee");

        // Act
        var customerAddresses = await service.GetByOwnerAsync("Customer", customerId);
        var companyAddresses = await service.GetByOwnerAsync("Company", companyId);

        // Assert
        Assert.Single(customerAddresses);
        Assert.Equal("Customer", customerAddresses[0].OwnerType);
        Assert.Equal(customerId, customerAddresses[0].OwnerId);

        Assert.Single(companyAddresses);
        Assert.Equal("Company", companyAddresses[0].OwnerType);
        Assert.Equal(companyId, companyAddresses[0].OwnerId);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedAddress()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = countryId
        }, "test-actor", "Employee");

        var updateRequest = new UpdateAddressRequest
        {
            AddressLine1 = "999 Updated Street",
            City = "Chiang Mai",
            PostalCode = "50000",
            Version = created.Version
        };

        // Act
        var result = await service.UpdateAsync(created.Id, updateRequest, "test-actor2", "Employee");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("999 Updated Street", result.AddressLine1);
        Assert.Equal("Chiang Mai", result.City);
        Assert.Equal("50000", result.PostalCode);
        Assert.Equal("Bangkok", result.Province); // Unchanged
        Assert.True(result.UpdatedAt > created.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithNewCountryId_ValidatesAndUpdates()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var oldCountryId = Guid.NewGuid();
        var newCountryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(oldCountryId))
            .ReturnsAsync(true);
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(newCountryId))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = oldCountryId
        }, "test-actor", "Employee");

        var updateRequest = new UpdateAddressRequest
        {
            CountryId = newCountryId,
            Version = created.Version
        };

        // Act
        var result = await service.UpdateAsync(created.Id, updateRequest, "test-actor", "Employee");

        // Assert
        Assert.Equal(newCountryId, result.CountryId);
        _mockCountryServiceClient.Verify(x => x.ValidateCountryIdAsync(newCountryId), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidCountryId_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var oldCountryId = Guid.NewGuid();
        var invalidCountryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(oldCountryId))
            .ReturnsAsync(true);
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(invalidCountryId))
            .ReturnsAsync(false);

        var created = await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = oldCountryId
        }, "test-actor", "Employee");

        var updateRequest = new UpdateAddressRequest
        {
            CountryId = invalidCountryId,
            Version = created.Version
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateAsync(created.Id, updateRequest, "test-actor", "Employee"));

        Assert.Contains("not valid", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentAddress_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateAddressRequest
        {
            AddressLine1 = "Updated Street",
            Version = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.UpdateAsync(nonExistentId, updateRequest, "test-actor", "Employee"));
    }

    [Fact]
    public async Task UpdateAsync_WithWrongVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(countryId))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = countryId
        }, "test-actor", "Employee");

        var updateRequest = new UpdateAddressRequest
        {
            AddressLine1 = "Updated Street",
            Version = new byte[] { 0, 0, 0, 0, 0, 0, 0, 99 } // Wrong version
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateAsync(created.Id, updateRequest, "test-actor", "Employee"));

        Assert.Contains("modified by another user", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(countryId))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = countryId
        }, "test-actor", "Employee");

        var updateRequest = new UpdateAddressRequest
        {
            AddressLine1 = "Updated Street",
            Version = created.Version
        };

        // Act
        await service.UpdateAsync(created.Id, updateRequest, "customer-456", "Customer");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        Assert.Equal(2, auditLogs.Count); // Create + Update
        var updateAudit = auditLogs[1];
        Assert.Equal("customer-456", updateAudit.ActorId);
        Assert.Equal("Customer", updateAudit.ActorType);
        Assert.Equal(AuditAction.Update, updateAudit.Action);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingAddress_ReturnsTrue()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(countryId))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = countryId
        }, "test-actor", "Employee");

        // Act
        var result = await service.DeleteAsync(created.Id, "admin-789", "Admin");

        // Assert
        Assert.True(result);

        // Verify address is deleted
        await using var context = _fixture.CreateDbContext();
        var address = await context.Addresses.FindAsync(created.Id);
        Assert.Null(address);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentAddress_ReturnsFalse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.DeleteAsync(nonExistentId, "test-actor", "Employee");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var countryId = Guid.NewGuid();
        _mockCountryServiceClient.Setup(x => x.ValidateCountryIdAsync(countryId))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateAddressRequest
        {
            OwnerType = "Company",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 Main Street",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = countryId
        }, "test-actor", "Employee");

        // Act
        await service.DeleteAsync(created.Id, "manager-999", "Manager");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        Assert.Equal(2, auditLogs.Count); // Create + Delete
        var deleteAudit = auditLogs[1];
        Assert.Equal("manager-999", deleteAudit.ActorId);
        Assert.Equal("Manager", deleteAudit.ActorType);
        Assert.Equal(AuditAction.Delete, deleteAudit.Action);
    }
}
