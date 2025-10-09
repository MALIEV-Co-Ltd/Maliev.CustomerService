using FluentAssertions;
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
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<ILogger<CompanyService>> _mockLogger;

    public CompanyServiceTests(TestDatabaseFixture fixture)
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
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("ACME Corporation");
        result.VatNumber.Should().Be("TH-1234567890");
        result.RegistrationNumber.Should().Be("REG-123456");
        result.ContactEmail.Should().Be("contact@acme.com");
        result.ContactPhone.Should().Be("+66-2-123-4567");
        result.Segment.Should().Be("Enterprise");
        result.Tier.Should().Be("Gold");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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

        exception.Message.Should().Contain("VAT number must be in format");
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
        result.Should().NotBeNull();
        result.VatNumber.Should().Be("US-987654321");
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
        result.Should().NotBeNull();
        result.VatNumber.Should().BeNull();
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

        auditLog.Should().NotBeNull();
        auditLog!.ActorId.Should().Be("employee-123");
        auditLog.ActorType.Should().Be("Employee");
        auditLog.Action.Should().Be(AuditAction.Create);
        auditLog.EntityType.Should().Be("Company");
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
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("Get Test Company");
        result.VatNumber.Should().Be("TH-1111111111");
        result.Segment.Should().Be("Enterprise");
        result.Tier.Should().Be("Gold");
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
        result.Should().BeNull();
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
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Company Name");
        result.ContactEmail.Should().Be("new@test.com");
        result.Tier.Should().Be("Silver");
        result.VatNumber.Should().Be("TH-2222222222"); // Unchanged
        result.Segment.Should().Be("Retail"); // Unchanged
        result.UpdatedAt.Should().BeAfter(created.UpdatedAt);
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

        exception.Message.Should().Contain("VAT number must be in format");
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

        exception.Message.Should().Contain("modified by another user");
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

        auditLogs.Should().HaveCount(2); // Create + Update
        var updateAudit = auditLogs[1];
        updateAudit.ActorId.Should().Be("manager-456");
        updateAudit.ActorType.Should().Be("Manager");
        updateAudit.Action.Should().Be(AuditAction.Update);
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
        companies.Should().NotBeNull();
        companies.Should().HaveCount(3);
        totalCount.Should().Be(3);
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
        companies.Should().HaveCount(1);
        totalCount.Should().Be(1);
        companies[0].Segment.Should().Be("Enterprise");
        companies[0].Name.Should().Be("Enterprise Company");
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
        companies.Should().HaveCount(2);
        totalCount.Should().Be(2);
        companies.Should().OnlyContain(c => c.Tier == "Gold");
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
        page1Companies.Should().HaveCount(2);
        page1Total.Should().Be(5);

        page2Companies.Should().HaveCount(2);
        page2Total.Should().Be(5);
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
        companies.Should().HaveCount(1);
        totalCount.Should().Be(1);
        companies[0].Name.Should().Be("Enterprise Gold 1");
        companies[0].Segment.Should().Be("Enterprise");
        companies[0].Tier.Should().Be("Gold");
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
        result.Should().NotBeNull();
        result!.Value.Company.Id.Should().Be(company.Id);
        result.Value.Customers.Should().HaveCount(2);
        result.Value.Customers.Should().OnlyContain(c => c.CompanyId == company.Id);
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
        result.Should().BeNull();
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
        result.Should().NotBeNull();
        result!.Value.Customers.Should().HaveCount(1);
        result.Value.Customers[0].FirstName.Should().Be("Active");
    }
}
