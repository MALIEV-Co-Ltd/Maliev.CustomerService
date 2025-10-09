namespace Maliev.CustomerService.Api.Services.External;

/// <summary>
/// Client interface for validating country IDs via the Country Service
/// </summary>
public interface ICountryServiceClient
{
    /// <summary>
    /// Validates if a country ID exists in the Country Service
    /// </summary>
    /// <param name="countryId">Country ID to validate</param>
    /// <returns>True if the country ID is valid, false otherwise</returns>
    Task<bool> ValidateCountryIdAsync(Guid countryId);
}
