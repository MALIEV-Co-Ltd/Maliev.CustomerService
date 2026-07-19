using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.InternalNotes;

/// <summary>
/// Request model for deleting an internal note
/// </summary>
public class DeleteInternalNoteRequest
{
    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [Required]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
