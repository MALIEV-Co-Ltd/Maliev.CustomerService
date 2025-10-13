using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Users;

/// <summary>
/// Response model for credential validation
/// </summary>
public class ValidationResponse
{
    /// <summary>
    /// Indicates if the credentials are valid
    /// </summary>
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    /// <summary>
    /// User ID (only returned if credentials are valid)
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Username (only returned if credentials are valid)
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// User roles (only returned if credentials are valid)
    /// </summary>
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
}
