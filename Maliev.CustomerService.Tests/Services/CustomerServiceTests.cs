using FluentAssertions;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Unit tests for CustomerService using real PostgreSQL database
/// Tests business logic, validation, and audit logging
/// </summary>
[Collection("Database Collection")]
public class CustomerServiceTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<ILogger<Api.Services.CustomerService>> _mockLogger;
    private readonly Mock<Api.Services.MetricsService> _mockMetricsService;

    public CustomerServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<Api.Services.CustomerService>>();
        _mockMetricsService = new Mock<Api.Services.MetricsService>(MockBehavior.Loose, new object[] { Mock.Of<IConfiguration>() });
    }

    private Api.Services.CustomerService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new Api.Services.CustomerService(context, _mockLogger.Object, _mockMetricsService.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsCustomerResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok",
            CommunicationPreferences = new Dictionary<string, object>
            {
                { "email_opt_in", true },
                { "sms_opt_in", false }
            }
        };

        // Act
        var result = await service.CreateAsync(request, "test-actor", "Employee");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john.doe@example.com");
        result.Phone.Should().Be("+66-2-123-4567");
        result.Segment.Should().Be("Retail");
        result.Tier.Should().Be("Bronze");
        result.PreferredLanguage.Should().Be("en");
        result.Timezone.Should().Be("Asia/Bangkok");
        result.CommunicationPreferences.Should().NotBeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateCustomerRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "duplicate@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        };

        // Create first customer
        await service.CreateAsync(request, "test-actor", "Employee");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateAsync(request, "test-actor", "Employee"));
    }

    [Fact]
    public async Task CreateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateCustomerRequest
        {
            FirstName = "Audit",
            LastName = "Test",
            Email = "audit.test@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Wholesale",
            Tier = "Silver",
            PreferredLanguage = "th",
            Timezone = "Asia/Bangkok"
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
        auditLog.EntityType.Should().Be("Customer");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingCustomer_ReturnsCustomerResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Get",
            LastName = "Test",
            Email = "get.test@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Enterprise",
            Tier = "Gold",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        // Act
        var result = await service.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Email.Should().Be("get.test@example.com");
        result.Segment.Should().Be("Enterprise");
        result.Tier.Should().Be("Gold");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentCustomer_ReturnsNull()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithSoftDeletedCustomer_ReturnsNull()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Delete",
            LastName = "Test",
            Email = "delete.test@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        // Soft delete the customer
        await service.SoftDeleteAsync(created.Id, "test-actor", "Employee");

        // Act
        var result = await service.GetByIdAsync(created.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedCustomer()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Update",
            LastName = "Test",
            Email = "update.test@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "Updated",
            Phone = "+66-2-999-9999",
            Tier = "Silver",
            Version = created.Version
        };

        // Act
        var result = await service.UpdateAsync(created.Id, updateRequest, "test-actor2", "Employee");

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Updated");
        result.Phone.Should().Be("+66-2-999-9999");
        result.Tier.Should().Be("Silver");
        result.LastName.Should().Be("Test"); // Unchanged
        result.Email.Should().Be("update.test@example.com"); // Unchanged
        result.UpdatedAt.Should().BeAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentCustomer_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "Updated",
            Version = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.UpdateAsync(nonExistentId, updateRequest, "test-actor", "Employee"));
    }

    [Fact]
    public async Task UpdateAsync_WithWrongVersion_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Concurrency",
            LastName = "Test",
            Email = "concurrency.test@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "Updated",
            Version = new byte[] { 0, 0, 0, 0, 0, 0, 0, 99 } // Wrong version
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateAsync(created.Id, updateRequest, "test-actor", "Employee"));
        exception.Message.Should().Contain("customer was modified by another user");
    }

    [Fact]
    public async Task UpdateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Audit",
            LastName = "Update",
            Email = "audit.update@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "Updated Audit",
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
    public async Task SoftDeleteAsync_WithExistingCustomer_ReturnsTrue()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Delete",
            LastName = "Test",
            Email = "soft.delete@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        // Act
        var result = await service.SoftDeleteAsync(created.Id, "admin-789", "Admin");

        // Assert
        result.Should().BeTrue();

        // Verify customer is marked as deleted
        await using var context = _fixture.CreateDbContext();
        var customer = await context.Customers.FindAsync(created.Id);
        customer.Should().NotBeNull();
        customer!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteAsync_WithNonExistentCustomer_ReturnsFalse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.SoftDeleteAsync(nonExistentId, "test-actor", "Employee");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Audit",
            LastName = "Delete",
            Email = "audit.delete@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        // Act
        await service.SoftDeleteAsync(created.Id, "manager-999", "Manager");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        auditLogs.Should().HaveCount(2); // Create + SoftDelete
        var deleteAudit = auditLogs[1];
        deleteAudit.ActorId.Should().Be("manager-999");
        deleteAudit.ActorType.Should().Be("Manager");
        deleteAudit.Action.Should().Be(AuditAction.SoftDelete);
    }

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ReturnsAllCustomers()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Create 3 customers
        for (int i = 0; i < 3; i++)
        {
            await service.CreateAsync(new CreateCustomerRequest
            {
                FirstName = $"Customer{i}",
                LastName = "Test",
                Email = $"customer{i}@example.com",
                Phone = "+66-2-123-4567",
                Segment = "Retail",
                Tier = "Bronze",
                PreferredLanguage = "en",
                Timezone = "Asia/Bangkok"
            }, "test-actor", "Employee");
        }

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetAllAsync_WithSegmentFilter_ReturnsFilteredCustomers()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Retail",
            LastName = "Customer",
            Email = "retail@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Enterprise",
            LastName = "Customer",
            Email = "enterprise@example.com",
            Phone = "+66-2-123-4568",
            Segment = "Enterprise",
            Tier = "Gold",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        // Act
        var result = await service.GetAllAsync(segment: "Enterprise");

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Segment.Should().Be("Enterprise");
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Create 5 customers
        for (int i = 0; i < 5; i++)
        {
            await service.CreateAsync(new CreateCustomerRequest
            {
                FirstName = $"Customer{i}",
                LastName = "Test",
                Email = $"page{i}@example.com",
                Phone = "+66-2-123-4567",
                Segment = "Retail",
                Tier = "Bronze",
                PreferredLanguage = "en",
                Timezone = "Asia/Bangkok"
            }, "test-actor", "Employee");
        }

        // Act
        var page1 = await service.GetAllAsync(page: 1, pageSize: 2);
        var page2 = await service.GetAllAsync(page: 2, pageSize: 2);

        // Assert
        page1.Items.Should().HaveCount(2);
        page1.Page.Should().Be(1);
        page1.TotalCount.Should().Be(5);

        page2.Items.Should().HaveCount(2);
        page2.Page.Should().Be(2);
        page2.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesDeletedCustomersByDefault()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "ToDelete",
            LastName = "Customer",
            Email = "todelete@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        }, "test-actor", "Employee");

        await service.SoftDeleteAsync(created.Id, "test-actor", "Employee");

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPreferencesAsync_ReturnsCustomerPreferences()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Preferences",
            LastName = "Test",
            Email = "prefs@example.com",
            Phone = "+66-2-123-4567",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "th",
            Timezone = "Asia/Bangkok",
            CommunicationPreferences = new Dictionary<string, object>
            {
                { "email_opt_in", true },
                { "sms_opt_in", false }
            }
        }, "test-actor", "Employee");

        // Act
        var result = await service.GetPreferencesAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);

        var pref = result.Items[0];
        pref.PreferredLanguage.Should().Be("th");
        pref.Timezone.Should().Be("Asia/Bangkok");
        pref.CommunicationPreferences.Should().NotBeNull();
    }
}
