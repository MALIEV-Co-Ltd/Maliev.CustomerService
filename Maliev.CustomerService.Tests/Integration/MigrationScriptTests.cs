using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Api.Scripts;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class MigrationScriptTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _databaseFixture;
    private TestWebApplicationFactory _factory = null!;

    public MigrationScriptTests(TestDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();
        await _factory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ExecuteAsync_MigratesExistingCustomersWithoutPrincipalId()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        using var dbContext = _factory.GetDbContext();

        // Create test customers without PrincipalId
        var customers = new List<Customer>
        {
            new() { FirstName = "User", LastName = "1", Email = "user1@example.com" },
            new() { FirstName = "User", LastName = "2", Email = "user2@example.com" },
            new() { FirstName = "User", LastName = "3", Email = "user3@example.com", PrincipalId = Guid.NewGuid() } // Already migrated
        };
        await dbContext.Customers.AddRangeAsync(customers);
        await dbContext.SaveChangesAsync();

        var iamClientMock = new Mock<IIAMClient>();
        iamClientMock
            .Setup(x => x.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreatePrincipalRequest req, CancellationToken ct) => new CreatePrincipalResponse
            {
                PrincipalId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            });

        var loggerMock = new Mock<ILogger<MigrateToPrincipalsScript>>();
        var script = new MigrateToPrincipalsScript(dbContext, iamClientMock.Object, loggerMock.Object);

        // Act
        await script.ExecuteAsync();

        // Assert
        var updatedCustomers = await dbContext.Customers.ToListAsync();
        Assert.All(updatedCustomers, c => Assert.NotEqual(Guid.Empty, c.PrincipalId));

        // Verify IAM client was called only for customers without PrincipalId
        iamClientMock.Verify(x => x.CreatePrincipalAsync(
            It.Is<CreatePrincipalRequest>(r => r.Email == "user1@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        iamClientMock.Verify(x => x.CreatePrincipalAsync(
            It.Is<CreatePrincipalRequest>(r => r.Email == "user2@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        iamClientMock.Verify(x => x.CreatePrincipalAsync(
            It.Is<CreatePrincipalRequest>(r => r.Email == "user3@example.com"), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesIAMFailuresAndContinues()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        using var dbContext = _factory.GetDbContext();

        var customers = new List<Customer>
        {
            new() { FirstName = "Fail", LastName = "User", Email = "fail@example.com" },
            new() { FirstName = "Success", LastName = "User", Email = "success@example.com" }
        };
        await dbContext.Customers.AddRangeAsync(customers);
        await dbContext.SaveChangesAsync();

        var iamClientMock = new Mock<IIAMClient>();
        iamClientMock
            .Setup(x => x.CreatePrincipalAsync(It.Is<CreatePrincipalRequest>(r => r.Email == "fail@example.com"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("IAM Service Down"));

        iamClientMock
            .Setup(x => x.CreatePrincipalAsync(It.Is<CreatePrincipalRequest>(r => r.Email == "success@example.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePrincipalResponse { PrincipalId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });

        var loggerMock = new Mock<ILogger<MigrateToPrincipalsScript>>();
        var script = new MigrateToPrincipalsScript(dbContext, iamClientMock.Object, loggerMock.Object);

        // Act
        await script.ExecuteAsync();

        // Assert
        var dbCustomers = await dbContext.Customers.ToListAsync();
        var failUser = dbCustomers.First(c => c.Email == "fail@example.com");
        var successUser = dbCustomers.First(c => c.Email == "success@example.com");

        Assert.Equal(Guid.Empty, failUser.PrincipalId);
        Assert.NotEqual(Guid.Empty, successUser.PrincipalId);
    }
}
