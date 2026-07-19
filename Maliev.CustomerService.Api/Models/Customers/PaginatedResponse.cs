using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Generic paginated response wrapper
/// </summary>
public class PaginatedResponse<T>
{
    /// <summary>
    /// List of items in the current page
    /// </summary>
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Total count of items across all pages
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-indexed)
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}
