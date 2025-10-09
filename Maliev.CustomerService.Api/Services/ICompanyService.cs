using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Models.Customers;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for company management operations
/// </summary>
public interface ICompanyService
{
    /// <summary>
    /// Creates a new company
    /// </summary>
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, string actorId, string actorType);

    /// <summary>
    /// Retrieves a company by ID
    /// </summary>
    Task<CompanyResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// Updates an existing company
    /// </summary>
    Task<CompanyResponse> UpdateAsync(Guid id, UpdateCompanyRequest request, string actorId, string actorType);

    /// <summary>
    /// Retrieves a company with its associated customers
    /// </summary>
    Task<(CompanyResponse Company, List<CustomerResponse> Customers)?> GetWithCustomersAsync(Guid id);

    /// <summary>
    /// Retrieves all companies with pagination and optional filtering
    /// </summary>
    Task<(List<CompanyResponse> Companies, int TotalCount)> GetAllAsync(int page, int pageSize, string? segment = null, string? tier = null);
}
