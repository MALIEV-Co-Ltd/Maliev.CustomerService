using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Data.Repositories;

/// <summary>
/// Repository implementation for Company entity
/// </summary>
public class CompanyRepository : ICompanyRepository
{
    private readonly CustomerDbContext _context;

    private static readonly string[] TierOrder = { "Classic", "Silver", "Gold" };

    /// <inheritdoc/>
    public CompanyRepository(CustomerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        _context.Companies.Update(company);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Companies.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> ResetAllYearlyValuesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.CurrentYearPurchaseValue, 0)
                .SetProperty(c => c.CurrentYearOrderCount, 0),
                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> ApplyYearEndDemotionsAsync(CancellationToken cancellationToken = default)
    {
        int demotedCount = 0;

        var companies = await _context.Companies.ToListAsync(cancellationToken);

        foreach (var company in companies)
        {
            var currentTierIndex = Array.IndexOf(TierOrder, company.Tier);

            if (currentTierIndex > 0)
            {
                company.Tier = TierOrder[currentTierIndex - 1];
                demotedCount++;
            }

            company.TierCalculatedAt = DateTime.UtcNow;
        }

        if (demotedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return demotedCount;
    }
}
