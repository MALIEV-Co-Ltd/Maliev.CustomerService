using FluentAssertions;
using Maliev.CustomerService.Api.Models.Users;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Unit tests for UserService using real PostgreSQL database
/// Tests user account management, password validation, role management
/// </summary>
[Collection("Database Collection")]
public class UserServiceTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<ILogger<Api.Services.UserService>> _mockLogger;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<IUserStore<ApplicationUser>> _mockUserStore;
    private readonly Mock<Api.Services.MetricsService> _mockMetricsService;

    public UserServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<Api.Services.UserService>>();
        _mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockMetricsService = new Mock<Api.Services.MetricsService>(MockBehavior.Loose, new object[] { Mock.Of<IConfiguration>() });

#pragma warning disable CS8625 // Null literal conversion warnings for mock setup
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            _mockUserStore.Object, null, null, null, null, null, null, null, null);

        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object, null, null, null, null, null, null);
#pragma warning restore CS8625
    }

    private Api.Services.UserService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new Api.Services.UserService(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            context,
            _mockLogger.Object,
            _mockMetricsService.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsUserResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@example.com"
        };

        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) =>
            {
                user.Id = testUser.Id;
                user.UserName = testUser.UserName;
                user.Email = testUser.Email;
            });

        _mockUserManager.Setup(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        var request = new CreateUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "SecureP@ssw0rd!",
            Roles = new List<string> { "Customer" }
        };

        // Act
        var result = await service.CreateAsync(request, "admin-actor", "Admin");

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.Roles.Should().Contain("Customer");

        _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "SecureP@ssw0rd!"), Times.Once);
        _mockUserManager.Verify(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.Is<IEnumerable<string>>(r => r.Contains("Customer"))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Username already exists" }));

        var request = new CreateUserRequest
        {
            Username = "duplicate",
            Email = "duplicate@example.com",
            Password = "SecureP@ssw0rd!",
            Roles = new List<string> { "Customer" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateAsync(request, "admin-actor", "Admin"));
    }

    [Fact]
    public async Task CreateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        var testUserId = Guid.NewGuid().ToString();

        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) => user.Id = testUserId);

        _mockUserManager.Setup(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        var request = new CreateUserRequest
        {
            Username = "audituser",
            Email = "audit@example.com",
            Password = "SecureP@ssw0rd!",
            Roles = new List<string> { "Employee" }
        };

        // Act
        var result = await service.CreateAsync(request, "admin-123", "Admin");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLog = await context.AuditLogs
            .Where(a => a.EntityId == testUserId && a.Action == AuditAction.Create)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.ActorId.Should().Be("admin-123");
        auditLog.ActorType.Should().Be("Admin");
        auditLog.EntityType.Should().Be("ApplicationUser");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithValidCredentials_ReturnsSuccessResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "validuser",
            Email = "valid@example.com"
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.Users.Add(testUser);
            await context.SaveChangesAsync();
        }

        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByNameAsync("validuser"))
            .ReturnsAsync(testUser);

        _mockUserManager.Setup(m => m.CheckPasswordAsync(testUser, "CorrectPassword!"))
            .ReturnsAsync(true);

        _mockUserManager.Setup(m => m.GetRolesAsync(testUser))
            .ReturnsAsync(new List<string> { "Customer" });

        var request = new ValidateCredentialsRequest
        {
            Username = "validuser",
            Password = "CorrectPassword!"
        };

        // Act
        var result = await service.ValidateCredentialsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(testUser.Id);
        result.Username.Should().Be("validuser");
        result.Roles.Should().Contain("Customer");

        // Verify last_login_at was updated
        await using (var context = _fixture.CreateDbContext())
        {
            var updatedUser = await context.Users.FindAsync(testUser.Id);
            updatedUser!.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithInvalidPassword_ReturnsFailureResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "invalidpassuser",
            Email = "invalidpass@example.com"
        };

        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByNameAsync("invalidpassuser"))
            .ReturnsAsync(testUser);

        _mockUserManager.Setup(m => m.CheckPasswordAsync(testUser, "WrongPassword!"))
            .ReturnsAsync(false);

        var request = new ValidateCredentialsRequest
        {
            Username = "invalidpassuser",
            Password = "WrongPassword!"
        };

        // Act
        var result = await service.ValidateCredentialsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.Username.Should().BeNull();
        result.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithNonExistentUser_ReturnsFailureResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByNameAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new ValidateCredentialsRequest
        {
            Username = "nonexistent",
            Password = "AnyPassword!"
        };

        // Act
        var result = await service.ValidateCredentialsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.UserId.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithValidCurrentPassword_ReturnsTrue()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "passworduser",
            Email = "password@example.com"
        };

        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByIdAsync(testUser.Id))
            .ReturnsAsync(testUser);

        _mockUserManager.Setup(m => m.ChangePasswordAsync(testUser, "OldPass!", "NewPass!"))
            .ReturnsAsync(IdentityResult.Success);

        var request = new UpdatePasswordRequest
        {
            CurrentPassword = "OldPass!",
            NewPassword = "NewPass!"
        };

        // Act
        var result = await service.UpdatePasswordAsync(testUser.Id, request, "user-self", "Customer");

        // Assert
        result.Should().BeTrue();
        _mockUserManager.Verify(m => m.ChangePasswordAsync(testUser, "OldPass!", "NewPass!"), Times.Once);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WithInvalidCurrentPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "passworduser",
            Email = "password@example.com"
        };

        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByIdAsync(testUser.Id))
            .ReturnsAsync(testUser);

        _mockUserManager.Setup(m => m.ChangePasswordAsync(testUser, "WrongPass!", "NewPass!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));

        var request = new UpdatePasswordRequest
        {
            CurrentPassword = "WrongPass!",
            NewPassword = "NewPass!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdatePasswordAsync(testUser.Id, request, "user-self", "Customer"));
    }

    [Fact]
    public async Task UpdatePasswordAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "auditpassword",
            Email = "auditpass@example.com"
        };

        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByIdAsync(testUser.Id))
            .ReturnsAsync(testUser);

        _mockUserManager.Setup(m => m.ChangePasswordAsync(testUser, "OldPass!", "NewPass!"))
            .ReturnsAsync(IdentityResult.Success);

        var request = new UpdatePasswordRequest
        {
            CurrentPassword = "OldPass!",
            NewPassword = "NewPass!"
        };

        // Act
        await service.UpdatePasswordAsync(testUser.Id, request, "admin-789", "Admin");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLog = await context.AuditLogs
            .Where(a => a.EntityId == testUser.Id && a.Action == AuditAction.Update)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.ActorId.Should().Be("admin-789");
        auditLog.ActorType.Should().Be("Admin");
        auditLog.EntityType.Should().Be("ApplicationUser");
    }

    [Fact]
    public async Task UpdateRolesAsync_WithValidRoles_ReturnsUpdatedUser()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "roleuser",
            Email = "role@example.com"
        };

        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByIdAsync(testUser.Id))
            .ReturnsAsync(testUser);

        _mockUserManager.Setup(m => m.GetRolesAsync(testUser))
            .ReturnsAsync(new List<string> { "Customer" });

        _mockUserManager.Setup(m => m.RemoveFromRolesAsync(testUser, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(m => m.AddToRolesAsync(testUser, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        var request = new UpdateRolesRequest
        {
            Roles = new List<string> { "Employee", "Manager" }
        };

        // Act
        var result = await service.UpdateRolesAsync(testUser.Id, request, "admin-456", "Admin");

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("roleuser");

        _mockUserManager.Verify(m => m.RemoveFromRolesAsync(testUser, It.Is<IEnumerable<string>>(r => r.Contains("Customer"))), Times.Once);
        _mockUserManager.Verify(m => m.AddToRolesAsync(testUser, It.Is<IEnumerable<string>>(r => r.Contains("Employee") && r.Contains("Manager"))), Times.Once);
    }

    [Fact]
    public async Task UpdateRolesAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByIdAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new UpdateRolesRequest
        {
            Roles = new List<string> { "Employee" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.UpdateRolesAsync("nonexistent", request, "admin", "Admin"));
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ReturnsUserResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        var testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "getuser",
            Email = "get@example.com"
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.Users.Add(testUser);
            await context.SaveChangesAsync();
        }

        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByIdAsync(testUser.Id))
            .ReturnsAsync(testUser);

        _mockUserManager.Setup(m => m.GetRolesAsync(testUser))
            .ReturnsAsync(new List<string> { "Customer" });

        // Act
        var result = await service.GetByIdAsync(testUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("getuser");
        result.Email.Should().Be("get@example.com");
        result.Roles.Should().Contain("Customer");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        _mockUserManager.Setup(m => m.FindByIdAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await service.GetByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsUsersList()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        var users = new List<ApplicationUser>
        {
            new() { Id = Guid.NewGuid().ToString(), UserName = "user1", Email = "user1@example.com", LastLoginAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid().ToString(), UserName = "user2", Email = "user2@example.com", LastLoginAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = Guid.NewGuid().ToString(), UserName = "user3", Email = "user3@example.com", LastLoginAt = DateTime.UtcNow.AddDays(-10) }
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        var service = CreateService();

        foreach (var user in users)
        {
            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Customer" });
        }

        // Act
        var (resultUsers, totalCount) = await service.GetAllAsync(1, 10);

        // Assert
        resultUsers.Should().HaveCount(3);
        totalCount.Should().Be(3);
        resultUsers.Should().OnlyContain(u => u.Roles.Contains("Customer"));
    }

    [Fact]
    public async Task GetAllAsync_WithLastLoginBeforeFilter_ReturnsFilteredUsers()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();

        var users = new List<ApplicationUser>
        {
            new() { Id = Guid.NewGuid().ToString(), UserName = "recent", Email = "recent@example.com", LastLoginAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid().ToString(), UserName = "old", Email = "old@example.com", LastLoginAt = DateTime.UtcNow.AddDays(-20) }
        };

        await using (var context = _fixture.CreateDbContext())
        {
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        var service = CreateService();

        var oldUser = users[1];
        _mockUserManager.Setup(m => m.GetRolesAsync(oldUser))
            .ReturnsAsync(new List<string> { "Customer" });

        // Act - get users who haven't logged in for more than 10 days
        var (resultUsers, totalCount) = await service.GetAllAsync(1, 10, lastLoginBefore: DateTime.UtcNow.AddDays(-10));

        // Assert
        resultUsers.Should().HaveCount(1);
        resultUsers[0].Username.Should().Be("old");
        totalCount.Should().Be(1);
    }
}
