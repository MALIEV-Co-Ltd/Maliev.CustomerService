using Maliev.CustomerService.Api.Models.InternalNotes;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for internal note management operations
/// </summary>
public interface IInternalNoteService
{
    /// <summary>
    /// Creates a new internal note with audit logging
    /// </summary>
    /// <param name="request">Internal note creation request</param>
    /// <param name="createdBy">ID of the employee creating the note</param>
    /// <returns>Created internal note response</returns>
    Task<InternalNoteResponse> CreateAsync(CreateInternalNoteRequest request, string createdBy);

    /// <summary>
    /// Retrieves all internal notes for a specific owner
    /// </summary>
    /// <param name="ownerType">Type of owner (Customer or Company)</param>
    /// <param name="ownerId">Owner ID</param>
    /// <returns>List of internal notes ordered by creation date descending</returns>
    Task<List<InternalNoteResponse>> GetByOwnerAsync(string ownerType, Guid ownerId);

    /// <summary>
    /// Updates an existing internal note with optimistic concurrency control
    /// </summary>
    /// <param name="id">Internal note ID</param>
    /// <param name="request">Update request containing new note text and version</param>
    /// <param name="actorId">ID of the employee updating the note</param>
    /// <returns>Updated internal note response</returns>
    /// <exception cref="KeyNotFoundException">Thrown when internal note is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when version conflict occurs</exception>
    Task<InternalNoteResponse> UpdateAsync(Guid id, UpdateInternalNoteRequest request, string actorId);

    /// <summary>
    /// Deletes an internal note with audit logging
    /// </summary>
    /// <param name="id">Internal note ID</param>
    /// <exception cref="KeyNotFoundException">Thrown when internal note is not found</exception>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Adds a comment to an existing internal note
    /// </summary>
    Task<InternalNoteCommentResponse> AddCommentAsync(Guid noteId, CreateInternalNoteCommentRequest request, string actorId);

    /// <summary>
    /// Retrieves all comments for a specific internal note
    /// </summary>
    Task<List<InternalNoteCommentResponse>> GetCommentsAsync(Guid noteId);

    /// <summary>
    /// Retrieves detailed activity for a specific internal note (comments + audit logs)
    /// </summary>
    Task<List<object>> GetNoteActivityAsync(Guid noteId);
}
