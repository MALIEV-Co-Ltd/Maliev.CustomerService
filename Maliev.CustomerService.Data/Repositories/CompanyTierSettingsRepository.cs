using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Data.Repositories;

/// <summary>
/// Repository implementation for CompanyTierSettings entity
/// </summary>
public class CompanyTierSettingsRepository : ICompanyTierSettingsRepository
{
    private readonly CustomerDbContext _context;

    /// <inheritdoc/>
    public CompanyTierSettingsRepository(CustomerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<CompanyTierSettings>> GetActiveSettingsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.CompanyTierSettings
            .Where(s => s.ValidFrom <= now && (s.ValidTo == null || s.ValidTo > now))
            .OrderBy(s => s.TierName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CompanyTierSettings?> GetByTierNameAsync(string tierName, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.CompanyTierSettings
            .FirstOrDefaultAsync(s => s.TierName == tierName && s.ValidFrom <= now && (s.ValidTo == null || s.ValidTo > now), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CompanyTierSettings?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyTierSettings
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CompanyTierSettings> CreateAsync(CompanyTierSettings settings, CancellationToken cancellationToken = default)
    {
        _context.CompanyTierSettings.Add(settings);
        await _context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(CompanyTierSettings settings, CancellationToken cancellationToken = default)
    {
        settings.UpdatedAt = DateTime.UtcNow;

        var updated = await _context.CompanyTierSettings
            .Where(s => s.Id == settings.Id && s.xmin == settings.xmin)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.TierName, settings.TierName)
                .SetProperty(s => s.MinPurchaseValue, settings.MinPurchaseValue)
                .SetProperty(s => s.MinOrderCount, settings.MinOrderCount)
                .SetProperty(s => s.DiscountPercentage, settings.DiscountPercentage)
                .SetProperty(s => s.FreeShippingMinOrder, settings.FreeShippingMinOrder)
                .SetProperty(s => s.CoinRewardPercentage, settings.CoinRewardPercentage)
                .SetProperty(s => s.ValidFrom, settings.ValidFrom)
                .SetProperty(s => s.ValidTo, settings.ValidTo)
                .SetProperty(s => s.UpdatedAt, settings.UpdatedAt),
                cancellationToken);

        return updated > 0;
    }
}
