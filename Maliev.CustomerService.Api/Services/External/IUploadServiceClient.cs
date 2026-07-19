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
    /// <remarks>
    /// Returns false if the Upload Service is unavailable, the file is not found, or any error occurs
    /// </remarks>
    Task<bool> ValidateFileReferenceAsync(string fileReference);

    /// <summary>
    /// Deletes a file from the Upload Service
    /// </summary>
    /// <param name="fileReference">File reference ID to delete</param>
    /// <returns>True if deletion was successful or file was already deleted, false if deletion failed</returns>
    /// <remarks>
    /// Returns false if the Upload Service is unavailable or any error occurs
    /// </remarks>
    Task<bool> DeleteFileAsync(string fileReference);
}
