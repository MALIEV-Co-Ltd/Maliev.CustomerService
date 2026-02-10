using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Documents;

/// <summary>
/// Response model for document reference data
/// </summary>
public class DocumentResponse
{
    /// <summary>
    /// Unique identifier for the document reference
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
    /// Document type classification
    /// </summary>
    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// File reference from Upload Service
    /// </summary>
    [JsonPropertyName("fileReference")]
    public string FileReference { get; set; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Document status (Pending, Complete, PendingDeletion, Orphaned, MissingFile)
    /// </summary>

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Document version number
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>
    /// User who signed the document
    /// </summary>
    [JsonPropertyName("signedBy")]
    public string? SignedBy { get; set; }

    /// <summary>
    /// User who created the document
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the creator
    /// </summary>
    [JsonPropertyName("createdByName")]
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Email of the creator
    /// </summary>
    [JsonPropertyName("createdByEmail")]
    public string? CreatedByEmail { get; set; }

    /// <summary>
    /// Timestamp when document was signed
    /// </summary>

    [JsonPropertyName("signedAt")]
    public DateTime? SignedAt { get; set; }

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
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
