using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.InternalNotes;

/// <summary>
/// Request model for creating an internal note
/// </summary>
public class CreateInternalNoteRequest
{
    /// <summary>
    /// Owner type (Customer or Company)
    /// </summary>
    [Required(ErrorMessage = "Owner type is required")]
    [JsonPropertyName("ownerType")]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>
    /// Owner ID (Customer ID or Company ID)
    /// </summary>
    [Required(ErrorMessage = "Owner ID is required")]
    [JsonPropertyName("ownerId")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Note text content
    /// </summary>
    [Required(ErrorMessage = "Note text is required")]
    [JsonPropertyName("noteText")]
    public string NoteText { get; set; } = string.Empty;
}
