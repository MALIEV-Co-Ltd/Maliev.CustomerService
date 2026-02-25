using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Document records attached to companies for compliance and verification
/// </summary>
public class CompanyDocument
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to Company
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Document type (TaxCert, BusinessLicense, Contract, Other)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// GCS URL
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Document expiration date
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency
    /// </summary>
    public uint xmin { get; set; }

    /// <summary>
    /// Navigation property to Company
    /// </summary>
    public virtual Company? Company { get; set; }
}
