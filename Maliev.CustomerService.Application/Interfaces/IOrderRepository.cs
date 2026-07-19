namespace Maliev.CustomerService.Application.Interfaces;

/// <summary>
/// Repository interface for Order entity lookups
/// Used to link orders to companies for tier calculation
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Gets the CompanyId associated with an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The CompanyId if found, otherwise null</returns>
    Task<Guid?> GetCompanyIdByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
