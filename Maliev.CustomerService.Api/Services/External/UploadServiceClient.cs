using Maliev.CustomerService.Api.Configuration;
using Microsoft.Extensions.Options;
using System.Net;

namespace Maliev.CustomerService.Api.Services.External;

/// <summary>
/// Client for Upload Service integration with resilient HTTP communication
/// </summary>
public class UploadServiceClient : IUploadServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UploadServiceClient> _logger;

    public UploadServiceClient(HttpClient httpClient, IOptions<UploadServiceOptions> options, ILogger<UploadServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // HttpClient is configured in Program.cs with base address and timeout
    }

    /// <summary>
    /// Validates that a file reference exists in the Upload Service
    /// </summary>
    public async Task<bool> ValidateFileReferenceAsync(string fileReference)
    {
        try
        {
            _logger.LogDebug("Validating file reference {FileReference} with Upload Service", fileReference);

            var response = await _httpClient.GetAsync($"/api/v1/files/{fileReference}/validate");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("File reference {FileReference} is valid", fileReference);
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File reference {FileReference} not found in Upload Service", fileReference);
                return false;
            }

            _logger.LogWarning("Upload Service validation failed for {FileReference} with status {StatusCode}",
                fileReference, response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error validating file reference {FileReference} with Upload Service", fileReference);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout validating file reference {FileReference} with Upload Service", fileReference);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating file reference {FileReference} with Upload Service", fileReference);
            return false;
        }
    }

    /// <summary>
    /// Deletes a file from the Upload Service
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileReference)
    {
        try
        {
            _logger.LogInformation("Deleting file reference {FileReference} from Upload Service", fileReference);

            var response = await _httpClient.DeleteAsync($"/api/v1/files/{fileReference}");

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("File reference {FileReference} deleted successfully (or already deleted)", fileReference);
                return true;
            }

            _logger.LogWarning("Upload Service deletion failed for {FileReference} with status {StatusCode}",
                fileReference, response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting file reference {FileReference} from Upload Service", fileReference);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout deleting file reference {FileReference} from Upload Service", fileReference);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting file reference {FileReference} from Upload Service", fileReference);
            return false;
        }
    }
}
