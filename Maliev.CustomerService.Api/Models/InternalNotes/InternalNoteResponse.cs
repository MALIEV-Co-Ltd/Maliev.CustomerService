using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.InternalNotes;

/// <summary>
/// Response model for internal note data
/// </summary>
public class InternalNoteResponse
{
    /// <summary>
    /// Unique identifier for the internal note
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Owner type (Customer or Company)
    /// </summary>
    [JsonPropertyName("ownerType")]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>
    /// Owner ID (Customer ID or Company ID)
    /// </summary>
    [JsonPropertyName("ownerId")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Note text content
    /// </summary>
    [JsonPropertyName("noteText")]
    public string NoteText { get; set; } = string.Empty;

    /// <summary>
    /// User ID who created the note
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
