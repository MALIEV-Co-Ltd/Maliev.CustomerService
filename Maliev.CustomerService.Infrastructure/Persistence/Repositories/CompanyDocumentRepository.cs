using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for CompanyDocument entity
/// </summary>
public class CompanyDocumentRepository : ICompanyDocumentRepository
{
    private readonly CustomerDbContext _context;

    /// <inheritdoc/>
    public CompanyDocumentRepository(CustomerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<CompanyDocument>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyDocuments
            .Where(d => d.CompanyId == companyId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CompanyDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyDocuments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CompanyDocument> CreateAsync(CompanyDocument document, CancellationToken cancellationToken = default)
    {
        _context.CompanyDocuments.Add(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, uint xmin, CancellationToken cancellationToken = default)
    {
        var deleted = await _context.CompanyDocuments
            .Where(d => d.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
