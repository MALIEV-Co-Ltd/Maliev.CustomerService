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
    private readonly IOrderRepository _orderRepository;
    private readonly ITierCalculationService _tierCalculationService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderPaidEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of OrderPaidEventConsumer
    /// </summary>
    public OrderPaidEventConsumer(
        ICompanyRepository companyRepository,
        IOrderRepository orderRepository,
        ITierCalculationService tierCalculationService,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderPaidEventConsumer> logger)
    {
        _companyRepository = companyRepository;
        _orderRepository = orderRepository;
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
            var companyId = await _orderRepository.GetCompanyIdByOrderIdAsync(
                message.Payload.OrderId, context.CancellationToken);

            if (companyId is null)
            {
                _logger.LogWarning(
                    "Could not find company for OrderId {OrderId}. Skipping tier calculation.",
                    message.Payload.OrderId);
                return;
            }

            var company = await _companyRepository.GetByIdAsync(companyId.Value, context.CancellationToken);
            if (company is null)
            {
                _logger.LogWarning(
                    "Company {CompanyId} not found for OrderId {OrderId}. Skipping tier calculation.",
                    companyId, message.Payload.OrderId);
                return;
            }

            var previousTier = company.Tier;

            company.CurrentYearPurchaseValue += (decimal)message.Payload.PaidAmount;
            company.CurrentYearOrderCount += 1;

            await _companyRepository.UpdateAsync(company, context.CancellationToken);

            var tierChanged = await _tierCalculationService.ApplyTierAsync(companyId.Value, context.CancellationToken);

            if (tierChanged)
            {
                var updatedCompany = await _companyRepository.GetByIdAsync(companyId.Value, context.CancellationToken);

                var tierChangedEvent = new CompanyTierChangedEvent
                {
                    CompanyId = companyId.Value,
                    PreviousTier = previousTier,
                    NewTier = updatedCompany?.Tier ?? previousTier,
                    CurrentYearPurchaseValue = updatedCompany?.CurrentYearPurchaseValue ?? company.CurrentYearPurchaseValue,
                    CurrentYearOrderCount = updatedCompany?.CurrentYearOrderCount ?? company.CurrentYearOrderCount,
                    Timestamp = DateTime.UtcNow
                };

                await _publishEndpoint.Publish(tierChangedEvent);

                _logger.LogInformation(
                    "Company {CompanyId} tier changed from {PreviousTier} to {NewTier}",
                    companyId, previousTier, tierChangedEvent.NewTier);
            }

            _logger.LogInformation(
                "Updated company {CompanyId} YTD: {YtdValue}, {YtdCount} orders",
                companyId, company.CurrentYearPurchaseValue, company.CurrentYearOrderCount);
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
