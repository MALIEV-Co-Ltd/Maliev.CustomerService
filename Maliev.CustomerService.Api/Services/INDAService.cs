using Maliev.CustomerService.Api.Models.NDAs;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for NDA lifecycle management operations
/// </summary>
public interface INDAService
{
    /// <summary>
    /// Creates a new NDA record in Draft status with audit logging
    /// </summary>
    /// <param name="request">NDA creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="actorName">Name of the actor performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created NDA response</returns>
    Task<NDAResponse> CreateAsync(CreateNDARequest request, string actorId, string actorType, string actorName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an NDA record by ID
    /// </summary>
    /// <param name="id">NDA ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NDA response or null if not found</returns>
    Task<NDAResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all NDA records for a specific customer
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of NDA responses</returns>
    Task<List<NDAResponse>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates NDA status with lifecycle validation and audit logging
    /// </summary>
    /// <param name="id">NDA ID</param>
    /// <param name="request">Status update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="actorName">Name of the actor performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated NDA response</returns>
    /// <exception cref="KeyNotFoundException">Thrown when NDA is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when lifecycle transition is invalid or version conflict occurs</exception>
    Task<NDAResponse> UpdateStatusAsync(Guid id, UpdateNDAStatusRequest request, string actorId, string actorType, string actorName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for expired NDAs and transitions them to Expired status (for background job processing)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of NDAs that were expired</returns>
    Task<int> CheckExpiredNDAsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for NDAs approaching expiration and publishes warning events
    /// </summary>
    Task<int> CheckUpcomingExpirationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an NDA record
    /// </summary>
    Task<bool> DeleteAsync(Guid id, uint xmin, string actorId, string actorType, string actorName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit history for a specific NDA
    /// </summary>
    Task<List<NDAAuditLogResponse>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
