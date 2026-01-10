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
    /// <param name="request">Document creation request containing owner details and file reference</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document response</returns>
    /// <exception cref="InvalidOperationException">Thrown when file reference is invalid in Upload Service</exception>
    Task<DocumentResponse> CreateAsync(CreateDocumentRequest request, string actorId, string actorType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all document references for a specific owner
    /// </summary>
    /// <param name="ownerType">Type of owner (Customer or Company)</param>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document responses ordered by creation date descending</returns>
    Task<List<DocumentResponse>> GetByOwnerAsync(string ownerType, Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document reference with a new file (creates new version)
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="request">Update request containing new file reference and filename</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document response with incremented version</returns>
    /// <exception cref="KeyNotFoundException">Thrown when document is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when file reference is invalid or version conflict occurs</exception>
    Task<DocumentResponse> UpdateAsync(Guid id, UpdateDocumentRequest request, string actorId, string actorType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a document as complete with optional signature information
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="signedBy">Optional name of the person who signed the document</param>
    /// <param name="signedAt">Optional timestamp when the document was signed</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document response with Complete status</returns>
    /// <exception cref="KeyNotFoundException">Thrown when document is not found</exception>
    Task<DocumentResponse> MarkCompleteAsync(Guid id, string? signedBy, DateTime? signedAt, string actorId, string actorType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document reference and associated file from Upload Service
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="KeyNotFoundException">Thrown when document is not found</exception>
    /// <remarks>
    /// If deletion from Upload Service fails, the document is marked as PendingDeletion for retry
    /// </remarks>
    Task DeleteAsync(Guid id, string actorId, string actorType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries deletion of documents marked as PendingDeletion (for background job processing)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of successfully deleted documents</returns>
    Task<int> RetryPendingDeletionsAsync(CancellationToken cancellationToken = default);
}
