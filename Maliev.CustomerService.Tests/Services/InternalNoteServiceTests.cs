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
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Customer", result.OwnerType);
        Assert.Equal(customerId, result.OwnerId);
        Assert.Equal("Customer has requested priority shipping for all orders", result.NoteText);
        Assert.Equal("employee-123", result.CreatedBy);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5) && result.CreatedAt <= DateTime.UtcNow.AddSeconds(5));
        Assert.True(result.UpdatedAt > DateTime.UtcNow.AddSeconds(-5) && result.UpdatedAt <= DateTime.UtcNow.AddSeconds(5));
        Assert.NotEmpty(result.Version);
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
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Company", result.OwnerType);
        Assert.Equal(companyId, result.OwnerId);
        Assert.Equal("Company requires 30-day payment terms", result.NoteText);
        Assert.Equal("employee-456", result.CreatedBy);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddSeconds(-5) && result.CreatedAt <= DateTime.UtcNow.AddSeconds(5));
        Assert.True(result.UpdatedAt > DateTime.UtcNow.AddSeconds(-5) && result.UpdatedAt <= DateTime.UtcNow.AddSeconds(5));
        Assert.NotEmpty(result.Version);
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

        Assert.NotNull(auditLog);
        Assert.Equal("employee-789", auditLog!.ActorId);
        Assert.Equal("Employee", auditLog.ActorType);
        Assert.Equal(AuditAction.Create, auditLog.Action);
        Assert.Equal(nameof(InternalNote), auditLog.EntityType);
        Assert.Contains("Customer", auditLog.ChangedFields);
        Assert.Contains("Audit test note", auditLog.ChangedFields);
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
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, note =>
        {
            Assert.Equal("Customer", note.OwnerType);
            Assert.Equal(customerId, note.OwnerId);
        });
        Assert.Contains(result, n => n.NoteText == "Customer note 1");
        Assert.Contains(result, n => n.NoteText == "Customer note 2");
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
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, note =>
        {
            Assert.Equal("Company", note.OwnerType);
            Assert.Equal(companyId, note.OwnerId);
        });
        Assert.Contains(result, n => n.NoteText == "Company note 1");
        Assert.Contains(result, n => n.NoteText == "Company note 2");
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
        Assert.Equal(3, result.Count);
        Assert.Equal("Third note", result[0].NoteText);
        Assert.Equal("Second note", result[1].NoteText);
        Assert.Equal("First note", result[2].NoteText);
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
        Assert.NotNull(result);
        Assert.Empty(result);
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
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Updated note text", result.NoteText);
        Assert.Equal(created.OwnerType, result.OwnerType);
        Assert.Equal(created.OwnerId, result.OwnerId);
        Assert.Equal(created.CreatedBy, result.CreatedBy);
        Assert.True(result.UpdatedAt > created.UpdatedAt);
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

        Assert.Contains("modified by another user", exception.Message);
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

        Assert.Equal(2, auditLogs.Count); // Create + Update
        var updateAudit = auditLogs[1];
        Assert.Equal("employee-2", updateAudit.ActorId);
        Assert.Equal("Employee", updateAudit.ActorType);
        Assert.Equal(AuditAction.Update, updateAudit.Action);
        Assert.Equal(nameof(InternalNote), updateAudit.EntityType);
        Assert.Contains("Updated note for audit", updateAudit.ChangedFields);
        Assert.Contains("Original note", updateAudit.PreviousValues);
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
        Assert.Null(deletedNote);
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

        Assert.Equal(2, auditLogs.Count); // Create + Delete
        var deleteAudit = auditLogs[1];
        Assert.Equal("System", deleteAudit.ActorId);
        Assert.Equal("System", deleteAudit.ActorType);
        Assert.Equal(AuditAction.Delete, deleteAudit.Action);
        Assert.Equal(nameof(InternalNote), deleteAudit.EntityType);
        Assert.Contains("Note for deletion audit", deleteAudit.PreviousValues);
        Assert.Contains("employee-1", deleteAudit.PreviousValues);
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
        Assert.Single(customerNotes);
        Assert.Equal("Customer-specific note", customerNotes[0].NoteText);

        Assert.Single(companyNotes);
        Assert.Equal("Company-specific note", companyNotes[0].NoteText);
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
        Assert.Single(customer1Notes);
        Assert.Equal("Customer 1 note", customer1Notes[0].NoteText);

        Assert.Single(customer2Notes);
        Assert.Equal("Customer 2 note", customer2Notes[0].NoteText);

        Assert.Single(company1Notes);
        Assert.Equal("Company 1 note", company1Notes[0].NoteText);

        Assert.Single(company2Notes);
        Assert.Equal("Company 2 note", company2Notes[0].NoteText);
    }

    #endregion
}
