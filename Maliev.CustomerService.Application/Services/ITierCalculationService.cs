using Maliev.CustomerService.Application.DTOs;
using Maliev.CustomerService.Domain.Entities;

namespace Maliev.CustomerService.Application.Services;

/// <summary>
/// Service interface for tier calculation operations
/// </summary>
public interface ITierCalculationService
{
    /// <summary>
    /// Gets all active tier settings
    /// </summary>
    Task<List<TierSettingsResponse>> GetTierSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the appropriate tier for a company based on YTD values
    /// </summary>
    /// <param name="purchaseValue">YTD purchase value</param>
    /// <param name="orderCount">YTD order count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The calculated tier name</returns>
    Task<string> CalculateTierAsync(decimal purchaseValue, int orderCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the calculated tier to a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if tier was changed, false otherwise</returns>
    Task<bool> ApplyTierAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets YTD values for all companies
    /// </summary>
    Task ResetYearlyValuesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies year-end demotions (Gold→Silver→Classic)
    /// </summary>
    /// <returns>Number of companies demoted</returns>
    Task<int> ApplyYearEndDemotionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets company with tier information including benefits
    /// </summary>
    Task<CompanyWithTierResponse?> GetCompanyWithTierAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the discount percentage for a company tier
    /// </summary>
    Task<decimal> GetDiscountPercentageAsync(string tierName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the free shipping threshold for a company tier
    /// </summary>
    Task<decimal?> GetFreeShippingThresholdAsync(string tierName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the coin reward percentage for a company tier
    /// </summary>
    Task<decimal?> GetCoinRewardPercentageAsync(string tierName, CancellationToken cancellationToken = default);
}
