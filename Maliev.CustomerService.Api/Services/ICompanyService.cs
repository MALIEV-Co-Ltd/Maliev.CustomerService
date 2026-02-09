using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Models.Customers;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for company management operations
/// </summary>
public interface ICompanyService
{
    /// <summary>
    /// Creates a new company with audit logging
    /// </summary>
    /// <param name="request">Company creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created company response</returns>
    /// <exception cref="InvalidOperationException">Thrown when VAT number format is invalid</exception>
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, string actorId, string actorType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a company by ID
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company response or null if not found</returns>
    Task<CompanyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing company with optimistic concurrency control and audit logging
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="request">Company update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated company response</returns>
    /// <exception cref="KeyNotFoundException">Thrown when company is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when VAT number format is invalid or version conflict occurs</exception>
    Task<CompanyResponse> UpdateAsync(Guid id, UpdateCompanyRequest request, string actorId, string actorType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a company with its associated active customers
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing company response and list of associated customer responses, or null if company not found</returns>
    Task<(CompanyResponse Company, List<CustomerResponse> Customers)?> GetWithCustomersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all companies with pagination and optional filtering by segment and tier
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="segment">Optional segment filter</param>
    /// <param name="tier">Optional tier filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing list of company responses and total count</returns>
    Task<(List<CompanyResponse> Companies, int TotalCount)> GetAllAsync(int page, int pageSize, string? segment = null, string? tier = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for companies by name or VAT number and includes their default billing address
    /// </summary>
    /// <param name="query">Search query (name or VAT)</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of company search results with addresses</returns>
    Task<List<CompanySearchResultDto>> SearchWithAddressAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
}
