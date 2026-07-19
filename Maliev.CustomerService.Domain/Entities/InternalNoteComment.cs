using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Comment on an internal note
/// </summary>
[Table("internal_note_comments")]
public class InternalNoteComment
{
    /// <summary>
    /// Unique identifier for the comment
    /// </summary>
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the parent internal note
    /// </summary>
    [Column("internal_note_id")]
    public Guid InternalNoteId { get; set; }

    /// <summary>
    /// Comment text content
    /// </summary>
    [Column("comment_text")]
    [Required]
    [MaxLength(2000)]
    public string CommentText { get; set; } = string.Empty;

    /// <summary>
    /// User ID who created the comment
    /// </summary>
    [Column("created_by")]
    [Required]
    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// User name who created the comment (denormalized for display)
    /// </summary>
    [Column("created_by_name")]
    [MaxLength(256)]
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to parent note
    /// </summary>
    [ForeignKey(nameof(InternalNoteId))]
    public virtual InternalNote Note { get; set; } = null!;
}
