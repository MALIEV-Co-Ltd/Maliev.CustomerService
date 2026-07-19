using Maliev.CustomerService.Domain.Entities;

namespace Maliev.CustomerService.Application.Interfaces;

/// <summary>
/// Repository interface for CompanyTierSettings entity operations
/// </summary>
public interface ICompanyTierSettingsRepository
{
    /// <summary>
    /// Gets all active tier settings
    /// </summary>
    Task<List<CompanyTierSettings>> GetActiveSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tier settings by tier name
    /// </summary>
    Task<CompanyTierSettings?> GetByTierNameAsync(string tierName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tier settings by ID
    /// </summary>
    Task<CompanyTierSettings?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new tier setting
    /// </summary>
    Task<CompanyTierSettings> CreateAsync(CompanyTierSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates tier settings with optimistic concurrency
    /// </summary>
    Task<bool> UpdateAsync(CompanyTierSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current xmin value for a tracked entity
    /// </summary>
    uint GetXmin(CompanyTierSettings settings);
}
