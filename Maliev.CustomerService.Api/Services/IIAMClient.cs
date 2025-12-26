using Maliev.CustomerService.Api.Models.IAM;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Interface for interacting with the Identity and Access Management (IAM) service
/// </summary>
public interface IIAMClient
{
    /// <summary>
    /// Creates a new principal in the IAM service
    /// </summary>
    /// <param name="request">Principal creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing the created PrincipalId</returns>
    Task<CreatePrincipalResponse> CreatePrincipalAsync(
        CreatePrincipalRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a principal from the IAM service (compensation action for failed transactions)
    /// </summary>
    /// <param name="principalId">The principal ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeletePrincipalAsync(
        Guid principalId,
        CancellationToken cancellationToken = default);
}
