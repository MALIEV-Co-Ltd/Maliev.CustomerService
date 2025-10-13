using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.InternalNotes;

/// <summary>
/// Request model for updating an internal note
/// </summary>
public class UpdateInternalNoteRequest
{
    /// <summary>
    /// Note text content
    /// </summary>
    [Required(ErrorMessage = "Note text is required")]
    [JsonPropertyName("noteText")]
    public string NoteText { get; set; } = string.Empty;

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Required(ErrorMessage = "Version is required for updates")]
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
