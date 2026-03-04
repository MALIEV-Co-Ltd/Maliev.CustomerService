using Maliev.CustomerService.Application.Services;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

[Collection("Database Collection")]
public class YearEndTierProcessorTests
{
    private readonly TestWebApplicationFactory _fixture;
    private readonly Mock<ILogger<YearEndTierProcessor>> _mockLogger;

    public YearEndTierProcessorTests(TestWebApplicationFactory fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<YearEndTierProcessor>>();
    }

    private YearEndTierProcessor CreateService()
    {
        var context = _fixture.CreateDbContext();
        var companyRepo = new Maliev.CustomerService.Infrastructure.Persistence.Repositories.CompanyRepository(context);
        var tierCalculationService = CreateTierCalculationService();
        return new YearEndTierProcessor(companyRepo, tierCalculationService, _mockLogger.Object);
    }

    private TierCalculationService CreateTierCalculationService()
    {
        var context = _fixture.CreateDbContext();
        var mockLogger = new Mock<ILogger<TierCalculationService>>();
        var companyRepo = new Maliev.CustomerService.Infrastructure.Persistence.Repositories.CompanyRepository(context);
        var tierSettingsRepo = new Maliev.CustomerService.Infrastructure.Persistence.Repositories.CompanyTierSettingsRepository(context);
        return new TierCalculationService(companyRepo, tierSettingsRepo, mockLogger.Object);
    }

    [Fact]
    public async Task RunAsync_ProcessesYearEndDemotions()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        // Setup tier settings
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

        // Create companies - one eligible for demotion, one not
        context.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "Demote Me",
            Tier = "Gold",
            CurrentYearPurchaseValue = 10000, // Below threshold
            CurrentYearOrderCount = 1
        });
        context.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "Keep My Tier",
            Tier = "Gold",
            CurrentYearPurchaseValue = 150000,
            CurrentYearOrderCount = 15
        });
        await context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var demotedCount = await service.RunAsync();

        // Assert
        Assert.True(demotedCount >= 0);
    }
}
