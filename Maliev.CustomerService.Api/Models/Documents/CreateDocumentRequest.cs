using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Documents;

/// <summary>
/// Request model for creating a document reference
/// </summary>
public class CreateDocumentRequest
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
    /// Document type classification
    /// </summary>
    [Required(ErrorMessage = "Document type is required")]
    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// File reference from Upload Service
    /// </summary>
    [Required(ErrorMessage = "File reference is required")]
    [JsonPropertyName("fileReference")]
    public string FileReference { get; set; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    [Required(ErrorMessage = "Filename is required")]
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;
}
