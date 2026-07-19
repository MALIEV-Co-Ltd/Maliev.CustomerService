using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Application.DTOs;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class TierSettingsControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public TierSettingsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsActiveTierSettings()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.read" });

        // Seed tier settings
        using var dbContext = _factory.GetDbContext();
        dbContext.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = Guid.NewGuid(),
            TierName = "Gold",
            MinPurchaseValue = 100000,
            MinOrderCount = 10,
            DiscountPercentage = 10,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync("/customer/v1/tier-settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<TierSettingsResponse>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Gold", result[0].TierName);
    }

    [Fact]
    public async Task Create_ReturnsCreatedResult()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.manage" });

        var request = new
        {
            tierName = "Platinum",
            minPurchaseValue = 500000m,
            minOrderCount = 50,
            discountPercentage = 15,
            freeShippingMinOrder = 10000m,
            coinRewardPercentage = 10,
            validFrom = DateTime.UtcNow,
            validTo = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/customer/v1/tier-settings", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TierSettingsResponse>();
        Assert.NotNull(result);
        Assert.Equal("Platinum", result.TierName);
    }

    [Fact]
    public async Task GetById_ReturnsTierSetting()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.read" });

        var tierId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = tierId,
            TierName = "Silver",
            MinPurchaseValue = 50000,
            MinOrderCount = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/tier-settings/{tierId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TierSettingsResponse>();
        Assert.NotNull(result);
        Assert.Equal("Silver", result.TierName);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.read" });

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/customer/v1/tier-settings/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsUpdatedTierSetting()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.manage" });

        var tierId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = tierId,
            TierName = "Silver",
            MinPurchaseValue = 50000,
            MinOrderCount = 5,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await dbContext.SaveChangesAsync();

        var request = new
        {
            tierName = "Silver",
            minPurchaseValue = 75000m,
            minOrderCount = 8,
            discountPercentage = 7,
            freeShippingMinOrder = 3000m,
            coinRewardPercentage = 3,
            validFrom = DateTime.UtcNow.AddYears(-1),
            validTo = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await client.PutAsJsonAsync($"/customer/v1/tier-settings/{tierId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TierSettingsResponse>();
        Assert.NotNull(result);
        Assert.Equal(75000m, result.MinPurchaseValue);
    }

    [Fact]
    public async Task Update_ReturnsNotFound()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.manage" });

        var nonExistentId = Guid.NewGuid();
        var request = new
        {
            tierName = "Gold",
            minPurchaseValue = 100000m,
            minOrderCount = 10,
            validFrom = DateTime.UtcNow,
            validTo = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var response = await client.PutAsJsonAsync($"/customer/v1/tier-settings/{nonExistentId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsForbidden_WhenMissingPermission()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null);

        // Act
        var response = await client.GetAsync("/customer/v1/tier-settings");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
