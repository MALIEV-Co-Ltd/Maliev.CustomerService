using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Users;

/// <summary>
/// Response model for user data
/// </summary>
public class UserResponse
{
    /// <summary>
    /// User ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User roles
    /// </summary>
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Linked customer ID (for customer self-service accounts)
    /// </summary>
    [JsonPropertyName("linkedCustomerId")]
    public Guid? LinkedCustomerId { get; set; }

    /// <summary>
    /// Indicates if the user account is active
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Last successful login timestamp
    /// </summary>
    [JsonPropertyName("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last account update timestamp
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
