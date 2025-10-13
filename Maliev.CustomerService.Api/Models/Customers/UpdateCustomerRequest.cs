using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Request model for updating an existing customer
/// </summary>
public class UpdateCustomerRequest
{
    /// <summary>
    /// Customer's first name (optional, max 100 chars)
    /// </summary>
    [MaxLength(100, ErrorMessage = "First name must not exceed 100 characters")]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Customer's last name (optional, max 100 chars)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Last name must not exceed 100 characters")]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    /// <summary>
    /// Customer's email address (optional, must be valid email format)
    /// </summary>
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [MaxLength(255, ErrorMessage = "Email must not exceed 255 characters")]
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Customer's phone number (optional, E.164 format)
    /// </summary>
    [Phone(ErrorMessage = "Phone must be a valid phone number")]
    [MaxLength(20, ErrorMessage = "Phone must not exceed 20 characters")]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Customer segmentation: Retail, Wholesale, Enterprise, Government
    /// </summary>
    [JsonPropertyName("segment")]
    public string? Segment { get; set; }

    /// <summary>
    /// Customer tier: Bronze, Silver, Gold, Platinum, VIP
    /// </summary>
    [JsonPropertyName("tier")]
    public string? Tier { get; set; }

    /// <summary>
    /// Preferred language (ISO 639-1 format, e.g., "en", "th")
    /// </summary>
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Preferred language must be 2 characters")]
    [JsonPropertyName("preferredLanguage")]
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Timezone (IANA format, e.g., "Asia/Bangkok", "UTC")
    /// </summary>
    [MaxLength(50, ErrorMessage = "Timezone must not exceed 50 characters")]
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    /// <summary>
    /// Communication preferences (JSON object with email_opt_in, sms_opt_in, etc.)
    /// </summary>
    [JsonPropertyName("communicationPreferences")]
    public Dictionary<string, object>? CommunicationPreferences { get; set; }

    /// <summary>
    /// Optional link to Company entity
    /// </summary>
    [JsonPropertyName("companyId")]
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control (required)
    /// </summary>
    [Required(ErrorMessage = "Version is required for updates")]
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
