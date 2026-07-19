using Maliev.CustomerService.Application.DTOs;
using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Application.Services;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

[Collection("Database Collection")]
public class TierCalculationServiceTests
{
    private readonly TestWebApplicationFactory _fixture;
    private readonly Mock<ILogger<TierCalculationService>> _mockLogger;

    public TierCalculationServiceTests(TestWebApplicationFactory fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<TierCalculationService>>();
    }

    private TierCalculationService CreateService()
    {
        var context = _fixture.CreateDbContext();
        ICompanyRepository companyRepo = new Maliev.CustomerService.Infrastructure.Persistence.Repositories.CompanyRepository(context);
        ICompanyTierSettingsRepository tierSettingsRepo = new Maliev.CustomerService.Infrastructure.Persistence.Repositories.CompanyTierSettingsRepository(context);
        return new TierCalculationService(companyRepo, tierSettingsRepo, _mockLogger.Object);
    }

    [Fact]
    public async Task GetTierSettingsAsync_WithActiveSettings_ReturnsSettings()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        // Seed tier settings
        await using var context = _fixture.CreateDbContext();
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            MinPurchaseValue = 100000,
            MinOrderCount = 10,
            DiscountPercentage = 10,
            FreeShippingMinOrder = 5000,
            CoinRewardPercentage = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetTierSettingsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Gold", result[0].TierName);
        Assert.Equal(100000, result[0].MinPurchaseValue);
    }

    [Fact]
    public async Task CalculateTierAsync_WithHighPurchaseValue_ReturnsGold()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        // Seed tier settings
        await using var context = _fixture.CreateDbContext();
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            MinPurchaseValue = 100000,
            MinOrderCount = 10,
            DiscountPercentage = 10,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Silver",
            MinPurchaseValue = 50000,
            MinOrderCount = 5,
            DiscountPercentage = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Classic",
            MinPurchaseValue = 0,
            MinOrderCount = 0,
            DiscountPercentage = 0,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.CalculateTierAsync(150000, 15);

        // Assert
        Assert.Equal("Gold", result);
    }

    [Fact]
    public async Task CalculateTierAsync_WithMediumPurchaseValue_ReturnsSilver()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            MinPurchaseValue = 100000,
            MinOrderCount = 10,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Silver",
            MinPurchaseValue = 50000,
            MinOrderCount = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Classic",
            MinPurchaseValue = 0,
            MinOrderCount = 0,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.CalculateTierAsync(75000, 8);

        // Assert
        Assert.Equal("Silver", result);
    }

    [Fact]
    public async Task CalculateTierAsync_WithLowPurchaseValue_ReturnsClassic()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Silver",
            MinPurchaseValue = 50000,
            MinOrderCount = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Classic",
            MinPurchaseValue = 0,
            MinOrderCount = 0,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.CalculateTierAsync(10000, 1);

        // Assert
        Assert.Equal("Classic", result);
    }

    [Fact]
    public async Task CalculateTierAsync_WithInsufficientOrderCount_ReturnsLowerTier()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            MinPurchaseValue = 100000,
            MinOrderCount = 10,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Silver",
            MinPurchaseValue = 50000,
            MinOrderCount = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Classic",
            MinPurchaseValue = 0,
            MinOrderCount = 0,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act - High purchase value but low order count (3) - should return Classic because Silver requires 5 orders
        var result = await service.CalculateTierAsync(150000, 3);

        // Assert - Silver requires 5 orders, so should return Classic
        Assert.Equal("Classic", result);
    }

    [Fact]
    public async Task ApplyTierAsync_WithExistingCompany_UpdatesTier()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        // Create company
        await using var context = _fixture.CreateDbContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            CurrentYearPurchaseValue = 150000,
            CurrentYearOrderCount = 15,
            Tier = "Classic"
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            MinPurchaseValue = 100000,
            MinOrderCount = 10,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Silver",
            MinPurchaseValue = 50000,
            MinOrderCount = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Classic",
            MinPurchaseValue = 0,
            MinOrderCount = 0,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.ApplyTierAsync(companyId);

        // Assert
        Assert.True(result);

        await using var verifyContext = _fixture.CreateDbContext();
        var updatedCompany = await verifyContext.Companies.FindAsync(companyId);
        Assert.NotNull(updatedCompany);
        Assert.Equal("Gold", updatedCompany.Tier);
    }

    [Fact]
    public async Task ApplyTierAsync_WithNonExistentCompany_ReturnsFalse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.ApplyTierAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ApplyTierAsync_WithSameTier_ReturnsFalse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            CurrentYearPurchaseValue = 50000,
            CurrentYearOrderCount = 5,
            Tier = "Silver"
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Silver",
            MinPurchaseValue = 50000,
            MinOrderCount = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Classic",
            MinPurchaseValue = 0,
            MinOrderCount = 0,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.ApplyTierAsync(companyId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ResetYearlyValuesAsync_ResetsAllCompanies()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
        context.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "Company 1",
            CurrentYearPurchaseValue = 100000,
            CurrentYearOrderCount = 10,
            Tier = "Gold"
        });
        context.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "Company 2",
            CurrentYearPurchaseValue = 50000,
            CurrentYearOrderCount = 5,
            Tier = "Silver"
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        await service.ResetYearlyValuesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateDbContext();
        var companies = await verifyContext.Companies.ToListAsync();
        Assert.All(companies, c =>
        {
            Assert.Equal(0, c.CurrentYearPurchaseValue);
            Assert.Equal(0, c.CurrentYearOrderCount);
        });
    }

    [Fact]
    public async Task GetCompanyWithTierAsync_WithExistingCompany_ReturnsTierInfo()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            CurrentYearPurchaseValue = 75000,
            CurrentYearOrderCount = 8,
            Tier = "Silver"
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            MinPurchaseValue = 100000,
            MinOrderCount = 10,
            DiscountPercentage = 10,
            FreeShippingMinOrder = 5000,
            CoinRewardPercentage = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Silver",
            MinPurchaseValue = 50000,
            MinOrderCount = 5,
            DiscountPercentage = 5,
            FreeShippingMinOrder = 2000,
            CoinRewardPercentage = 2,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Classic",
            MinPurchaseValue = 0,
            MinOrderCount = 0,
            DiscountPercentage = 0,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetCompanyWithTierAsync(companyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Silver", result.Tier);
        Assert.Equal(75000, result.CurrentYearPurchaseValue);
        Assert.Equal(8, result.CurrentYearOrderCount);
        Assert.Equal(5, result.DiscountPercentage);
        Assert.NotNull(result.NextTierProgress);
        Assert.Equal("Gold", result.NextTierProgress.NextTierName);
    }

    [Fact]
    public async Task GetCompanyWithTierAsync_WithNonExistentCompany_ReturnsNull()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.GetCompanyWithTierAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDiscountPercentageAsync_WithValidTier_ReturnsDiscount()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            DiscountPercentage = 10,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetDiscountPercentageAsync("Gold");

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task GetDiscountPercentageAsync_WithInvalidTier_ReturnsZero()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var result = await service.GetDiscountPercentageAsync("NonExistent");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetFreeShippingThresholdAsync_WithValidTier_ReturnsThreshold()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            FreeShippingMinOrder = 5000,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetFreeShippingThresholdAsync("Gold");

        // Assert
        Assert.Equal(5000, result);
    }

    [Fact]
    public async Task GetCoinRewardPercentageAsync_WithValidTier_ReturnsPercentage()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        await using var context = _fixture.CreateDbContext();
        context.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            CoinRewardPercentage = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetCoinRewardPercentageAsync("Gold");

        // Assert
        Assert.Equal(5, result);
    }
}
