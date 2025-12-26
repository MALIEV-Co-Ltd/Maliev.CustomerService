using System.Text.Json;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for internal note management operations
/// </summary>
public class InternalNoteService : IInternalNoteService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<InternalNoteService> _logger;

    /// <summary>
    /// Initializes a new instance of the InternalNoteService class
    /// </summary>
    /// <param name="context">Database context for Customer Service</param>
    /// <param name="logger">Logger instance</param>
    public InternalNoteService(CustomerDbContext context, ILogger<InternalNoteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new internal note with audit logging
    /// </summary>
    /// <param name="request">Internal note creation request</param>
    /// <param name="createdBy">ID of the employee creating the note</param>
    /// <returns>Created internal note response</returns>
    public async Task<InternalNoteResponse> CreateAsync(CreateInternalNoteRequest request, string createdBy)
    {
        _logger.LogInformation("Creating internal note for owner {OwnerType}/{OwnerId} by {CreatedBy}",
            request.OwnerType, request.OwnerId, createdBy);

        var note = new InternalNote
        {
            Id = Guid.NewGuid(),
            OwnerType = request.OwnerType,
            OwnerId = request.OwnerId,
            NoteText = request.NoteText,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InternalNotes.Add(note);

        var auditLog = new AuditLog
        {
            ActorId = createdBy,
            ActorType = "Employee",
            Action = AuditAction.Create,
            EntityType = nameof(InternalNote),
            EntityId = note.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                note.OwnerType,
                note.OwnerId,
                note.NoteText
            })
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Internal note {NoteId} created successfully", note.Id);
        return note.ToInternalNoteResponse();
    }

    /// <summary>
    /// Retrieves all internal notes for a specific owner
    /// </summary>
    /// <param name="ownerType">Type of owner (Customer or Company)</param>
    /// <param name="ownerId">Owner ID</param>
    /// <returns>List of internal notes ordered by creation date descending</returns>
    public async Task<List<InternalNoteResponse>> GetByOwnerAsync(string ownerType, Guid ownerId)
    {
        _logger.LogDebug("Retrieving internal notes for owner {OwnerType}/{OwnerId}", ownerType, ownerId);

        var notes = await _context.InternalNotes
            .Where(n => n.OwnerType == ownerType && n.OwnerId == ownerId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notes.Select(n => n.ToInternalNoteResponse()).ToList();
    }

    /// <summary>
    /// Updates an existing internal note with optimistic concurrency control
    /// </summary>
    /// <param name="id">Internal note ID</param>
    /// <param name="request">Update request containing new note text and version</param>
    /// <param name="actorId">ID of the employee updating the note</param>
    /// <returns>Updated internal note response</returns>
    /// <exception cref="KeyNotFoundException">Thrown when internal note is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when version conflict occurs</exception>
    public async Task<InternalNoteResponse> UpdateAsync(Guid id, UpdateInternalNoteRequest request, string actorId)
    {
        _logger.LogInformation("Updating internal note {NoteId} by {ActorId}", id, actorId);

        var note = await _context.InternalNotes.FirstOrDefaultAsync(n => n.Id == id);
        if (note == null)
        {
            _logger.LogWarning("Internal note {NoteId} not found for update", id);
            throw new KeyNotFoundException($"Internal note with ID '{id}' not found");
        }

        var previousValues = new
        {
            note.NoteText
        };

        note.NoteText = request.NoteText;
        note.UpdatedAt = DateTime.UtcNow;

        _context.Entry(note).Property(n => n.Version).OriginalValue = request.Version;

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = "Employee",
            Action = AuditAction.Update,
            EntityType = nameof(InternalNote),
            EntityId = note.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new { note.NoteText }),
            PreviousValues = JsonSerializer.Serialize(previousValues)
        };

        _context.AuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Internal note {NoteId} updated successfully", id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating internal note {NoteId}", id);
            throw new InvalidOperationException("The internal note was modified by another user. Please refresh and try again.");
        }

        return note.ToInternalNoteResponse();
    }

    /// <summary>
    /// Deletes an internal note with audit logging
    /// </summary>
    /// <param name="id">Internal note ID</param>
    /// <exception cref="KeyNotFoundException">Thrown when internal note is not found</exception>
    public async Task DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting internal note {NoteId}", id);

        var note = await _context.InternalNotes.FirstOrDefaultAsync(n => n.Id == id);
        if (note == null)
        {
            _logger.LogWarning("Internal note {NoteId} not found for deletion", id);
            throw new KeyNotFoundException($"Internal note with ID '{id}' not found");
        }

        _context.InternalNotes.Remove(note);

        var auditLog = new AuditLog
        {
            ActorId = "System",
            ActorType = "System",
            Action = AuditAction.Delete,
            EntityType = nameof(InternalNote),
            EntityId = note.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            PreviousValues = JsonSerializer.Serialize(new
            {
                note.OwnerType,
                note.OwnerId,
                note.NoteText,
                note.CreatedBy
            })
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Internal note {NoteId} deleted successfully", id);
    }
}
