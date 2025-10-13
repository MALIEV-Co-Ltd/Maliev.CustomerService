using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.NDAs;

/// <summary>
/// Response model for NDA data
/// </summary>
public class NDAResponse
{
    /// <summary>
    /// Unique identifier for the NDA record
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Customer ID associated with this NDA
    /// </summary>
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Reference to the document in the Document Management system
    /// </summary>
    [JsonPropertyName("documentReferenceId")]
    public Guid? DocumentReferenceId { get; set; }

    /// <summary>
    /// NDA status (Draft, Signed, Expired, Revoked)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// User ID who signed the NDA
    /// </summary>
    [JsonPropertyName("signedBy")]
    public string? SignedBy { get; set; }

    /// <summary>
    /// Timestamp when the NDA was signed
    /// </summary>
    [JsonPropertyName("signedAt")]
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Timestamp when the NDA was revoked
    /// </summary>
    [JsonPropertyName("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Expiration date for the NDA
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

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
