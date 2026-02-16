using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Companies;

/// <summary>
/// Response model for company data
/// </summary>
public class CompanyResponse
{
    /// <summary>
    /// Unique identifier for the company
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Company name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// VAT number
    /// </summary>
    [JsonPropertyName("vatNumber")]
    public string? VatNumber { get; set; }

    /// <summary>
    /// Company registration number
    /// </summary>
    [JsonPropertyName("registrationNumber")]
    public string? RegistrationNumber { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    [JsonPropertyName("contactEmail")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    [JsonPropertyName("contactPhone")]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Company segment
    /// </summary>
    [JsonPropertyName("segment")]
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// Company tier
    /// </summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Full company name in Thai (from BDEX registry)
    /// </summary>
    [JsonPropertyName("fullNameTh")]
    public string? FullNameTh { get; set; }

    /// <summary>
    /// Company registration date
    /// </summary>
    [JsonPropertyName("registrationDate")]
    public DateTime? RegistrationDate { get; set; }

    /// <summary>
    /// Company status code
    /// </summary>
    [JsonPropertyName("companyStatus")]
    public string? CompanyStatus { get; set; }

    /// <summary>
    /// Company status description in Thai
    /// </summary>
    [JsonPropertyName("companyStatusNameTh")]
    public string? CompanyStatusNameTh { get; set; }

    /// <summary>
    /// Type of business entity code
    /// </summary>
    [JsonPropertyName("companyTypeCode")]
    public string? CompanyTypeCode { get; set; }

    /// <summary>
    /// Business objectives
    /// </summary>
    [JsonPropertyName("businessObjectives")]
    public string? BusinessObjectives { get; set; }

    /// <summary>
    /// Whether company data was verified from BDEX registry
    /// </summary>
    [JsonPropertyName("isVerifiedFromBdex")]
    public bool IsVerifiedFromBdex { get; set; }

    /// <summary>
    /// Date when company was verified from BDEX
    /// </summary>
    [JsonPropertyName("bdexVerificationDate")]
    public DateTime? BdexVerificationDate { get; set; }

    /// <summary>
    /// Stock symbol for publicly listed companies
    /// </summary>
    [JsonPropertyName("stockSymbol")]
    public string? StockSymbol { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
