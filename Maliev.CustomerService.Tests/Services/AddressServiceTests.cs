using FluentAssertions;
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
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<ILogger<AddressService>> _mockLogger;
    private readonly Mock<ICountryServiceClient> _mockCountryServiceClient;

    public AddressServiceTests(TestDatabaseFixture fixture)
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
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.OwnerType.Should().Be("Customer");
        result.OwnerId.Should().Be(request.OwnerId);
        result.Type.Should().Be("Billing");
        result.AddressLine1.Should().Be("123 Main Street");
        result.AddressLine2.Should().Be("Suite 100");
        result.City.Should().Be("Bangkok");
        result.Province.Should().Be("Bangkok");
        result.PostalCode.Should().Be("10110");
        result.CountryId.Should().Be(countryId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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

        exception.Message.Should().Contain("not valid");
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

        exception.Message.Should().Contain("unavailable");
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

        auditLog.Should().NotBeNull();
        auditLog!.ActorId.Should().Be("employee-123");
        auditLog.ActorType.Should().Be("Employee");
        auditLog.Action.Should().Be(AuditAction.Create);
        auditLog.EntityType.Should().Be("Address");
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
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.OwnerType == "Customer" && a.OwnerId == ownerId);
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
        result.Should().NotBeNull();
        result.Should().BeEmpty();
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
        customerAddresses.Should().HaveCount(1);
        customerAddresses[0].OwnerType.Should().Be("Customer");
        customerAddresses[0].OwnerId.Should().Be(customerId);

        companyAddresses.Should().HaveCount(1);
        companyAddresses[0].OwnerType.Should().Be("Company");
        companyAddresses[0].OwnerId.Should().Be(companyId);
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
        result.Should().NotBeNull();
        result.AddressLine1.Should().Be("999 Updated Street");
        result.City.Should().Be("Chiang Mai");
        result.PostalCode.Should().Be("50000");
        result.Province.Should().Be("Bangkok"); // Unchanged
        result.UpdatedAt.Should().BeAfter(created.UpdatedAt);
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
        result.CountryId.Should().Be(newCountryId);
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

        exception.Message.Should().Contain("not valid");
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

        exception.Message.Should().Contain("modified by another user");
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

        auditLogs.Should().HaveCount(2); // Create + Update
        var updateAudit = auditLogs[1];
        updateAudit.ActorId.Should().Be("customer-456");
        updateAudit.ActorType.Should().Be("Customer");
        updateAudit.Action.Should().Be(AuditAction.Update);
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
        result.Should().BeTrue();

        // Verify address is deleted
        await using var context = _fixture.CreateDbContext();
        var address = await context.Addresses.FindAsync(created.Id);
        address.Should().BeNull();
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
        result.Should().BeFalse();
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

        auditLogs.Should().HaveCount(2); // Create + Delete
        var deleteAudit = auditLogs[1];
        deleteAudit.ActorId.Should().Be("manager-999");
        deleteAudit.ActorType.Should().Be("Manager");
        deleteAudit.Action.Should().Be(AuditAction.Delete);
    }
}
