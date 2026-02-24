using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Tests for the new company search functionality including address joining
/// </summary>
[Collection("Database Collection")]
public class CompanySearchTests
{
    private readonly TestWebApplicationFactory _fixture;
    private readonly Mock<ILogger<CompanyService>> _mockLogger;

    public CompanySearchTests(TestWebApplicationFactory fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<CompanyService>>();
    }

    private CompanyService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new CompanyService(context, Mock.Of<IIAMClient>(), _mockLogger.Object);
    }

    [Fact]
    public async Task SearchWithAddressAsync_ReturnsMatchingCompaniesWithDefaultBillingAddress()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        using (var context = _fixture.CreateDbContext())
        {
            // 1. Create a company
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "ABC Logistics",
                VatNumber = "TH-1111111111",
                Segment = "Wholesale",
                Tier = "Gold"
            };
            context.Companies.Add(company);

            // 2. Add multiple addresses, one default billing
            context.Addresses.Add(new Address
            {
                OwnerType = OwnerType.Company,
                OwnerId = company.Id,
                Type = AddressType.Billing,
                AddressLine1 = "123 Main St",
                City = "Bangkok",
                StateProvince = "Bangkok",
                PostalCode = "10110",
                IsDefault = true
            });

            // A non-default address (should be ignored)
            context.Addresses.Add(new Address
            {
                OwnerType = OwnerType.Company,
                OwnerId = company.Id,
                Type = AddressType.Billing,
                AddressLine1 = "Old Office",
                City = "Bangkok",
                StateProvince = "Bangkok",
                PostalCode = "10110",
                IsDefault = false
            });

            // 3. Create another company with no address
            context.Companies.Add(new Company
            {
                Id = Guid.NewGuid(),
                Name = "ABC Trading",
                VatNumber = "TH-2222222222",
                Segment = "Retail",
                Tier = "Bronze"
            });

            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.SearchWithAddressAsync("ABC");

        // Assert
        Assert.Equal(2, results.Count);

        var logistics = results.FirstOrDefault(r => r.Name == "ABC Logistics");
        Assert.NotNull(logistics);
        Assert.NotNull(logistics.BillingAddress);
        Assert.Equal("123 Main St", logistics.BillingAddress.AddressLine1);
        Assert.Equal(CompanySource.Internal, logistics.Source);

        var trading = results.FirstOrDefault(r => r.Name == "ABC Trading");
        Assert.NotNull(trading);
        Assert.Null(trading.BillingAddress);
    }

    [Fact]
    public async Task SearchWithAddressAsync_SearchByVatNumber_Succeeds()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        using (var context = _fixture.CreateDbContext())
        {
            context.Companies.Add(new Company
            {
                Name = "Vat Company",
                VatNumber = "TH-9998887776"
            });
            await context.SaveChangesAsync();
        }

        // Act
        var results = await service.SearchWithAddressAsync("999888");

        // Assert
        Assert.Single(results);
        Assert.Equal("Vat Company", results[0].Name);
    }
}
