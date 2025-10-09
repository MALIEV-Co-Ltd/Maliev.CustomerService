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
