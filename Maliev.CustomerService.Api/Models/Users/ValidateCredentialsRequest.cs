using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Users;

/// <summary>
/// Request model for credential validation (used by Auth Service)
/// </summary>
public class ValidateCredentialsRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
