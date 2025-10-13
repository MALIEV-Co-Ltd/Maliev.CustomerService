using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Users;

/// <summary>
/// Request model for updating a user's roles
/// </summary>
public class UpdateRolesRequest
{
    /// <summary>
    /// New roles to assign to the user (replaces existing roles)
    /// </summary>
    [Required(ErrorMessage = "Roles are required")]
    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
}
