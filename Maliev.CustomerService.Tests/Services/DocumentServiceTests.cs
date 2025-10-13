using FluentAssertions;
using Maliev.CustomerService.Api.Models.Documents;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Api.Services.External;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Unit tests for DocumentService using real PostgreSQL database
/// Tests business logic, external service integration, validation, and audit logging
/// </summary>
[Collection("Database Collection")]
public class DocumentServiceTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<IUploadServiceClient> _mockUploadServiceClient;
    private readonly Mock<ILogger<DocumentService>> _mockLogger;
    private readonly Mock<MetricsService> _mockMetricsService;

    public DocumentServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _mockUploadServiceClient = new Mock<IUploadServiceClient>();
        _mockLogger = new Mock<ILogger<DocumentService>>();
        _mockMetricsService = new Mock<MetricsService>(MockBehavior.Loose, new object[] { Mock.Of<IConfiguration>() });
    }

    private DocumentService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new DocumentService(context, _mockUploadServiceClient.Object, _mockLogger.Object, _mockMetricsService.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsDocumentInPendingStatus()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync("file-ref-123"))
            .ReturnsAsync(true);

        var request = new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-123",
            Filename = "nda-document.pdf"
        };

        // Act
        var result = await service.CreateAsync(request, "test-actor", "Employee");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.OwnerType.Should().Be("Customer");
        result.OwnerId.Should().Be(request.OwnerId);
        result.DocumentType.Should().Be("NDA");
        result.FileReference.Should().Be("file-ref-123");
        result.Filename.Should().Be("nda-document.pdf");
        result.Status.Should().Be(DocumentStatus.Pending);
        result.Version.Should().Be(1);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockUploadServiceClient.Verify(x => x.ValidateFileReferenceAsync("file-ref-123"), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidFileReference_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync("invalid-ref"))
            .ReturnsAsync(false);

        var request = new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "invalid-ref",
            Filename = "document.pdf"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.CreateAsync(request, "test-actor", "Employee"));

        exception.Message.Should().Contain("not valid in Upload Service");
        _mockUploadServiceClient.Verify(x => x.ValidateFileReferenceAsync("invalid-ref"), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new CreateDocumentRequest
        {
            OwnerType = "Company",
            OwnerId = Guid.NewGuid(),
            DocumentType = "Contract",
            FileReference = "file-ref-456",
            Filename = "contract.pdf"
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
        auditLog.EntityType.Should().Be("DocumentReference");
    }

    [Fact]
    public async Task GetByOwnerAsync_WithMatchingDocuments_ReturnsDocuments()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var ownerId = Guid.NewGuid();

        // Create two documents for the same owner
        await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = ownerId,
            DocumentType = "NDA",
            FileReference = "file-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = ownerId,
            DocumentType = "Contract",
            FileReference = "file-2",
            Filename = "contract.pdf"
        }, "test-actor", "Employee");

        // Create a document for a different owner
        await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-3",
            Filename = "other.pdf"
        }, "test-actor", "Employee");

        // Act
        var result = await service.GetByOwnerAsync("Customer", ownerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(d => d.OwnerId == ownerId).Should().BeTrue();
        result.All(d => d.OwnerType == "Customer").Should().BeTrue();
    }

    [Fact]
    public async Task GetByOwnerAsync_WithNoMatchingDocuments_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentOwnerId = Guid.NewGuid();

        // Act
        var result = await service.GetByOwnerAsync("Customer", nonExistentOwnerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_IncrementsVersionAndUpdatesFile()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda-v1.pdf"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateDocumentRequest
        {
            FileReference = "file-ref-2",
            Filename = "nda-v2.pdf",
            RowVersion = created.RowVersion
        };

        // Act
        var result = await service.UpdateAsync(created.Id, updateRequest, "test-actor", "Employee");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(created.Id);
        result.FileReference.Should().Be("file-ref-2");
        result.Filename.Should().Be("nda-v2.pdf");
        result.Version.Should().Be(2);
        result.UpdatedAt.Should().BeAfter(created.UpdatedAt);

        _mockUploadServiceClient.Verify(x => x.ValidateFileReferenceAsync("file-ref-2"), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidFileReference_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync("file-ref-1"))
            .ReturnsAsync(true);
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync("invalid-ref"))
            .ReturnsAsync(false);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateDocumentRequest
        {
            FileReference = "invalid-ref",
            Filename = "updated.pdf",
            RowVersion = created.RowVersion
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateAsync(created.Id, updateRequest, "test-actor", "Employee"));

        exception.Message.Should().Contain("not valid in Upload Service");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentDocument_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateDocumentRequest
        {
            FileReference = "file-ref",
            Filename = "updated.pdf",
            RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.UpdateAsync(nonExistentId, updateRequest, "test-actor", "Employee"));
    }

    [Fact]
    public async Task UpdateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda-v1.pdf"
        }, "test-actor", "Employee");

        var updateRequest = new UpdateDocumentRequest
        {
            FileReference = "file-ref-2",
            Filename = "nda-v2.pdf",
            RowVersion = created.RowVersion
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
    public async Task MarkCompleteAsync_WithValidDocument_TransitionsToComplete()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        // Act
        var result = await service.MarkCompleteAsync(created.Id, "customer-user", DateTime.UtcNow, "test-actor", "Employee");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(DocumentStatus.Complete);
        result.SignedBy.Should().Be("customer-user");
        result.SignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task MarkCompleteAsync_WithNonExistentDocument_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.MarkCompleteAsync(nonExistentId, "user", DateTime.UtcNow, "test-actor", "Employee"));
    }

    [Fact]
    public async Task MarkCompleteAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        // Act
        await service.MarkCompleteAsync(created.Id, "customer-user", DateTime.UtcNow, "admin-789", "Admin");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        auditLogs.Should().HaveCount(2); // Create + Update
        var updateAudit = auditLogs[1];
        updateAudit.ActorId.Should().Be("admin-789");
        updateAudit.ActorType.Should().Be("Admin");
        updateAudit.Action.Should().Be(AuditAction.Update);
    }

    [Fact]
    public async Task DeleteAsync_WithSuccessfulUploadServiceDeletion_RemovesDocument()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync("file-ref-1"))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        // Act
        await service.DeleteAsync(created.Id, "admin-user", "Admin");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var document = await context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == created.Id);
        document.Should().BeNull(); // Should be deleted

        _mockUploadServiceClient.Verify(x => x.DeleteFileAsync("file-ref-1"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithFailedUploadServiceDeletion_MarksPendingDeletion()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync("file-ref-1"))
            .ReturnsAsync(false);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        // Act
        await service.DeleteAsync(created.Id, "admin-user", "Admin");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var document = await context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == created.Id);
        document.Should().NotBeNull();
        document!.Status.Should().Be(DocumentStatus.PendingDeletion);

        _mockUploadServiceClient.Verify(x => x.DeleteFileAsync("file-ref-1"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentDocument_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.DeleteAsync(nonExistentId, "test-actor", "Employee"));
    }

    [Fact]
    public async Task DeleteAsync_CreatesAuditLogForSuccessfulDeletion()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        // Act
        await service.DeleteAsync(created.Id, "admin-999", "Admin");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        auditLogs.Should().HaveCount(2); // Create + Delete
        var deleteAudit = auditLogs[1];
        deleteAudit.ActorId.Should().Be("admin-999");
        deleteAudit.ActorType.Should().Be("Admin");
        deleteAudit.Action.Should().Be(AuditAction.Delete);
    }

    [Fact]
    public async Task DeleteAsync_CreatesAuditLogForPendingDeletion()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        // Act
        await service.DeleteAsync(created.Id, "admin-999", "Admin");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        auditLogs.Should().HaveCount(2); // Create + MarkPendingDeletion
        var markPendingAudit = auditLogs[1];
        markPendingAudit.ActorId.Should().Be("admin-999");
        markPendingAudit.ActorType.Should().Be("Admin");
        markPendingAudit.Action.Should().Be("MarkPendingDeletion");
    }

    [Fact]
    public async Task RetryPendingDeletionsAsync_WithSuccessfulRetry_RemovesDocuments()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // First attempt fails
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync("file-ref-1"))
            .ReturnsAsync(false);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        await service.DeleteAsync(created.Id, "admin-user", "Admin");

        // Verify it's pending deletion
        await using var context1 = _fixture.CreateDbContext();
        var pendingDoc = await context1.DocumentReferences.FirstOrDefaultAsync(d => d.Id == created.Id);
        pendingDoc!.Status.Should().Be(DocumentStatus.PendingDeletion);

        // Now setup the retry to succeed
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync("file-ref-1"))
            .ReturnsAsync(true);

        // Act
        var retryCount = await service.RetryPendingDeletionsAsync();

        // Assert
        retryCount.Should().Be(1);

        await using var context2 = _fixture.CreateDbContext();
        var document = await context2.DocumentReferences.FirstOrDefaultAsync(d => d.Id == created.Id);
        document.Should().BeNull(); // Should be deleted

        _mockUploadServiceClient.Verify(x => x.DeleteFileAsync("file-ref-1"), Times.Exactly(2)); // Original + Retry
    }

    [Fact]
    public async Task RetryPendingDeletionsAsync_WithFailedRetry_KeepsDocumentPending()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync("file-ref-1"))
            .ReturnsAsync(false);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        await service.DeleteAsync(created.Id, "admin-user", "Admin");

        // Act
        var retryCount = await service.RetryPendingDeletionsAsync();

        // Assert
        retryCount.Should().Be(0); // No successful retries

        await using var context = _fixture.CreateDbContext();
        var document = await context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == created.Id);
        document.Should().NotBeNull();
        document!.Status.Should().Be(DocumentStatus.PendingDeletion);

        _mockUploadServiceClient.Verify(x => x.DeleteFileAsync("file-ref-1"), Times.Exactly(2)); // Original + Retry
    }

    [Fact]
    public async Task RetryPendingDeletionsAsync_WithNoPendingDeletions_ReturnsZero()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();

        // Act
        var retryCount = await service.RetryPendingDeletionsAsync();

        // Assert
        retryCount.Should().Be(0);
    }

    [Fact]
    public async Task RetryPendingDeletionsAsync_CreatesAuditLogForSuccessfulRetry()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // First attempt fails
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync("file-ref-1"))
            .ReturnsAsync(false);

        var created = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda.pdf"
        }, "test-actor", "Employee");

        await service.DeleteAsync(created.Id, "admin-user", "Admin");

        // Setup retry to succeed
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync("file-ref-1"))
            .ReturnsAsync(true);

        // Act
        await service.RetryPendingDeletionsAsync();

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        auditLogs.Should().HaveCount(3); // Create + MarkPendingDeletion + RetryDeletion
        var retryAudit = auditLogs[2];
        retryAudit.ActorId.Should().Be("System");
        retryAudit.ActorType.Should().Be("System");
        retryAudit.Action.Should().Be("RetryDeletion");
    }

    [Fact]
    public async Task RetryPendingDeletionsAsync_HandlesMultiplePendingDeletions()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        _mockUploadServiceClient.Setup(x => x.ValidateFileReferenceAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Create two documents that will fail deletion
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var doc1 = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-ref-1",
            Filename = "nda1.pdf"
        }, "test-actor", "Employee");

        var doc2 = await service.CreateAsync(new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "Contract",
            FileReference = "file-ref-2",
            Filename = "contract.pdf"
        }, "test-actor", "Employee");

        await service.DeleteAsync(doc1.Id, "admin-user", "Admin");
        await service.DeleteAsync(doc2.Id, "admin-user", "Admin");

        // Setup retry - only first succeeds
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync("file-ref-1"))
            .ReturnsAsync(true);
        _mockUploadServiceClient.Setup(x => x.DeleteFileAsync("file-ref-2"))
            .ReturnsAsync(false);

        // Act
        var retryCount = await service.RetryPendingDeletionsAsync();

        // Assert
        retryCount.Should().Be(1); // Only one succeeded

        await using var context = _fixture.CreateDbContext();
        var doc1AfterRetry = await context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == doc1.Id);
        var doc2AfterRetry = await context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == doc2.Id);

        doc1AfterRetry.Should().BeNull(); // Successfully deleted
        doc2AfterRetry.Should().NotBeNull(); // Still pending
        doc2AfterRetry!.Status.Should().Be(DocumentStatus.PendingDeletion);
    }
}
