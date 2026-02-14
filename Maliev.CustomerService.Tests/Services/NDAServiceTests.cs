using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using MassTransit;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Unit tests for NDAService using real PostgreSQL database
/// Tests business logic, state machine transitions, validation, and audit logging
/// </summary>
[Collection("Database Collection")]
public class NDAServiceTests
{
    private readonly TestWebApplicationFactory _fixture;
    private readonly Mock<ILogger<NDAService>> _mockLogger;
    private readonly Mock<MetricsService> _mockMetricsService;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;

    public NDAServiceTests(TestWebApplicationFactory fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<NDAService>>();
        _mockMetricsService = new Mock<MetricsService>(MockBehavior.Loose, new object[] { Mock.Of<IHostEnvironment>() });
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
    }


    private NDAService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new NDAService(context, _mockLogger.Object, _mockMetricsService.Object, _mockPublishEndpoint.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsNDAInDraftStatus()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = await service.CreateAsync(request, "test-actor", "Employee", "Test Actor");

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.CustomerId, result.CustomerId);
        Assert.Equal(request.DocumentReferenceId, result.DocumentReferenceId);
        Assert.Equal(NDAStatus.Draft, result.Status);
        Assert.Equal(request.ExpiresAt, result.ExpiresAt);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5) && result.CreatedAt <= DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_WithoutDocumentReference_CreatesDraftNDA()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = null,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = await service.CreateAsync(request, "test-actor", "Employee", "Test Actor");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(NDAStatus.Draft, result.Status);
        Assert.Null(result.DocumentReferenceId);
    }

    [Fact]
    public async Task CreateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = await service.CreateAsync(request, "employee-123", "Employee", "Test Actor");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLog = await context.AuditLogs
            .Where(a => a.EntityId == result.Id.ToString())
            .FirstOrDefaultAsync();

        Assert.NotNull(auditLog);
        Assert.Equal("employee-123", auditLog!.ActorId);
        Assert.Equal("Employee", auditLog.ActorType);
        Assert.Equal(AuditAction.Create, auditLog.Action);
        Assert.Equal("NDARecord", auditLog.EntityType);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingNDA_ReturnsNDAResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        }, "test-actor", "Employee", "Test Actor");

        // Act
        var result = await service.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
        Assert.Equal(created.CustomerId, result.CustomerId);
        Assert.Equal(NDAStatus.Draft, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentNDA_ReturnsNull()
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
    public async Task UpdateStatusAsync_DraftToSigned_WithDocumentReference_Succeeds()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        }, "test-actor", "Employee", "Test Actor");

        var updateRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow,
            Version = created.Version
        };

        // Act
        var result = await service.UpdateStatusAsync(created.Id, updateRequest, "test-actor", "Employee", "Test Actor");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(NDAStatus.Signed, result.Status);
        Assert.Equal("customer-user", result.SignedBy);
        Assert.True(result.SignedAt > DateTime.UtcNow.AddSeconds(-5) && result.SignedAt <= DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task UpdateStatusAsync_DraftToSigned_WithoutDocumentReference_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = null,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        }, "test-actor", "Employee", "Test Actor");

        var updateRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow,
            Version = created.Version
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateStatusAsync(created.Id, updateRequest, "test-actor", "Employee", "Test Actor"));

        Assert.Contains("without a document reference", exception.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_SignedToRevoked_Succeeds()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        }, "test-actor", "Employee", "Test Actor");

        // First, sign the NDA
        var signRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow,
            Version = created.Version
        };
        var signed = await service.UpdateStatusAsync(created.Id, signRequest, "test-actor", "Employee", "Test Actor");

        // Now revoke it
        var revokeRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Revoked,
            RevokedAt = DateTime.UtcNow,
            Version = signed.Version
        };

        // Act
        var result = await service.UpdateStatusAsync(created.Id, revokeRequest, "admin-user", "Admin", "Test Actor");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(NDAStatus.Revoked, result.Status);
        Assert.True(result.RevokedAt > DateTime.UtcNow.AddSeconds(-5) && result.RevokedAt <= DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task UpdateStatusAsync_DraftToExpired_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        }, "test-actor", "Employee", "Test Actor");

        var updateRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Expired,
            Version = created.Version
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateStatusAsync(created.Id, updateRequest, "test-actor", "Employee", "Test Actor"));

        Assert.Contains("Cannot transition from 'Draft' to 'Expired'", exception.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_DraftToRevoked_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        }, "test-actor", "Employee", "Test Actor");

        var updateRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Revoked,
            RevokedAt = DateTime.UtcNow,
            Version = created.Version
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateStatusAsync(created.Id, updateRequest, "test-actor", "Employee", "Test Actor"));

        Assert.Contains("Cannot transition from 'Draft' to 'Revoked'", exception.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromTerminalState_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        }, "test-actor", "Employee", "Test Actor");

        // Sign and then revoke
        var signRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow,
            Version = created.Version
        };
        var signed = await service.UpdateStatusAsync(created.Id, signRequest, "test-actor", "Employee", "Test Actor");

        var revokeRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Revoked,
            RevokedAt = DateTime.UtcNow,
            Version = signed.Version
        };
        var revoked = await service.UpdateStatusAsync(created.Id, revokeRequest, "admin-user", "Admin", "Test Actor");

        // Try to transition from Revoked to Signed
        var invalidRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow,
            Version = revoked.Version
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateStatusAsync(created.Id, invalidRequest, "test-actor", "Employee", "Test Actor"));

        Assert.Contains("terminal state", exception.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNonExistentNDA_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow,
            Version = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.UpdateStatusAsync(nonExistentId, updateRequest, "test-actor", "Employee", "Test Actor"));
    }

    [Fact]
    public async Task UpdateStatusAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        }, "test-actor", "Employee", "Test Actor");

        var updateRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow,
            Version = created.Version
        };

        // Act
        await service.UpdateStatusAsync(created.Id, updateRequest, "manager-456", "Manager", "Test Actor");

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
    public async Task CheckExpiredNDAsAsync_WithExpiredNDAs_TransitionsToExpired()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Create a signed NDA that is expired
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Already expired
        }, "test-actor", "Employee", "Test Actor");

        // Sign the NDA
        var signRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow.AddDays(-2),
            Version = created.Version
        };
        await service.UpdateStatusAsync(created.Id, signRequest, "test-actor", "Employee", "Test Actor");

        // Act
        var expiredCount = await service.CheckExpiredNDAsAsync();

        // Assert
        Assert.Equal(1, expiredCount);

        var updated = await service.GetByIdAsync(created.Id);
        Assert.NotNull(updated);
        Assert.Equal(NDAStatus.Expired, updated!.Status);
    }

    [Fact]
    public async Task CheckExpiredNDAsAsync_WithNoExpiredNDAs_ReturnsZero()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Create a signed NDA that is not expired
        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1) // Future expiration
        }, "test-actor", "Employee", "Test Actor");

        var signRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow,
            Version = created.Version
        };
        await service.UpdateStatusAsync(created.Id, signRequest, "test-actor", "Employee", "Test Actor");

        // Act
        var expiredCount = await service.CheckExpiredNDAsAsync();

        // Assert
        Assert.Equal(0, expiredCount);
    }

    [Fact]
    public async Task CheckExpiredNDAsAsync_OnlyExpiresSignedNDAs()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Create a draft NDA that is expired (should not be transitioned)
        await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Already expired
        }, "test-actor", "Employee", "Test Actor");

        // Act
        var expiredCount = await service.CheckExpiredNDAsAsync();

        // Assert
        Assert.Equal(0, expiredCount); // Should not expire Draft NDAs
    }

    [Fact]
    public async Task CheckExpiredNDAsAsync_CreatesAuditLogForEachExpiredNDA()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        var created = await service.CreateAsync(new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        }, "test-actor", "Employee", "Test Actor");

        var signRequest = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "customer-user",
            SignedAt = DateTime.UtcNow.AddDays(-2),
            Version = created.Version
        };
        await service.UpdateStatusAsync(created.Id, signRequest, "test-actor", "Employee", "Test Actor");

        // Act
        await service.CheckExpiredNDAsAsync();

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        Assert.Equal(3, auditLogs.Count); // Create + Sign + AutoExpire
        var expireAudit = auditLogs[2];
        Assert.Equal("System", expireAudit.ActorId);
        Assert.Equal("System", expireAudit.ActorType);
        Assert.Equal("AutoExpire", expireAudit.Action);
    }
}
