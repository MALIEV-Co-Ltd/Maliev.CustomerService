using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Documents;

/// <summary>
/// Request model for updating a document reference (versioning)
/// </summary>
public class UpdateDocumentRequest
{
    /// <summary>
    /// New file reference from Upload Service for versioning
    /// </summary>
    [Required(ErrorMessage = "File reference is required")]
    [JsonPropertyName("fileReference")]
    public string FileReference { get; set; } = string.Empty;

    /// <summary>
    /// New filename
    /// </summary>
    [Required(ErrorMessage = "Filename is required")]
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Required(ErrorMessage = "Row version is required for updates")]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
