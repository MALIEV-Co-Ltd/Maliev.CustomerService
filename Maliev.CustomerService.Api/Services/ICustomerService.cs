using Maliev.CustomerService.Api.Models.Customers;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for customer management operations
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Creates a new customer with audit logging
    /// </summary>
    /// <param name="request">Customer creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created customer response</returns>
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, string actorId, string actorType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer response or null if not found</returns>
    Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a customer by their central IAM Principal ID
    /// </summary>
    /// <param name="principalId">The IAM Principal ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer response or null if not found</returns>
    Task<CustomerResponse?> GetByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer with optimistic concurrency control and audit logging
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="request">Customer update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated customer response</returns>
    /// <exception cref="KeyNotFoundException">Customer not found</exception>
    /// <exception cref="InvalidOperationException">Version conflict</exception>
    Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, string actorId, string actorType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a customer with audit logging
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> SoftDeleteAsync(Guid id, string actorId, string actorType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all customers with optional filtering and pagination (T119-T120, T126-T127)
    /// </summary>
    Task<PaginatedResponse<CustomerResponse>> GetAllAsync(
        string? query = null,
        string? segment = null,
        string? tier = null,
        string? preferredLanguage = null,
        string? email = null,
        Guid? companyId = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        bool includeDeleted = false,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customer preferences for compliance/audit purposes (T123)
    /// </summary>
    Task<PaginatedResponse<GetCustomerPreferencesResponse>> GetPreferencesAsync(
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer with the specified email already exists
    /// </summary>
    /// <param name="email">Email address to check (will be normalized to lowercase)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
