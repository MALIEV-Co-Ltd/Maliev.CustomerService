using Maliev.CustomerService.Domain.Entities;

namespace Maliev.CustomerService.Application.Interfaces;

/// <summary>
/// Repository interface for Company entity operations
/// </summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Gets a company by ID
    /// </summary>
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a company by ID with tier settings
    /// </summary>
    Task<Company?> GetByIdWithTierSettingsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a company
    /// </summary>
    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all companies
    /// </summary>
    Task<List<Company>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets YTD values for all companies using batch update
    /// </summary>
    Task<int> ResetAllYearlyValuesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies year-end demotions and returns count of demoted companies
    /// </summary>
    Task<int> ApplyYearEndDemotionsAsync(CancellationToken cancellationToken = default);
}
