namespace Maliev.CustomerService.Api.Configuration;

/// <summary>
/// Base configuration options for external service clients
/// </summary>
public abstract class ExternalServiceOptions
{
    /// <summary>
    /// Base URL for the external service
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds (default: 30 seconds)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for transient failures (default: 3)
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Whether to enable circuit breaker pattern (default: true)
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;
}

/// <summary>
/// Configuration options for Upload Service client
/// </summary>
public class UploadServiceOptions : ExternalServiceOptions
{
    /// <summary>
    /// Configuration section name in appsettings
    /// </summary>
    public const string SectionName = "ExternalServices:UploadService";

    /// <summary>
    /// Maximum file size in megabytes for upload validation (default: 50 MB)
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 50;

    /// <summary>
    /// Upload operation timeout in seconds (default: 300 seconds / 5 minutes)
    /// </summary>
    public new int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Configuration options for Country Service client
/// </summary>
public class CountryServiceOptions : ExternalServiceOptions
{
    /// <summary>
    /// Configuration section name in appsettings
    /// </summary>
    public const string SectionName = "ExternalServices:CountryService";

    /// <summary>
    /// Cache duration for valid country IDs in hours (default: 24 hours)
    /// </summary>
    public int CacheDurationHours { get; set; } = 24;
}
