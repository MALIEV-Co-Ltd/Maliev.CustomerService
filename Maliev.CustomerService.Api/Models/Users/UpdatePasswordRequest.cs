using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Users;

/// <summary>
/// Request model for updating a user's password
/// </summary>
public class UpdatePasswordRequest
{
    /// <summary>
    /// Current password for verification
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password (must meet complexity requirements)
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;
}
