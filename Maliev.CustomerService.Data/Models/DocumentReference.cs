using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Document reference entity for tracking document metadata with Upload Service integration
/// </summary>
[Table("document_references")]
public class DocumentReference
{
    /// <summary>
    /// Unique identifier for the document reference
    /// </summary>
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Owner type (Customer or Company)
    /// </summary>
    [Column("owner_type")]
    [Required]
    [MaxLength(50)]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>
    /// Owner ID (references Customer.Id or Company.Id)
    /// </summary>
    [Column("owner_id")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Document type classification
    /// </summary>
    [Column("document_type")]
    [Required]
    [MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// File reference from Upload Service
    /// </summary>
    [Column("file_reference")]
    [Required]
    [MaxLength(500)]
    public string FileReference { get; set; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    [Column("filename")]
    [Required]
    [MaxLength(255)]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Document status (Pending, Complete, PendingDeletion, Orphaned, MissingFile)
    /// </summary>
    [Column("status")]
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = DocumentStatus.Pending;

    /// <summary>
    /// Document version number
    /// </summary>
    [Column("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// User who signed the document
    /// </summary>
    [Column("signed_by")]
    [MaxLength(256)]
    public string? SignedBy { get; set; }

    /// <summary>
    /// Timestamp when document was signed
    /// </summary>
    [Column("signed_at")]
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Column("file_size")]
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    [Column("mime_type")]
    [MaxLength(100)]
    public string MimeType { get; set; } = "application/octet-stream";

    /// <summary>
    /// User who created/uploaded the document
    /// </summary>
    [Column("created_by")]
    [MaxLength(256)]
    public string CreatedBy { get; set; } = "System";

    /// <summary>
    /// Creation timestamp
    /// </summary>

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Column("row_version")]
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
