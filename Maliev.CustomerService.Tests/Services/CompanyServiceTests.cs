using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Unit tests for CompanyService using real PostgreSQL database
/// Tests business logic, validation, VAT format validation, and audit logging
/// </summary>
[Collection("Database Collection")]
public class CompanyServiceTests
{
    private readonly TestWebApplicationFactory _fixture;
    private readonly Mock<ILogger<CompanyService>> _mockLogger;

    public CompanyServiceTests(TestWebApplicationFactory fixture)
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
    public async Task CreateAsync_WithValidData_ReturnsCompanyResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateCompanyRequest
        {
            Name = "ACME Corporation",
            VatNumber = "TH-1234567890",
            RegistrationNumber = "REG-123456",
            ContactEmail = "contact@acme.com",
            ContactPhone = "+66-2-123-4567",
            Segment = "Enterprise",
            Tier = "Gold"
        };

        // Act
        var result = await service.CreateAsync(request, "test-actor", "Employee");

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("ACME Corporation", result.Name);
        Assert.Equal("TH-1234567890", result.VatNumber);
        Assert.Equal("REG-123456", result.RegistrationNumber);
        Assert.Equal("contact@acme.com", result.ContactEmail);
        Assert.Equal("+66-2-123-4567", result.ContactPhone);
        Assert.Equal("Enterprise", result.Segment);
        Assert.Equal("Gold", result.Tier);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5) && result.CreatedAt <= DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidVatFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateCompanyRequest
        {
            Name = "Bad VAT Company",
            VatNumber = "INVALID-VAT", // Invalid format
            Segment = "Retail",
            Tier = "Bronze"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateAsync(request, "test-actor", "Employee"));

        Assert.Contains("VAT number must be in format", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WithValidVatFormat_Succeeds()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateCompanyRequest
        {
            Name = "Valid VAT Company",
            VatNumber = "US-987654321", // Valid format
            Segment = "Wholesale",
            Tier = "Silver"
        };

        // Act
        var result = await service.CreateAsync(request, "test-actor", "Employee");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("US-987654321", result.VatNumber);
    }

    [Fact]
    public async Task CreateAsync_WithoutVatNumber_Succeeds()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateCompanyRequest
        {
            Name = "No VAT Company",
            VatNumber = null,
            Segment = "Retail",
            Tier = "Bronze"
        };

        // Act
        var result = await service.CreateAsync(request, "test-actor", "Employee");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.VatNumber);
    }

    [Fact]
    public async Task CreateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateCompanyRequest
        {
            Name = "Audit Test Company",
            VatNumber = "TH-5555555555",
            Segment = "Enterprise",
            Tier = "Platinum"
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
        Assert.Equal("Company", auditLog.EntityType);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingCompany_ReturnsCompanyResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Get Test Company",
            VatNumber = "TH-1111111111",
            Segment = "Enterprise",
            Tier = "Gold"
        }, "test-actor", "Employee");

        // Act
        var result = await service.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
        Assert.Equal("Get Test Company", result.Name);
        Assert.Equal("TH-1111111111", result.VatNumber);
        Assert.Equal("Enterprise", result.Segment);
        Assert.Equal("Gold", result.Tier);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentCompany_ReturnsNull()
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
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedCompany()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Update Test Company",
            VatNumber = "TH-2222222222",
            ContactEmail = "old@test.com",
            Segment = "Retail",
            Tier = "Bronze"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateCompanyRequest
        {
            Name = "Updated Company Name",
            ContactEmail = "new@test.com",
            Tier = "Silver",
            Version = created.Version
        };

        // Act
        var result = await service.UpdateAsync(created.Id, updateRequest, "test-actor2", "Employee");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Company Name", result.Name);
        Assert.Equal("new@test.com", result.ContactEmail);
        Assert.Equal("Silver", result.Tier);
        Assert.Equal("TH-2222222222", result.VatNumber); // Unchanged
        Assert.Equal("Retail", result.Segment); // Unchanged
        Assert.True(result.UpdatedAt > created.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidVatFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "VAT Update Test",
            VatNumber = "TH-3333333333",
            Segment = "Retail",
            Tier = "Bronze"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateCompanyRequest
        {
            VatNumber = "INVALID-FORMAT",
            Version = created.Version
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateAsync(created.Id, updateRequest, "test-actor", "Employee"));

        Assert.Contains("VAT number must be in format", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentCompany_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateCompanyRequest
        {
            Name = "Updated Name",
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
        var created = await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Concurrency Test Company",
            Segment = "Retail",
            Tier = "Bronze"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateCompanyRequest
        {
            Name = "Updated Name",
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
        var created = await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Audit Update Company",
            Segment = "Enterprise",
            Tier = "Gold"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateCompanyRequest
        {
            Name = "Updated Audit Company",
            Version = created.Version
        };

        // Act
        await service.UpdateAsync(created.Id, updateRequest, "manager-456", "Manager");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        Assert.Equal(2, auditLogs.Count); // Create + Update
        var updateAudit = auditLogs[1];
        Assert.Equal("manager-456", updateAudit.ActorId);
        Assert.Equal("Manager", updateAudit.ActorType);
        Assert.Equal(AuditAction.Update, updateAudit.Action);
    }

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ReturnsAllCompanies()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Create 3 companies
        for (int i = 0; i < 3; i++)
        {
            await service.CreateAsync(new CreateCompanyRequest
            {
                Name = $"Company{i}",
                Segment = "Retail",
                Tier = "Bronze"
            }, "test-actor", "Employee");
        }

        // Act
        var (companies, totalCount) = await service.GetAllAsync(page: 1, pageSize: 50);

        // Assert
        Assert.NotNull(companies);
        Assert.Equal(3, companies.Count);
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithSegmentFilter_ReturnsFilteredCompanies()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Retail Company",
            Segment = "Retail",
            Tier = "Bronze"
        }, "test-actor", "Employee");

        await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Enterprise Company",
            Segment = "Enterprise",
            Tier = "Gold"
        }, "test-actor", "Employee");

        // Act
        var (companies, totalCount) = await service.GetAllAsync(page: 1, pageSize: 50, segment: "Enterprise");

        // Assert
        Assert.Single(companies);
        Assert.Equal(1, totalCount);
        Assert.Equal("Enterprise", companies[0].Segment);
        Assert.Equal("Enterprise Company", companies[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_WithTierFilter_ReturnsFilteredCompanies()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Bronze Company",
            Segment = "Retail",
            Tier = "Bronze"
        }, "test-actor", "Employee");

        await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Gold Company 1",
            Segment = "Enterprise",
            Tier = "Gold"
        }, "test-actor", "Employee");

        await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Gold Company 2",
            Segment = "Wholesale",
            Tier = "Gold"
        }, "test-actor", "Employee");

        // Act
        var (companies, totalCount) = await service.GetAllAsync(page: 1, pageSize: 50, tier: "Gold");

        // Assert
        Assert.Equal(2, companies.Count);
        Assert.Equal(2, totalCount);
        Assert.All(companies, c => Assert.Equal("Gold", c.Tier));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Create 5 companies
        for (int i = 0; i < 5; i++)
        {
            await service.CreateAsync(new CreateCompanyRequest
            {
                Name = $"Company{i}",
                Segment = "Retail",
                Tier = "Bronze"
            }, "test-actor", "Employee");
        }

        // Act
        var (page1Companies, page1Total) = await service.GetAllAsync(page: 1, pageSize: 2);
        var (page2Companies, page2Total) = await service.GetAllAsync(page: 2, pageSize: 2);

        // Assert
        Assert.Equal(2, page1Companies.Count);
        Assert.Equal(5, page1Total);

        Assert.Equal(2, page2Companies.Count);
        Assert.Equal(5, page2Total);
    }

    [Fact]
    public async Task GetAllAsync_WithSegmentAndTierFilter_ReturnsFilteredCompanies()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Enterprise Gold 1",
            Segment = "Enterprise",
            Tier = "Gold"
        }, "test-actor", "Employee");

        await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Enterprise Silver",
            Segment = "Enterprise",
            Tier = "Silver"
        }, "test-actor", "Employee");

        await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Retail Gold",
            Segment = "Retail",
            Tier = "Gold"
        }, "test-actor", "Employee");

        // Act
        var (companies, totalCount) = await service.GetAllAsync(
            page: 1,
            pageSize: 50,
            segment: "Enterprise",
            tier: "Gold");

        // Assert
        Assert.Single(companies);
        Assert.Equal(1, totalCount);
        Assert.Equal("Enterprise Gold 1", companies[0].Name);
        Assert.Equal("Enterprise", companies[0].Segment);
        Assert.Equal("Gold", companies[0].Tier);
    }

    [Fact]
    public async Task GetWithCustomersAsync_WithExistingCompany_ReturnsCompanyAndCustomers()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Create company
        var company = await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Company With Customers",
            Segment = "Enterprise",
            Tier = "Gold"
        }, "test-actor", "Employee");

        // Create customers associated with the company
        await using (var context = _fixture.CreateDbContext())
        {
            var customer1 = new Customer
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john@company.com",
                Phone = "+66-2-123-4567",
                Segment = "Enterprise",
                Tier = "Gold",
                PreferredLanguage = "en",
                Timezone = "Asia/Bangkok",
                CompanyId = company.Id,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var customer2 = new Customer
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@company.com",
                Phone = "+66-2-123-4568",
                Segment = "Enterprise",
                Tier = "Gold",
                PreferredLanguage = "en",
                Timezone = "Asia/Bangkok",
                CompanyId = company.Id,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Customers.Add(customer1);
            context.Customers.Add(customer2);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.GetWithCustomersAsync(company.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(company.Id, result!.Value.Company.Id);
        Assert.Equal(2, result.Value.Customers.Count);
        Assert.All(result.Value.Customers, c => Assert.Equal(company.Id, c.CompanyId));
    }

    [Fact]
    public async Task GetWithCustomersAsync_WithNonExistentCompany_ReturnsNull()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GetWithCustomersAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWithCustomersAsync_ExcludesDeletedCustomers()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Create company
        var company = await service.CreateAsync(new CreateCompanyRequest
        {
            Name = "Company With Mixed Customers",
            Segment = "Enterprise",
            Tier = "Gold"
        }, "test-actor", "Employee");

        // Create active and deleted customers
        await using (var context = _fixture.CreateDbContext())
        {
            var activeCustomer = new Customer
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                FirstName = "Active",
                LastName = "Customer",
                Email = "active@company.com",
                Phone = "+66-2-123-4567",
                Segment = "Enterprise",
                Tier = "Gold",
                PreferredLanguage = "en",
                Timezone = "Asia/Bangkok",
                CompanyId = company.Id,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var deletedCustomer = new Customer
            {
                Id = Guid.NewGuid(),
                PrincipalId = Guid.NewGuid(),
                FirstName = "Deleted",
                LastName = "Customer",
                Email = "deleted@company.com",
                Phone = "+66-2-123-4568",
                Segment = "Enterprise",
                Tier = "Gold",
                PreferredLanguage = "en",
                Timezone = "Asia/Bangkok",
                CompanyId = company.Id,
                IsDeleted = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Customers.Add(activeCustomer);
            context.Customers.Add(deletedCustomer);
            await context.SaveChangesAsync();
        }

        // Act
        var result = await service.GetWithCustomersAsync(company.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.Value.Customers);
        Assert.Equal("Active", result.Value.Customers[0].FirstName);
    }
}
