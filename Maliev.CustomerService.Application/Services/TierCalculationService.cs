using Maliev.CustomerService.Application.DTOs;
using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Maliev.CustomerService.Application.Services;

/// <summary>
/// Service implementation for tier calculation operations
/// </summary>
public class TierCalculationService : ITierCalculationService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyTierSettingsRepository _tierSettingsRepository;
    private readonly ILogger<TierCalculationService> _logger;

    private static readonly string[] TierOrder = { "Classic", "Silver", "Gold" };

    public TierCalculationService(
        ICompanyRepository companyRepository,
        ICompanyTierSettingsRepository tierSettingsRepository,
        ILogger<TierCalculationService> logger)
    {
        _companyRepository = companyRepository;
        _tierSettingsRepository = tierSettingsRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<TierSettingsResponse>> GetTierSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _tierSettingsRepository.GetActiveSettingsAsync(cancellationToken);

        return settings.Select(s => new TierSettingsResponse
        {
            Id = s.Id,
            TierName = s.TierName,
            MinPurchaseValue = s.MinPurchaseValue,
            MinOrderCount = s.MinOrderCount,
            DiscountPercentage = s.DiscountPercentage,
            FreeShippingMinOrder = s.FreeShippingMinOrder,
            CoinRewardPercentage = s.CoinRewardPercentage,
            ValidFrom = s.ValidFrom,
            ValidTo = s.ValidTo,
            xmin = _tierSettingsRepository.GetXmin(s)
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<string> CalculateTierAsync(decimal purchaseValue, int orderCount, CancellationToken cancellationToken = default)
    {
        var settings = await _tierSettingsRepository.GetActiveSettingsAsync(cancellationToken);

        foreach (var tier in TierOrder.Reverse())
        {
            var tierSettings = settings.FirstOrDefault(s => s.TierName == tier);
            if (tierSettings != null)
            {
                if (purchaseValue >= tierSettings.MinPurchaseValue && orderCount >= tierSettings.MinOrderCount)
                {
                    return tier;
                }
            }
        }

        return "Classic";
    }

    /// <inheritdoc/>
    public async Task<bool> ApplyTierAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
        {
            _logger.LogWarning("Company {CompanyId} not found for tier calculation", companyId);
            return false;
        }

        var newTier = await CalculateTierAsync(company.CurrentYearPurchaseValue, company.CurrentYearOrderCount, cancellationToken);

        if (company.Tier != newTier)
        {
            _logger.LogInformation(
                "Company {CompanyId} tier changed from {OldTier} to {NewTier}",
                companyId, company.Tier, newTier);

            company.Tier = newTier;
            company.TierCalculatedAt = DateTime.UtcNow;
            await _companyRepository.UpdateAsync(company, cancellationToken);
            return true;
        }

        company.TierCalculatedAt = DateTime.UtcNow;
        await _companyRepository.UpdateAsync(company, cancellationToken);
        return false;
    }

    /// <inheritdoc/>
    public async Task ResetYearlyValuesAsync(CancellationToken cancellationToken = default)
    {
        var count = await _companyRepository.ResetAllYearlyValuesAsync(cancellationToken);
        _logger.LogInformation("Reset YTD values for {Count} companies", count);
    }

    /// <inheritdoc/>
    public async Task<int> ApplyYearEndDemotionsAsync(CancellationToken cancellationToken = default)
    {
        var demotedCount = await _companyRepository.ApplyYearEndDemotionsAsync(cancellationToken);
        _logger.LogInformation("Demoted {Count} companies at year-end", demotedCount);
        return demotedCount;
    }

    /// <inheritdoc/>
    public async Task<CompanyWithTierResponse?> GetCompanyWithTierAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
        {
            return null;
        }

        var tierSettings = await _tierSettingsRepository.GetByTierNameAsync(company.Tier, cancellationToken);

        var response = new CompanyWithTierResponse
        {
            Id = company.Id,
            Name = company.Name,
            Tier = company.Tier,
            CurrentYearPurchaseValue = company.CurrentYearPurchaseValue,
            CurrentYearOrderCount = company.CurrentYearOrderCount,
            TierCalculatedAt = company.TierCalculatedAt,
            DiscountPercentage = tierSettings?.DiscountPercentage ?? 0,
            FreeShippingMinOrder = tierSettings?.FreeShippingMinOrder,
            CoinRewardPercentage = tierSettings?.CoinRewardPercentage
        };

        var currentTierIndex = Array.IndexOf(TierOrder, company.Tier);
        if (currentTierIndex < TierOrder.Length - 1)
        {
            var nextTier = TierOrder[currentTierIndex + 1];
            var nextTierSettings = await _tierSettingsRepository.GetByTierNameAsync(nextTier, cancellationToken);

            if (nextTierSettings != null)
            {
                response.NextTierProgress = new TierProgressResponse
                {
                    NextTierName = nextTier,
                    RequiredPurchaseValue = nextTierSettings.MinPurchaseValue,
                    RequiredOrderCount = nextTierSettings.MinOrderCount,
                    CurrentPurchaseValue = company.CurrentYearPurchaseValue,
                    CurrentOrderCount = company.CurrentYearOrderCount,
                    PurchaseValueProgress = nextTierSettings.MinPurchaseValue > 0
                        ? (company.CurrentYearPurchaseValue / nextTierSettings.MinPurchaseValue) * 100
                        : 0,
                    OrderCountProgress = nextTierSettings.MinOrderCount > 0
                        ? ((decimal)company.CurrentYearOrderCount / nextTierSettings.MinOrderCount) * 100
                        : 0
                };
            }
        }

        return response;
    }

    /// <inheritdoc/>
    public async Task<decimal> GetDiscountPercentageAsync(string tierName, CancellationToken cancellationToken = default)
    {
        var settings = await _tierSettingsRepository.GetByTierNameAsync(tierName, cancellationToken);
        return settings?.DiscountPercentage ?? 0;
    }

    /// <inheritdoc/>
    public async Task<decimal?> GetFreeShippingThresholdAsync(string tierName, CancellationToken cancellationToken = default)
    {
        var settings = await _tierSettingsRepository.GetByTierNameAsync(tierName, cancellationToken);
        return settings?.FreeShippingMinOrder;
    }

    /// <inheritdoc/>
    public async Task<decimal?> GetCoinRewardPercentageAsync(string tierName, CancellationToken cancellationToken = default)
    {
        var settings = await _tierSettingsRepository.GetByTierNameAsync(tierName, cancellationToken);
        return settings?.CoinRewardPercentage;
    }
}
