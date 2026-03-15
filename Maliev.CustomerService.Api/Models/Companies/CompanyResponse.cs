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
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }

    /// <summary>
    /// Primary contact for this company (customer with IsPrimaryContact = true),
    /// or the first associated customer if none is explicitly set.
    /// </summary>
    [JsonPropertyName("primaryContact")]
    public CompanyPrimaryContactDto? PrimaryContact { get; set; }
}

/// <summary>
/// Lightweight primary contact info embedded in CompanyResponse.
/// </summary>
public class CompanyPrimaryContactDto
{
    /// <summary>Customer ID.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>Customer full name.</summary>
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Customer email address.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>Customer mobile number.</summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>Whether this customer is explicitly set as primary contact (vs inferred).</summary>
    [JsonPropertyName("isPrimaryContact")]
    public bool IsPrimaryContact { get; set; }
}
