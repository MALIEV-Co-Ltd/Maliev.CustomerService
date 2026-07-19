namespace Maliev.CustomerService.Api.Models.IAM;

/// <summary>
/// Response model for principal creation in IAM
/// </summary>
public record CreatePrincipalResponse
{
    /// <summary>
    /// Unique principal identifier from IAM
    /// </summary>
    public Guid PrincipalId { get; init; }

    /// <summary>
    /// Timestamp when the principal was created
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
