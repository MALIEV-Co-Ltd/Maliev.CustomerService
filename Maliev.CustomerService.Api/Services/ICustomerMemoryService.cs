using Maliev.CustomerService.Api.Models.Customers;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for durable customer-scoped memories.
/// </summary>
public interface ICustomerMemoryService
{
    /// <summary>
    /// Gets memories owned by a customer, optionally filtered by query text.
    /// </summary>
    /// <param name="customerId">Owning customer ID.</param>
    /// <param name="query">Optional search query.</param>
    /// <param name="limit">Maximum result count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching customer memories.</returns>
    Task<CustomerMemoryQueryResponse> GetAsync(Guid customerId, string? query, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Observes or reinforces a memory for the customer.
    /// </summary>
    /// <param name="customerId">Owning customer ID.</param>
    /// <param name="request">Memory observation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inserted or updated memory.</returns>
    Task<CustomerMemoryResponse> ObserveAsync(Guid customerId, CustomerMemoryObserveRequest request, CancellationToken cancellationToken = default);
}
