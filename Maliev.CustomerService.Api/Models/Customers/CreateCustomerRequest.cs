using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Request model for creating a new customer
/// </summary>
public class CreateCustomerRequest
{
    /// <summary>
    /// Customer's first name (required, max 100 chars)
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name must not exceed 100 characters")]
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's last name (required, max 100 chars)
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name must not exceed 100 characters")]
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's email address (required, must be valid email format)
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [MaxLength(255, ErrorMessage = "Email must not exceed 255 characters")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Customer's phone number (optional, E.164 format)
    /// </summary>
    [Phone(ErrorMessage = "Phone must be a valid phone number")]
    [MaxLength(20, ErrorMessage = "Phone must not exceed 20 characters")]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Customer segmentation: Retail, Wholesale, Enterprise, Government (default: Retail)
    /// </summary>
    [Required(ErrorMessage = "Segment is required")]
    [JsonPropertyName("segment")]
    public string Segment { get; set; } = "Retail";

    /// <summary>
    /// Customer tier: Bronze, Silver, Gold, Platinum, VIP (default: Bronze)
    /// </summary>
    [Required(ErrorMessage = "Tier is required")]
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "Bronze";

    /// <summary>
    /// Preferred language (ISO 639-1 format, e.g., "en", "th")
    /// </summary>
    [Required(ErrorMessage = "Preferred language is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Preferred language must be 2 characters")]
    [JsonPropertyName("preferredLanguage")]
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>
    /// Timezone (IANA format, e.g., "Asia/Bangkok", "UTC")
    /// </summary>
    [Required(ErrorMessage = "Timezone is required")]
    [MaxLength(50, ErrorMessage = "Timezone must not exceed 50 characters")]
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "UTC";

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
}
