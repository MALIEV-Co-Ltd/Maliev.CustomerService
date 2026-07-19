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
    /// Full company name in Thai (from BDEX registry)
    /// </summary>
    [MaxLength(500)]
    [JsonPropertyName("fullNameTh")]
    public string? FullNameTh { get; set; }

    /// <summary>
    /// Company registration date
    /// </summary>
    [JsonPropertyName("registrationDate")]
    public DateTime? RegistrationDate { get; set; }

    /// <summary>
    /// Company status code (1=Active, 5=Liquidated, 8=Vacant)
    /// </summary>
    [MaxLength(10)]
    [JsonPropertyName("companyStatus")]
    public string? CompanyStatus { get; set; }

    /// <summary>
    /// Company status description in Thai
    /// </summary>
    [MaxLength(100)]
    [JsonPropertyName("companyStatusNameTh")]
    public string? CompanyStatusNameTh { get; set; }

    /// <summary>
    /// Type of business entity code
    /// </summary>
    [MaxLength(10)]
    [JsonPropertyName("companyTypeCode")]
    public string? CompanyTypeCode { get; set; }

    /// <summary>
    /// Business objectives (semicolon-separated)
    /// </summary>
    [JsonPropertyName("businessObjectives")]
    public string? BusinessObjectives { get; set; }

    /// <summary>
    /// Whether company data was verified from BDEX registry
    /// </summary>
    [JsonPropertyName("isVerifiedFromBdex")]
    public bool? IsVerifiedFromBdex { get; set; }

    /// <summary>
    /// Date when company was verified from BDEX
    /// </summary>
    [JsonPropertyName("bdexVerificationDate")]
    public DateTime? BdexVerificationDate { get; set; }

    /// <summary>
    /// Stock symbol for publicly listed companies
    /// </summary>
    [MaxLength(20)]
    [JsonPropertyName("stockSymbol")]
    public string? StockSymbol { get; set; }

    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [Required(ErrorMessage = "xmin is required for updates")]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
