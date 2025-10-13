using Maliev.CustomerService.Api.Models.InternalNotes;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for internal note management operations
/// </summary>
public interface IInternalNoteService
{
    /// <summary>
    /// Creates a new internal note
    /// </summary>
    Task<InternalNoteResponse> CreateAsync(CreateInternalNoteRequest request, string createdBy);

    /// <summary>
    /// Retrieves internal notes by owner
    /// </summary>
    Task<List<InternalNoteResponse>> GetByOwnerAsync(string ownerType, Guid ownerId);

    /// <summary>
    /// Updates an internal note
    /// </summary>
    Task<InternalNoteResponse> UpdateAsync(Guid id, UpdateInternalNoteRequest request, string actorId);

    /// <summary>
    /// Deletes an internal note
    /// </summary>
    Task DeleteAsync(Guid id);
}
