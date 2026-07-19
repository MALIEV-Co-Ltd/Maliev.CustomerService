using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Companies;

/// <summary>
/// Lightweight address summary for search results
/// </summary>
public sealed record AddressSummaryDto
{
    /// <summary>Whether this is the default address</summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; init; }

    /// <summary>Address line 1</summary>
    [JsonPropertyName("addressLine1")]
    public string AddressLine1 { get; init; } = string.Empty;

    /// <summary>Address line 2</summary>
    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; init; }

    /// <summary>Address line 3</summary>
    [JsonPropertyName("addressLine3")]
    public string? AddressLine3 { get; init; }

    /// <summary>District</summary>
    [JsonPropertyName("district")]
    public string? District { get; init; }

    /// <summary>City</summary>
    [JsonPropertyName("city")]
    public string City { get; init; } = string.Empty;

    /// <summary>State or Province</summary>
    [JsonPropertyName("stateProvince")]
    public string StateProvince { get; init; } = string.Empty;

    /// <summary>Postal code</summary>
    [JsonPropertyName("postalCode")]
    public string PostalCode { get; init; } = string.Empty;

    /// <summary>Country name</summary>
    [JsonPropertyName("countryName")]
    public string? CountryName { get; init; }
}
