using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Documents;

/// <summary>
/// Request model for deleting a document reference
/// </summary>
public class DeleteDocumentRequest
{
    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [Required]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
