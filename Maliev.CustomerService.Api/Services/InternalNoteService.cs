using System.Text.Json;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for internal note management operations
/// </summary>
public class InternalNoteService : IInternalNoteService
{
    private readonly CustomerDbContext _context;
    private readonly IIAMClient _iamClient;
    private readonly ILogger<InternalNoteService> _logger;

    /// <summary>
    /// Initializes a new instance of the InternalNoteService class
    /// </summary>
    public InternalNoteService(CustomerDbContext context, IIAMClient iamClient, ILogger<InternalNoteService> logger)
    {
        _context = context;
        _iamClient = iamClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new internal note with audit logging
    /// </summary>
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

        var response = note.ToInternalNoteResponse();
        if (Guid.TryParse(createdBy, out var principalId))
        {
            var principal = await _iamClient.GetPrincipalByIdAsync(principalId);
            if (principal != null)
            {
                response.CreatedByName = principal.DisplayName;
                response.CreatedByEmail = principal.Email;
            }
        }

        return response;
    }

    /// <summary>
    /// Retrieves all internal notes for a specific owner
    /// </summary>
    public async Task<List<InternalNoteResponse>> GetByOwnerAsync(string ownerType, Guid ownerId)
    {
        _logger.LogDebug("Retrieving internal notes for owner {OwnerType}/{OwnerId}", ownerType, ownerId);

        var notes = await _context.InternalNotes
            .Where(n => n.OwnerId == ownerId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var responses = new List<InternalNoteResponse>();

        // Filter by ownerType in memory
        var filteredNotes = notes
            .Where(n => n.OwnerType.Equals(ownerType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Collect unique principal IDs
        var principalIds = filteredNotes
            .Where(n => Guid.TryParse(n.CreatedBy, out _))
            .Select(n => Guid.Parse(n.CreatedBy))
            .Distinct()
            .ToList();

        // Resolve them (simplistic for now, batching would be better if IAM supported it)
        var principalMap = new Dictionary<Guid, Maliev.CustomerService.Api.Models.IAM.PrincipalResponse>();
        foreach (var pId in principalIds)
        {
            var principal = await _iamClient.GetPrincipalByIdAsync(pId);
            if (principal != null) principalMap[pId] = principal;
        }

        foreach (var note in notes)
        {
            var response = note.ToInternalNoteResponse();
            if (Guid.TryParse(note.CreatedBy, out var pId) && principalMap.TryGetValue(pId, out var p))
            {
                response.CreatedByName = p.DisplayName;
                response.CreatedByEmail = p.Email;
            }
            responses.Add(response);
        }

        return responses;
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

        _context.Entry(note).Property(n => n.xmin).OriginalValue = request.xmin;

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = "Employee",
            Action = AuditAction.Update,
            EntityType = nameof(InternalNote),
            EntityId = note.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new { note.NoteText, note.OwnerId, note.OwnerType }),
            PreviousValues = JsonSerializer.Serialize(new { previousValues.NoteText, note.OwnerId, note.OwnerType })
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
    /// Deletes an internal note with optimistic concurrency control and audit logging
    /// </summary>
    /// <param name="id">Internal note ID</param>
    /// <param name="xmin">PostgreSQL xmin for concurrency control</param>
    /// <exception cref="KeyNotFoundException">Thrown when internal note is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when version conflict occurs</exception>
    public async Task DeleteAsync(Guid id, uint xmin)
    {
        _logger.LogInformation("Deleting internal note {NoteId}", id);

        var note = await _context.InternalNotes.FirstOrDefaultAsync(n => n.Id == id);
        if (note == null)
        {
            _logger.LogWarning("Internal note {NoteId} not found for deletion", id);
            throw new KeyNotFoundException($"Internal note with ID '{id}' not found");
        }

        _context.Entry(note).Property(n => n.xmin).OriginalValue = xmin;

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

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Internal note {NoteId} deleted successfully", id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict deleting internal note {NoteId}", id);
            throw new InvalidOperationException("The internal note was modified by another user. Please refresh and try again.");
        }
    }

    /// <inheritdoc />
    public async Task<InternalNoteCommentResponse> AddCommentAsync(Guid noteId, CreateInternalNoteCommentRequest request, string actorId)
    {
        _logger.LogInformation("Adding comment to internal note {NoteId} by {ActorId}", noteId, actorId);

        var note = await _context.InternalNotes.AnyAsync(n => n.Id == noteId);
        if (!note)
        {
            throw new KeyNotFoundException($"Internal note with ID '{noteId}' not found");
        }

        string? actorName = null;
        if (Guid.TryParse(actorId, out var principalId))
        {
            var principal = await _iamClient.GetPrincipalByIdAsync(principalId);
            actorName = principal?.DisplayName;
        }

        var comment = new InternalNoteComment
        {
            Id = Guid.NewGuid(),
            InternalNoteId = noteId,
            CommentText = request.CommentText,
            CreatedBy = actorId,
            CreatedByName = actorName,
            CreatedAt = DateTime.UtcNow
        };

        _context.InternalNoteComments.Add(comment);

        // Add audit log for the comment
        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = "Employee",
            Action = "Comment",
            EntityType = nameof(InternalNote),
            EntityId = noteId.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new { CommentText = request.CommentText })
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        var response = comment.ToInternalNoteCommentResponse();
        if (Guid.TryParse(actorId, out var pId))
        {
            var p = await _iamClient.GetPrincipalByIdAsync(pId);
            if (p != null) response.CreatedByEmail = p.Email;
        }

        return response;
    }

