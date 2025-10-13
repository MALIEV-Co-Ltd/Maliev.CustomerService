namespace Maliev.CustomerService.Api.Services.External;

/// <summary>
/// Client interface for Upload Service integration
/// </summary>
public interface IUploadServiceClient
{
    /// <summary>
    /// Validates that a file reference exists in the Upload Service
    /// </summary>
    /// <param name="fileReference">File reference ID from Upload Service</param>
    /// <returns>True if the file reference is valid, false otherwise</returns>
    Task<bool> ValidateFileReferenceAsync(string fileReference);

    /// <summary>
    /// Deletes a file from the Upload Service
    /// </summary>
    /// <param name="fileReference">File reference ID to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteFileAsync(string fileReference);
}
