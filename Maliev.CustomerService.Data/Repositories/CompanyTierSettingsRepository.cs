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

        // Use the change tracker so EF Core refreshes xmin on the entity after save.
        // ExecuteUpdateAsync bypasses the change tracker, causing xmin to become stale
        // and breaking optimistic concurrency on subsequent requests.
        _context.CompanyTierSettings.Update(settings);

        try
        {
            var updated = await _context.SaveChangesAsync(cancellationToken);
            return updated > 0;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
