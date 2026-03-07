using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// NDA (Non-Disclosure Agreement) record entity for tracking NDA lifecycle
/// Supports lifecycle: Draft → Signed → Expired/Revoked
/// </summary>
public class NDARecord
{
    /// <summary>
    /// Unique identifier for the NDA record
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Customer ID associated with this NDA
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Reference to the document in the Document Management system (optional until signed)
    /// </summary>
    public Guid? DocumentReferenceId { get; set; }

    /// <summary>
    /// NDA status (Draft, Signed, Expired, Revoked)
    /// </summary>
    public string Status { get; set; } = NDAStatus.Draft;

    /// <summary>
    /// User ID who signed the NDA (required when status is Signed)
    /// </summary>
    public string? SignedBy { get; set; }

    /// <summary>
    /// Timestamp when the NDA was signed
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Timestamp when the NDA was revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Reason for revoking the NDA
    /// </summary>
    public string? RevokeReason { get; set; }

    /// <summary>
    /// Expiration date for the NDA
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}
