using Maliev.CustomerService.Domain.Entities;

namespace Maliev.CustomerService.Application.Interfaces;

/// <summary>
/// Repository interface for CompanyDocument entity operations
/// </summary>
public interface ICompanyDocumentRepository
{
    /// <summary>
    /// Gets all documents for a company
    /// </summary>
    Task<List<CompanyDocument>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by ID
    /// </summary>
    Task<CompanyDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document
    /// </summary>
    Task<CompanyDocument> CreateAsync(CompanyDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document
    /// </summary>
    Task<bool> DeleteAsync(Guid id, uint xmin, CancellationToken cancellationToken = default);
}
