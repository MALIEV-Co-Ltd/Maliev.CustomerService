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
    public async Task<Company?> GetByIdWithTierSettingsAsync(Guid id, CancellationToken cancellationToken = default)
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
}
