using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Response model for customer preferences (for compliance/audit purposes)
/// </summary>
public class GetCustomerPreferencesResponse
{
    /// <summary>
    /// Customer ID
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Customer email
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Customer segment
    /// </summary>
    [JsonPropertyName("segment")]
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// Customer tier
    /// </summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Preferred language (ISO 639-1)
    /// </summary>
    [JsonPropertyName("preferredLanguage")]
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Timezone (IANA timezone database)
    /// </summary>
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    /// <summary>
    /// Communication preferences (JSON)
    /// </summary>
    [JsonPropertyName("communicationPreferences")]
    public object? CommunicationPreferences { get; set; }
}
