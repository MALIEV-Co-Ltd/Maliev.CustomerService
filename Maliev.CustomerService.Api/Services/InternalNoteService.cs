using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Maliev.CustomerService.Api.Services;

public class InternalNoteService : IInternalNoteService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<InternalNoteService> _logger;

    public InternalNoteService(CustomerDbContext context, ILogger<InternalNoteService> logger)
    {
        _context = context;
        _logger = logger;
    }

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
        return MapToResponse(note);
    }

    public async Task<List<InternalNoteResponse>> GetByOwnerAsync(string ownerType, Guid ownerId)
    {
        _logger.LogDebug("Retrieving internal notes for owner {OwnerType}/{OwnerId}", ownerType, ownerId);

        var notes = await _context.InternalNotes
            .Where(n => n.OwnerType == ownerType && n.OwnerId == ownerId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notes.Select(MapToResponse).ToList();
    }

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

        return MapToResponse(note);
    }

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

    private InternalNoteResponse MapToResponse(InternalNote note)
    {
        return new InternalNoteResponse
        {
            Id = note.Id,
            OwnerType = note.OwnerType,
            OwnerId = note.OwnerId,
            NoteText = note.NoteText,
            CreatedBy = note.CreatedBy,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt,
            Version = note.Version
        };
    }
}
