using Maliev.CustomerService.Api.Models.NDAs;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for NDA lifecycle management operations
/// </summary>
public interface INDAService
{
    /// <summary>
    /// Creates a new NDA record in Draft status
    /// </summary>
    Task<NDAResponse> CreateAsync(CreateNDARequest request, string actorId, string actorType);

    /// <summary>
    /// Retrieves an NDA record by ID
    /// </summary>
    Task<NDAResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// Updates NDA status with lifecycle validation
    /// </summary>
    Task<NDAResponse> UpdateStatusAsync(Guid id, UpdateNDAStatusRequest request, string actorId, string actorType);

    /// <summary>
    /// Checks for expired NDAs and transitions them to Expired status
    /// </summary>
    /// <returns>Count of NDAs that were expired</returns>
    Task<int> CheckExpiredNDAsAsync();
}
