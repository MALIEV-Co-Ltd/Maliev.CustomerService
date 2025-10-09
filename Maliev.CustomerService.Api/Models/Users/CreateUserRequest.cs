using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Users;

/// <summary>
/// Request model for creating a new user account
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Username for login
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [MaxLength(50)]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [MaxLength(255)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password (must meet complexity requirements)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Roles to assign to the user
    /// </summary>
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Optional link to Customer entity (for customer self-service accounts)
    /// </summary>
    [JsonPropertyName("linkedCustomerId")]
    public Guid? LinkedCustomerId { get; set; }
}
