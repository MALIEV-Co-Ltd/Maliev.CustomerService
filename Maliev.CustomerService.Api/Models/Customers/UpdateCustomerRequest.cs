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
    /// Customer's mobile phone number (optional, E.164 format)
    /// </summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [MaxLength(20, ErrorMessage = "Mobile must not exceed 20 characters")]
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// Extension number for reaching the customer via company landline (optional)
    /// </summary>
    [MaxLength(10, ErrorMessage = "Extension must not exceed 10 characters")]
    [JsonPropertyName("extension")]
    public string? Extension { get; set; }

    /// <summary>
    /// Customer's personal or direct landline phone number (optional, E.164 format)
    /// </summary>
    [RegularExpression(@"^$|^[+]?[0-9\s-]{7,20}$", ErrorMessage = "Invalid phone format")]
    [MaxLength(20, ErrorMessage = "Landline must not exceed 20 characters")]
    [JsonPropertyName("landline")]
    public string? Landline { get; set; }

    /// <summary>
    /// Thai National ID (13 digits) - Optional, encrypted at rest for PDPA compliance
    /// </summary>
    [MaxLength(13, ErrorMessage = "Thai National ID must be exactly 13 digits")]
    [RegularExpression(@"^\d{13}$", ErrorMessage = "Thai National ID must be exactly 13 digits")]
    [JsonPropertyName("thaiNationalId")]
    public string? ThaiNationalId { get; set; }

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
    /// Payment terms for this customer.
    /// </summary>
    [MaxLength(100, ErrorMessage = "Payment terms must not exceed 100 characters")]
    [JsonPropertyName("paymentTerms")]
    public string? PaymentTerms { get; set; }

    /// <summary>
    /// Optional link to Company entity
    /// </summary>
    [JsonPropertyName("companyId")]
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Optional EmployeeService employee ID for the internal account manager.
    /// </summary>
    [JsonPropertyName("accountManagerEmployeeId")]
    public Guid? AccountManagerEmployeeId { get; set; }

    /// <summary>
    /// Clears the current account manager assignment when set to true.
    /// </summary>
    [JsonPropertyName("clearAccountManager")]
    public bool ClearAccountManager { get; set; }

    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control (required)
    /// </summary>
    [Required(ErrorMessage = "xmin is required for updates")]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
