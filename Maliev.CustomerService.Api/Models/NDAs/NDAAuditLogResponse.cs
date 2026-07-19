using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.NDAs;

/// <summary>
/// Response model for NDA audit log data
/// </summary>
public class NDAAuditLogResponse
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Action performed (Create, Update, Delete, etc.)
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// ID of the actor who performed the action
    /// </summary>
    [JsonPropertyName("actorId")]
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the actor (Customer, Employee, System)
    /// </summary>
    [JsonPropertyName("actorType")]
    public string ActorType { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the actor (if resolved)
    /// </summary>
    [JsonPropertyName("actorName")]
    public string? ActorName { get; set; }

    /// <summary>
    /// Timestamp of the action
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// NDA status at this point
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Previous status (if applicable)
    /// </summary>
    [JsonPropertyName("previousStatus")]
    public string? PreviousStatus { get; set; }

    /// <summary>
    /// Expiration date
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Revocation date
    /// </summary>
    [JsonPropertyName("revokedAt")]
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// ID of the document associated with this version
    /// </summary>
    [JsonPropertyName("documentReferenceId")]
    public Guid? DocumentReferenceId { get; set; }

    /// <summary>
    /// ID of the previous document (if changed)
    /// </summary>
    [JsonPropertyName("previousDocumentReferenceId")]
    public Guid? PreviousDocumentReferenceId { get; set; }

    /// <summary>
    /// Name of the document associated with this version
    /// </summary>
    [JsonPropertyName("documentName")]
    public string? DocumentName { get; set; }

    /// <summary>
    /// Name of the previous document (if changed)
    /// </summary>
    [JsonPropertyName("previousDocumentName")]
    public string? PreviousDocumentName { get; set; }
}
