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
    /// Reference to the central IAM principal ID
    /// </summary>
    [JsonPropertyName("principalId")]
    public Guid? PrincipalId { get; set; }

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
    /// Calculated full name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Customer's email address
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Customer status (Active/Inactive)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status => IsDeleted ? "Inactive" : "Active";

    /// <summary>
    /// Customer's mobile phone number
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// Extension number for reaching the customer via company landline
    /// </summary>
    [JsonPropertyName("extension")]
    public string? Extension { get; set; }

    /// <summary>
    /// Customer's personal or direct landline phone number
    /// </summary>
    [JsonPropertyName("landline")]
    public string? Landline { get; set; }

    /// <summary>
    /// Company name associated with the customer
    /// </summary>
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Company's landline phone number
    /// </summary>
    [JsonPropertyName("companyPhone")]
    public string? CompanyPhone { get; set; }

    /// <summary>
    /// Thai National ID (masked for security - only shows last 2 digits)
    /// Full ID is never exposed in API responses for PDPA compliance
    /// </summary>
    [JsonPropertyName("thaiNationalIdMasked")]
    public string? ThaiNationalIdMasked
    {
        get
        {
            if (string.IsNullOrEmpty(ThaiNationalId))
                return null;

            // Mask all but last 2 digits: ***-****-**XX
            return ThaiNationalId.Length >= 2
                ? $"***-****-**{ThaiNationalId.Substring(ThaiNationalId.Length - 2)}"
                : "***-****-****";
        }
    }

    /// <summary>
    /// Thai National ID (internal use only - never serialized to JSON)
    /// </summary>
    [JsonIgnore]
    public string? ThaiNationalId { get; set; }

    /// <summary>
    /// Status of the Non-Disclosure Agreement
    /// </summary>
    [JsonPropertyName("ndaStatus")]
    public string? NDAStatus { get; set; }

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
    public Dictionary<string, bool>? CommunicationPreferences { get; set; }

    /// <summary>
    /// Optional link to Company entity
    /// </summary>
    [JsonPropertyName("companyId")]
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Whether to use the company's billing address
    /// </summary>
    [JsonPropertyName("usesCompanyBillingAddress")]
    public bool UsesCompanyBillingAddress { get; set; }


    /// <summary>
    /// Soft delete flag
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// ID of the user who created this record
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Display name of the user who created this record
    /// </summary>
    [JsonPropertyName("createdByName")]
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Email of the user who created this record
    /// </summary>
    [JsonPropertyName("createdByEmail")]
    public string? CreatedByEmail { get; set; }

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
