using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Response model for customer data
/// </summary>
public class CustomerResponse
{
    /// <summary>
    /// Unique customer identifier
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Customer's first name
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's last name
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's email address
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Customer's phone number
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Customer segmentation: Retail, Wholesale, Enterprise, Government
    /// </summary>
    [JsonPropertyName("segment")]
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// Customer tier: Bronze, Silver, Gold, Platinum, VIP
    /// </summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Preferred language (ISO 639-1 format)
    /// </summary>
    [JsonPropertyName("preferredLanguage")]
    public string PreferredLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Timezone (IANA format)
    /// </summary>
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    /// <summary>
    /// Communication preferences (JSON object)
    /// </summary>
    [JsonPropertyName("communicationPreferences")]
    public Dictionary<string, object>? CommunicationPreferences { get; set; }

    /// <summary>
    /// Optional link to Company entity
    /// </summary>
    [JsonPropertyName("companyId")]
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Record last update timestamp
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
