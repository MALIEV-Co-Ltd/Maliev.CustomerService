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
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [Required(ErrorMessage = "xmin is required for updates")]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
