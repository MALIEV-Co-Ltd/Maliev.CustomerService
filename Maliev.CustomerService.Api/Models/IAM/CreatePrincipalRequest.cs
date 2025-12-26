namespace Maliev.CustomerService.Api.Models.IAM;

/// <summary>
/// Request model for creating a principal in IAM
/// </summary>
public record CreatePrincipalRequest
{
    /// <summary>
    /// Type of principal (e.g., "user", "service")
    /// </summary>
    public string PrincipalType { get; init; } = "user";

    /// <summary>
    /// Service that owns this principal link
    /// </summary>
    public string LinkedService { get; init; } = "CustomerService";

    /// <summary>
    /// Email associated with the principal
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Display name for the principal
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
}
