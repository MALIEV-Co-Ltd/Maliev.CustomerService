using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Unit tests for CustomerService using real PostgreSQL database
/// Tests business logic, validation, and audit logging
/// </summary>
[Collection("Database Collection")]
public class CustomerServiceTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<IIAMClient> _mockIamClient;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<Api.Services.CustomerService>> _mockLogger;
    private readonly Mock<Api.Services.MetricsService> _mockMetricsService;

    public CustomerServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _mockIamClient = new Mock<IIAMClient>();
        _mockConfig = new Mock<IConfiguration>();

        // Setup default configuration (enabled in final state)
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns("true");
        _mockConfig.Setup(c => c.GetSection("Features:PrincipalBasedAuthEnabled")).Returns(mockSection.Object);

        // Default IAM response (unique per call)
        _mockIamClient.Setup(x => x.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new CreatePrincipalResponse { PrincipalId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });

        _mockLogger = new Mock<ILogger<Api.Services.CustomerService>>();
        _mockMetricsService = new Mock<Api.Services.MetricsService>(MockBehavior.Loose, new object[] { Mock.Of<IConfiguration>() });
    }

    private Api.Services.CustomerService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new Api.Services.CustomerService(
            context,
            _mockIamClient.Object,
            _mockConfig.Object,
            _mockLogger.Object,
            _mockMetricsService.Object);
    }

    [Fact]
    public async Task CreateAsync_WithPrincipalAuthEnabled_CallsIAMClientAndSetsPrincipalId()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        // Enable feature flag
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns("true");
        _mockConfig.Setup(c => c.GetSection("Features:PrincipalBasedAuthEnabled")).Returns(mockSection.Object);

        var principalId = Guid.NewGuid();
        _mockIamClient.Setup(x => x.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePrincipalResponse { PrincipalId = principalId, CreatedAt = DateTime.UtcNow });

        var service = CreateService();
        var request = new CreateCustomerRequest
        {
            FirstName = "IAM",
            LastName = "User",
            Email = "iam@example.com",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act
        var result = await service.CreateAsync(request, "test-actor", "Employee");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(principalId, result.PrincipalId);
        _mockIamClient.Verify(x => x.CreatePrincipalAsync(
            It.Is<CreatePrincipalRequest>(r => r.Email == "iam@example.com"), It.IsAny<CancellationToken>()), Times.Once);

        // Verify persisted in DB
        await using var context = _fixture.CreateDbContext();
        var customerInDb = await context.Customers.FindAsync(result.Id);
        Assert.NotNull(customerInDb);
        Assert.Equal(principalId, customerInDb!.PrincipalId);
    }

    [Fact]
    public async Task CreateAsync_WithIAMFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        // Enable feature flag
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns("true");
        _mockConfig.Setup(c => c.GetSection("Features:PrincipalBasedAuthEnabled")).Returns(mockSection.Object);

        _mockIamClient.Setup(x => x.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("IAM Error"));

        var service = CreateService();
        var request = new CreateCustomerRequest
        {
            FirstName = "Fail",
            LastName = "User",
            Email = "fail@example.com",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, "test-actor", "Employee"));

        // Verify NO customer was created in DB
        await using var context = _fixture.CreateDbContext();
        var count = await context.Customers.CountAsync();
        Assert.Equal(0, count);
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
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("john.doe@example.com", result.Email);
        Assert.Equal("+66-2-123-4567", result.Phone);
        Assert.Equal("Retail", result.Segment);
        Assert.Equal("Bronze", result.Tier);
        Assert.Equal("en", result.PreferredLanguage);
        Assert.Equal("Asia/Bangkok", result.Timezone);
        Assert.NotNull(result.CommunicationPreferences);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5) && result.CreatedAt <= DateTime.UtcNow.AddSeconds(5));
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

        Assert.NotNull(auditLog);
        Assert.Equal("employee-123", auditLog!.ActorId);
        Assert.Equal("Employee", auditLog.ActorType);
        Assert.Equal(AuditAction.Create, auditLog.Action);
        Assert.Equal("Customer", auditLog.EntityType);
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
        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
        Assert.Equal("get.test@example.com", result.Email);
        Assert.Equal("Enterprise", result.Segment);
        Assert.Equal("Gold", result.Tier);
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
        Assert.Null(result);
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
        Assert.Null(result);
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
        Assert.NotNull(result);
        Assert.Equal("Updated", result.FirstName);
        Assert.Equal("+66-2-999-9999", result.Phone);
        Assert.Equal("Silver", result.Tier);
        Assert.Equal("Test", result.LastName); // Unchanged
        Assert.Equal("update.test@example.com", result.Email); // Unchanged
        Assert.True(result.UpdatedAt > created.UpdatedAt);
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
        Assert.Contains("customer was modified by another user", exception.Message);
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

        Assert.Equal(2, auditLogs.Count); // Create + Update
        var updateAudit = auditLogs[1];
        Assert.Equal("customer-456", updateAudit.ActorId);
        Assert.Equal("Customer", updateAudit.ActorType);
        Assert.Equal(AuditAction.Update, updateAudit.Action);
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
        Assert.True(result);

        // Verify customer is marked as deleted
        await using var context = _fixture.CreateDbContext();
        var customer = await context.Customers.FindAsync(created.Id);
        Assert.NotNull(customer);
        Assert.True(customer!.IsDeleted);
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
        Assert.False(result);
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

        Assert.Equal(2, auditLogs.Count); // Create + SoftDelete
        var deleteAudit = auditLogs[1];
        Assert.Equal("manager-999", deleteAudit.ActorId);
        Assert.Equal("Manager", deleteAudit.ActorType);
        Assert.Equal(AuditAction.SoftDelete, deleteAudit.Action);
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
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
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
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Enterprise", result.Items[0].Segment);
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
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(1, page1.Page);
        Assert.Equal(5, page1.TotalCount);

        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(2, page2.Page);
        Assert.Equal(5, page2.TotalCount);
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
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
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
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);

        var pref = result.Items[0];
        Assert.Equal("th", pref.PreferredLanguage);
        Assert.Equal("Asia/Bangkok", pref.Timezone);
        Assert.NotNull(pref.CommunicationPreferences);
    }
}
