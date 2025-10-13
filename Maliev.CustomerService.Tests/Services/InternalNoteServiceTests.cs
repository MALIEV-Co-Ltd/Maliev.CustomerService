using FluentAssertions;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Unit tests for InternalNoteService using real PostgreSQL database
/// Tests business logic, validation, audit logging, and polymorphic relationships
/// </summary>
[Collection("Database Collection")]
public class InternalNoteServiceTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly Mock<ILogger<InternalNoteService>> _mockLogger;

    public InternalNoteServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<InternalNoteService>>();
    }

    private InternalNoteService CreateService()
    {
        var context = _fixture.CreateDbContext();
        return new InternalNoteService(context, _mockLogger.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDataForCustomerOwner_ReturnsInternalNoteResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var customerId = Guid.NewGuid();
        var request = new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "Customer has requested priority shipping for all orders"
        };

        // Act
        var result = await service.CreateAsync(request, "employee-123");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.OwnerType.Should().Be("Customer");
        result.OwnerId.Should().Be(customerId);
        result.NoteText.Should().Be("Customer has requested priority shipping for all orders");
        result.CreatedBy.Should().Be("employee-123");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Version.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidDataForCompanyOwner_ReturnsInternalNoteResponse()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var companyId = Guid.NewGuid();
        var request = new CreateInternalNoteRequest
        {
            OwnerType = "Company",
            OwnerId = companyId,
            NoteText = "Company requires 30-day payment terms"
        };

        // Act
        var result = await service.CreateAsync(request, "employee-456");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.OwnerType.Should().Be("Company");
        result.OwnerId.Should().Be(companyId);
        result.NoteText.Should().Be("Company requires 30-day payment terms");
        result.CreatedBy.Should().Be("employee-456");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Version.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var customerId = Guid.NewGuid();
        var request = new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "Audit test note"
        };

        // Act
        var result = await service.CreateAsync(request, "employee-789");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLog = await context.AuditLogs
            .Where(a => a.EntityId == result.Id.ToString())
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.ActorId.Should().Be("employee-789");
        auditLog.ActorType.Should().Be("Employee");
        auditLog.Action.Should().Be(AuditAction.Create);
        auditLog.EntityType.Should().Be(nameof(InternalNote));
        auditLog.ChangedFields.Should().Contain("Customer");
        auditLog.ChangedFields.Should().Contain("Audit test note");
    }

    #endregion

    #region GetByOwnerAsync Tests

    [Fact]
    public async Task GetByOwnerAsync_WithCustomerOwner_ReturnsCustomerNotes()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var customerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        // Create notes for customer
        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "Customer note 1"
        }, "employee-1");

        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "Customer note 2"
        }, "employee-2");

        // Create note for company (should not be returned)
        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Company",
            OwnerId = companyId,
            NoteText = "Company note"
        }, "employee-3");

        // Act
        var result = await service.GetByOwnerAsync("Customer", customerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(note =>
        {
            note.OwnerType.Should().Be("Customer");
            note.OwnerId.Should().Be(customerId);
        });
        result.Select(n => n.NoteText).Should().Contain(new[] { "Customer note 1", "Customer note 2" });
    }

    [Fact]
    public async Task GetByOwnerAsync_WithCompanyOwner_ReturnsCompanyNotes()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        // Create notes for company
        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Company",
            OwnerId = companyId,
            NoteText = "Company note 1"
        }, "employee-1");

        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Company",
            OwnerId = companyId,
            NoteText = "Company note 2"
        }, "employee-2");

        // Create note for customer (should not be returned)
        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "Customer note"
        }, "employee-3");

        // Act
        var result = await service.GetByOwnerAsync("Company", companyId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(note =>
        {
            note.OwnerType.Should().Be("Company");
            note.OwnerId.Should().Be(companyId);
        });
        result.Select(n => n.NoteText).Should().Contain(new[] { "Company note 1", "Company note 2" });
    }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsNotesInDescendingOrder()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var customerId = Guid.NewGuid();

        // Create notes with slight delays to ensure different timestamps
        var note1 = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "First note"
        }, "employee-1");

        await Task.Delay(100); // Small delay

        var note2 = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "Second note"
        }, "employee-2");

        await Task.Delay(100); // Small delay

        var note3 = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId,
            NoteText = "Third note"
        }, "employee-3");

        // Act
        var result = await service.GetByOwnerAsync("Customer", customerId);

        // Assert
        result.Should().HaveCount(3);
        result[0].NoteText.Should().Be("Third note");
        result[1].NoteText.Should().Be("Second note");
        result[2].NoteText.Should().Be("First note");
    }

    [Fact]
    public async Task GetByOwnerAsync_WithNoNotes_ReturnsEmptyList()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var customerId = Guid.NewGuid();

        // Act
        var result = await service.GetByOwnerAsync("Customer", customerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedNote()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = "Original note text"
        }, "employee-1");

        var updateRequest = new UpdateInternalNoteRequest
        {
            NoteText = "Updated note text",
            Version = created.Version
        };

        // Act
        var result = await service.UpdateAsync(created.Id, updateRequest, "employee-2");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(created.Id);
        result.NoteText.Should().Be("Updated note text");
        result.OwnerType.Should().Be(created.OwnerType);
        result.OwnerId.Should().Be(created.OwnerId);
        result.CreatedBy.Should().Be(created.CreatedBy);
        result.UpdatedAt.Should().BeAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentNote_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateInternalNoteRequest
        {
            NoteText = "Updated text",
            Version = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.UpdateAsync(nonExistentId, updateRequest, "employee-1"));
    }

    [Fact]
    public async Task UpdateAsync_WithWrongVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = "Original note"
        }, "employee-1");

        var updateRequest = new UpdateInternalNoteRequest
        {
            NoteText = "Updated note",
            Version = new byte[] { 0, 0, 0, 0, 0, 0, 0, 99 } // Wrong version
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateAsync(created.Id, updateRequest, "employee-1"));

        exception.Message.Should().Contain("modified by another user");
    }

    [Fact]
    public async Task UpdateAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = "Original note"
        }, "employee-1");

        var updateRequest = new UpdateInternalNoteRequest
        {
            NoteText = "Updated note for audit",
            Version = created.Version
        };

        // Act
        await service.UpdateAsync(created.Id, updateRequest, "employee-2");

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        auditLogs.Should().HaveCount(2); // Create + Update
        var updateAudit = auditLogs[1];
        updateAudit.ActorId.Should().Be("employee-2");
        updateAudit.ActorType.Should().Be("Employee");
        updateAudit.Action.Should().Be(AuditAction.Update);
        updateAudit.EntityType.Should().Be(nameof(InternalNote));
        updateAudit.ChangedFields.Should().Contain("Updated note for audit");
        updateAudit.PreviousValues.Should().Contain("Original note");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingNote_DeletesSuccessfully()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = "Note to delete"
        }, "employee-1");

        // Act
        await service.DeleteAsync(created.Id);

        // Assert
        await using var context = _fixture.CreateDbContext();
        var deletedNote = await context.InternalNotes.FindAsync(created.Id);
        deletedNote.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentNote_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await service.DeleteAsync(nonExistentId));
    }

    [Fact]
    public async Task DeleteAsync_CreatesAuditLog()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var created = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Company",
            OwnerId = Guid.NewGuid(),
            NoteText = "Note for deletion audit"
        }, "employee-1");

        // Act
        await service.DeleteAsync(created.Id);

        // Assert
        await using var context = _fixture.CreateDbContext();
        var auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == created.Id.ToString())
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        auditLogs.Should().HaveCount(2); // Create + Delete
        var deleteAudit = auditLogs[1];
        deleteAudit.ActorId.Should().Be("System");
        deleteAudit.ActorType.Should().Be("System");
        deleteAudit.Action.Should().Be(AuditAction.Delete);
        deleteAudit.EntityType.Should().Be(nameof(InternalNote));
        deleteAudit.PreviousValues.Should().Contain("Note for deletion audit");
        deleteAudit.PreviousValues.Should().Contain("employee-1");
    }

    #endregion

    #region Polymorphic Relationship Tests

    [Fact]
    public async Task PolymorphicRelationship_DifferentOwnersWithSameId_AreIsolated()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var sharedId = Guid.NewGuid(); // Same ID for both customer and company

        // Create note for customer
        var customerNote = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = sharedId,
            NoteText = "Customer-specific note"
        }, "employee-1");

        // Create note for company with same ID
        var companyNote = await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Company",
            OwnerId = sharedId,
            NoteText = "Company-specific note"
        }, "employee-2");

        // Act
        var customerNotes = await service.GetByOwnerAsync("Customer", sharedId);
        var companyNotes = await service.GetByOwnerAsync("Company", sharedId);

        // Assert
        customerNotes.Should().HaveCount(1);
        customerNotes[0].NoteText.Should().Be("Customer-specific note");

        companyNotes.Should().HaveCount(1);
        companyNotes[0].NoteText.Should().Be("Company-specific note");
    }

    [Fact]
    public async Task PolymorphicRelationship_MultipleNotesAcrossOwnerTypes_AreCorrectlyFiltered()
    {
        // Arrange
        await _fixture.ClearDatabaseAsync();
        var service = CreateService();
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        var companyId1 = Guid.NewGuid();
        var companyId2 = Guid.NewGuid();

        // Create notes for different owners
        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId1,
            NoteText = "Customer 1 note"
        }, "employee-1");

        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = customerId2,
            NoteText = "Customer 2 note"
        }, "employee-2");

        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Company",
            OwnerId = companyId1,
            NoteText = "Company 1 note"
        }, "employee-3");

        await service.CreateAsync(new CreateInternalNoteRequest
        {
            OwnerType = "Company",
            OwnerId = companyId2,
            NoteText = "Company 2 note"
        }, "employee-4");

        // Act
        var customer1Notes = await service.GetByOwnerAsync("Customer", customerId1);
        var customer2Notes = await service.GetByOwnerAsync("Customer", customerId2);
        var company1Notes = await service.GetByOwnerAsync("Company", companyId1);
        var company2Notes = await service.GetByOwnerAsync("Company", companyId2);

        // Assert
        customer1Notes.Should().HaveCount(1);
        customer1Notes[0].NoteText.Should().Be("Customer 1 note");

        customer2Notes.Should().HaveCount(1);
        customer2Notes[0].NoteText.Should().Be("Customer 2 note");

        company1Notes.Should().HaveCount(1);
        company1Notes[0].NoteText.Should().Be("Company 1 note");

        company2Notes.Should().HaveCount(1);
        company2Notes[0].NoteText.Should().Be("Company 2 note");
    }

    #endregion
}
