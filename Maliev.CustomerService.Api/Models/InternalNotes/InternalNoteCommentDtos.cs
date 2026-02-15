using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.InternalNotes;

/// <summary>
/// Request for creating a comment on an internal note
/// </summary>
public class CreateInternalNoteCommentRequest
{
    /// <summary>
    /// Comment text content
    /// </summary>
    [Required]
    [MaxLength(2000)]
    [JsonPropertyName("commentText")]
    public string CommentText { get; set; } = string.Empty;
}

/// <summary>
/// Response containing internal note comment details
/// </summary>
public class InternalNoteCommentResponse
{
    /// <summary>
    /// Unique identifier for the comment
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Parent internal note ID
    /// </summary>
    [JsonPropertyName("internalNoteId")]
    public Guid InternalNoteId { get; set; }

    /// <summary>
    /// Comment text content
    /// </summary>
    [JsonPropertyName("commentText")]
    public string CommentText { get; set; } = string.Empty;

    /// <summary>
    /// User ID who created the comment
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// User name who created the comment
    /// </summary>
    [JsonPropertyName("createdByName")]
    public string? CreatedByName { get; set; }

    /// <summary>
    /// User email who created the comment
    /// </summary>
    [JsonPropertyName("createdByEmail")]
    public string? CreatedByEmail { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Row version for concurrency control
    /// </summary>
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
