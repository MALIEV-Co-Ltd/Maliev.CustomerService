using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Internal note entity for employee-only customer and company context
/// </summary>
[Table("internal_notes")]
public class InternalNote
{
    /// <summary>
    /// Unique identifier for the internal note
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
    /// Note text content
    /// </summary>
    [Column("note_text")]
    [Required]
    [MaxLength(5000)]
    public string NoteText { get; set; } = string.Empty;

    /// <summary>
    /// User ID who created the note
    /// </summary>
    [Column("created_by")]
    [Required]
    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

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
    [Column("version")]
    [Timestamp]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
