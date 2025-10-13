using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Companies;

/// <summary>
/// Request model for creating a new company
/// </summary>
public class CreateCompanyRequest
{
    /// <summary>
    /// Company name
    /// </summary>
    [Required(ErrorMessage = "Company name is required")]
    [MaxLength(255)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// VAT number with country prefix (e.g., "TH-1234567890")
    /// </summary>
    [MaxLength(50)]
    [JsonPropertyName("vatNumber")]
    public string? VatNumber { get; set; }

    /// <summary>
    /// Company registration number
    /// </summary>
    [MaxLength(100)]
    [JsonPropertyName("registrationNumber")]
    public string? RegistrationNumber { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    [EmailAddress]
    [MaxLength(255)]
    [JsonPropertyName("contactEmail")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact phone number (E.164 format)
    /// </summary>
    [MaxLength(20)]
    [JsonPropertyName("contactPhone")]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Company segment (Retail, Wholesale, Enterprise, Government)
    /// </summary>
    [Required(ErrorMessage = "Segment is required")]
    [JsonPropertyName("segment")]
    public string Segment { get; set; } = "Retail";

    /// <summary>
    /// Company tier (Bronze, Silver, Gold, Platinum, VIP)
    /// </summary>
    [Required(ErrorMessage = "Tier is required")]
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "Bronze";
}
