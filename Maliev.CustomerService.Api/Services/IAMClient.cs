using Maliev.CustomerService.Api.Models.IAM;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Implementation of the IAM client using HttpClient
/// </summary>
public class IAMClient : IIAMClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IAMClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IAMClient"/> class
    /// </summary>
    /// <param name="httpClient">Configured HttpClient</param>
    /// <param name="logger">Logger instance</param>
    public IAMClient(HttpClient httpClient, ILogger<IAMClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CreatePrincipalResponse> CreatePrincipalAsync(
        CreatePrincipalRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating IAM principal for email {Email}", request.Email);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/iam/v1/principals", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("IAM service returned error: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"IAM service returned {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<CreatePrincipalResponse>(
                cancellationToken: cancellationToken);

            if (result == null)
            {
                _logger.LogError("IAM service returned empty response for email {Email}", request.Email);
                throw new InvalidOperationException("IAM service returned an empty response.");
            }

            _logger.LogInformation("Successfully created IAM principal {PrincipalId} for email {Email}",
                result.PrincipalId, request.Email);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while creating IAM principal for email {Email}", request.Email);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating IAM principal for email {Email}", request.Email);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CreatePrincipalResponse?> GetPrincipalByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving IAM principal for email {Email}", email);

        try
        {
            var response = await _httpClient.GetAsync($"/iam/v1/principals/by-email/{Uri.EscapeDataString(email)}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("IAM service returned error while getting principal by email: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CreatePrincipalResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving IAM principal for email {Email}", email);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task DeletePrincipalAsync(
        Guid principalId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Deleting IAM principal {PrincipalId} as compensation for failed transaction", principalId);

        try
        {
            var response = await _httpClient.DeleteAsync($"/iam/v1/principals/{principalId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted IAM principal {PrincipalId} as compensation", principalId);
            }
            else
            {
                _logger.LogError("Failed to delete IAM principal {PrincipalId} during compensation. Status: {StatusCode}. Manual cleanup may be required.",
                    principalId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting IAM principal {PrincipalId} during compensation. Manual cleanup may be required.", principalId);
            // Don't re-throw - compensation is best-effort
        }
    }
}
