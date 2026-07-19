namespace Maliev.CustomerService.Api.Models;

/// <summary>
/// Standard error response format for all API errors
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Machine-readable error code (e.g., "VALIDATION_ERROR", "NOT_FOUND")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details (e.g., validation errors, field-specific messages)
    /// </summary>
    public Dictionary<string, string[]>? Details { get; set; }

    /// <summary>
    /// Correlation ID for tracing the request across services
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the error occurred (ISO 8601 format)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
