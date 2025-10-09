using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Companies;

/// <summary>
/// Request model for updating an existing company
/// </summary>
public class UpdateCompanyRequest
{
    /// <summary>
    /// Company name
    /// </summary>
    [MaxLength(255)]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// VAT number with country prefix
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
    /// Company segment
    /// </summary>
    [JsonPropertyName("segment")]
    public string? Segment { get; set; }

    /// <summary>
    /// Company tier
    /// </summary>
    [JsonPropertyName("tier")]
    public string? Tier { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Required(ErrorMessage = "Version is required for updates")]
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
