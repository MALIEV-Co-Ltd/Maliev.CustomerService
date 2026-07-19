using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Companies;

/// <summary>
/// Source of the company information
/// </summary>
public enum CompanySource
{
    /// <summary>Existing internal company</summary>
    Internal,
    /// <summary>External DBD registry</summary>
    Registry
}

/// <summary>
/// Result of a unified company search
/// </summary>
public sealed record CompanySearchResultDto
{
    /// <summary>Internal ID if existing</summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; init; }

    /// <summary>Company name</summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>VAT number / Tax ID</summary>
    [JsonPropertyName("vatNumber")]
    public string? VatNumber { get; init; }

    /// <summary>Segment</summary>
    [JsonPropertyName("segment")]
    public string? Segment { get; init; }

    /// <summary>Source of data</summary>
    [JsonPropertyName("source")]
    public CompanySource Source { get; init; }

    /// <summary>Billing address summary</summary>
    [JsonPropertyName("billingAddress")]
    public AddressSummaryDto? BillingAddress { get; init; }

    /// <summary>Registration number (Registry specific)</summary>
    [JsonPropertyName("registrationNumber")]
    public string? RegistrationNumber { get; init; }

    /// <summary>Business type (Registry specific)</summary>
    [JsonPropertyName("businessType")]
    public string? BusinessType { get; init; }
}
