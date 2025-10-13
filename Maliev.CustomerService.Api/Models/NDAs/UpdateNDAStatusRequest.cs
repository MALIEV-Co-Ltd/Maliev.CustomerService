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
    /// Row version for optimistic concurrency control
    /// </summary>
    [Required(ErrorMessage = "Version is required for updates")]
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
