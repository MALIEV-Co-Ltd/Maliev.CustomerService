using Maliev.CustomerService.Api.Models.Documents;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for document reference management operations
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Creates a new document reference with Upload Service validation
    /// </summary>
    Task<DocumentResponse> CreateAsync(CreateDocumentRequest request, string actorId, string actorType);

    /// <summary>
    /// Retrieves document references by owner
    /// </summary>
    Task<List<DocumentResponse>> GetByOwnerAsync(string ownerType, Guid ownerId);

    /// <summary>
    /// Updates document reference with new file (versioning)
    /// </summary>
    Task<DocumentResponse> UpdateAsync(Guid id, UpdateDocumentRequest request, string actorId, string actorType);

    /// <summary>
    /// Marks document as complete
    /// </summary>
    Task<DocumentResponse> MarkCompleteAsync(Guid id, string? signedBy, DateTime? signedAt, string actorId, string actorType);

    /// <summary>
    /// Deletes a document reference and associated file from Upload Service
    /// </summary>
    Task DeleteAsync(Guid id, string actorId, string actorType);

    /// <summary>
    /// Retries pending deletions (for background job)
    /// </summary>
    Task<int> RetryPendingDeletionsAsync();
}
