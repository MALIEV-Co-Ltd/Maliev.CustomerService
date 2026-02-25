using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Application.Services;
using Maliev.MessagingContracts.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Maliev.CustomerService.Api.Consumers;

/// <summary>
/// Consumer for OrderPaidEvent - updates company tier when orders are paid
/// </summary>
public class OrderPaidEventConsumer : IConsumer<OrderPaidEvent>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITierCalculationService _tierCalculationService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderPaidEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of OrderPaidEventConsumer
    /// </summary>
    public OrderPaidEventConsumer(
        ICompanyRepository companyRepository,
        ITierCalculationService tierCalculationService,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderPaidEventConsumer> logger)
    {
        _companyRepository = companyRepository;
        _tierCalculationService = tierCalculationService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the OrderPaidEvent message
    /// </summary>
    public async Task Consume(ConsumeContext<OrderPaidEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processing OrderPaidEvent for OrderId {OrderId}, Total {Total}",
            message.Payload.OrderId, message.Payload.PaidAmount);

        try
        {
            // Note: OrderPaidEvent doesn't contain CompanyId directly
            // We would need to look up the order to get the CompanyId
            // For now, this is a placeholder - in production, you'd query an Order service
            // or include CompanyId in the event

            _logger.LogWarning(
                "OrderPaidEvent processing requires CompanyId lookup - not implemented yet. OrderId: {OrderId}",
                message.Payload.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing OrderPaidEvent for Order {OrderId}",
                message.Payload.OrderId);
            throw;
        }
    }
}

/// <summary>
/// Event published when a company's tier changes
/// </summary>
public class CompanyTierChangedEvent
{
    /// <summary>Company ID</summary>
    public Guid CompanyId { get; set; }
    /// <summary>Previous tier</summary>
    public string PreviousTier { get; set; } = string.Empty;
    /// <summary>New tier</summary>
    public string NewTier { get; set; } = string.Empty;
    /// <summary>Current YTD purchase value</summary>
    public decimal CurrentYearPurchaseValue { get; set; }
    /// <summary>Current YTD order count</summary>
    public int CurrentYearOrderCount { get; set; }
    /// <summary>Timestamp when tier changed</summary>
    public DateTime Timestamp { get; set; }
}
