using Maliev.CustomerService.Api.Models.Addresses;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for address management operations
/// </summary>
public interface IAddressService
{
    /// <summary>
    /// Creates a new address with country validation and audit logging
    /// </summary>
    /// <param name="request">Address creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <returns>Created address response</returns>
    /// <exception cref="InvalidOperationException">Country Service unavailable or invalid country ID</exception>
    Task<AddressResponse> CreateAsync(CreateAddressRequest request, string actorId, string actorType);

    /// <summary>
    /// Retrieves all addresses for a specific owner
    /// </summary>
    /// <param name="ownerType">Type of owner (Customer or Company)</param>
    /// <param name="ownerId">Owner ID</param>
    /// <returns>List of addresses</returns>
    Task<List<AddressResponse>> GetByOwnerAsync(string ownerType, Guid ownerId);

    /// <summary>
    /// Updates an existing address with optimistic concurrency control, country validation, and audit logging
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="request">Address update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <returns>Updated address response</returns>
    /// <exception cref="KeyNotFoundException">Address not found</exception>
    /// <exception cref="InvalidOperationException">Country Service unavailable or invalid country ID, or version conflict</exception>
    Task<AddressResponse> UpdateAsync(Guid id, UpdateAddressRequest request, string actorId, string actorType);

    /// <summary>
    /// Deletes an address with audit logging
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, string actorId, string actorType);
}
