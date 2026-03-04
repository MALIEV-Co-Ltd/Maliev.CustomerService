using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.NDAs;

/// <summary>
/// Request model for updating NDA status (lifecycle transitions)
/// </summary>
public class UpdateNDAStatusRequest
{
    /// <summary>
    /// New status (Draft, Signed, Expired, Revoked)
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// User ID who signed the NDA (required when transitioning to Signed)
    /// </summary>
    [JsonPropertyName("signedBy")]
    public string? SignedBy { get; set; }

    /// <summary>
    /// Timestamp when the NDA was signed (required when transitioning to Signed)
    /// </summary>
    [JsonPropertyName("signedAt")]
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Timestamp when the NDA was revoked (required when transitioning to Revoked)
    /// </summary>
    [JsonPropertyName("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Reason for revoking the NDA (required when transitioning to Revoked)
    /// </summary>
    [JsonPropertyName("revokeReason")]
    public string? RevokeReason { get; set; }

    /// <summary>
    /// Expiration date for the NDA
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Optional: New document reference ID (if updating the document)
    /// </summary>
    [JsonPropertyName("documentReferenceId")]
    public Guid? DocumentReferenceId { get; set; }

    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [Required(ErrorMessage = "xmin is required for updates")]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