    /// <inheritdoc />
    public async Task<List<InternalNoteCommentResponse>> GetCommentsAsync(Guid noteId)
    {
        var comments = await _context.InternalNoteComments
            .Where(c => c.InternalNoteId == noteId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var responses = new List<InternalNoteCommentResponse>();

        // Collect unique principal IDs
        var principalIds = comments
            .Where(c => Guid.TryParse(c.CreatedBy, out _))
            .Select(c => Guid.Parse(c.CreatedBy))
            .Distinct()
            .ToList();

        var principalMap = new Dictionary<Guid, Maliev.CustomerService.Api.Models.IAM.PrincipalResponse>();
        foreach (var pId in principalIds)
        {
            var principal = await _iamClient.GetPrincipalByIdAsync(pId);
            if (principal != null) principalMap[pId] = principal;
        }

        foreach (var comment in comments)
        {
            var res = comment.ToInternalNoteCommentResponse();
            if (Guid.TryParse(comment.CreatedBy, out var pId) && principalMap.TryGetValue(pId, out var p))
            {
                res.CreatedByName = p.DisplayName;
                res.CreatedByEmail = p.Email;
            }
            responses.Add(res);
        }

        return responses;
    }

    /// <inheritdoc />
    public async Task<List<object>> GetNoteActivityAsync(Guid noteId)
    {
        // Get comments
        var comments = await GetCommentsAsync(noteId);

        // Get audit logs for this specific note
        var auditLogs = await _context.AuditLogs
            .Where(a => a.EntityType == nameof(InternalNote) && a.EntityId == noteId.ToString())
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        var activity = new List<object>();

        foreach (var comment in comments)
        {
            activity.Add(new
            {
                Type = "Comment",
                comment.Id,
                comment.CommentText,
                comment.CreatedBy,
                comment.CreatedByName,
                comment.CreatedByEmail,
                Timestamp = comment.CreatedAt
            });
        }

        foreach (var log in auditLogs)
        {
            if (log.Action == "Comment") continue; // Already added from comments table for richness

            activity.Add(new
            {
                Type = "Audit",
                log.Id,
                log.Action,
                log.ActorId,
                ActorName = (await _iamClient.GetPrincipalByIdAsync(Guid.TryParse(log.ActorId, out var aid) ? aid : Guid.Empty))?.DisplayName ?? log.ActorId,
                log.ChangedFields,
                log.PreviousValues,
                Timestamp = log.Timestamp
            });
        }

        return activity.OrderByDescending(a => (DateTime)a.GetType().GetProperty("Timestamp")!.GetValue(a)!).ToList();
    }
}
