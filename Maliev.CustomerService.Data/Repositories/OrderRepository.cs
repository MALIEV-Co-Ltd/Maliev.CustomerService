using Maliev.CustomerService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.CustomerService.Data.Repositories;

/// <summary>
/// Repository for order lookups
/// Note: This is a placeholder implementation. In production, this should:
/// 1. Query a local Order table/cache, OR
/// 2. Call an external Order service to get CompanyId by OrderId
///
/// The OrderPaidEvent should ideally include CompanyId to avoid this coupling.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly ILogger<OrderRepository> _logger;

    /// <summary>Initializes a new instance of <see cref="OrderRepository"/>.</summary>
    public OrderRepository(ILogger<OrderRepository> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Guid?> GetCompanyIdByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Order lookup not implemented. OrderId {OrderId} cannot be linked to a company. "
            + "Either update OrderPaidEvent to include CompanyId or implement order lookup.",
            orderId);

        return null;
    }
}
