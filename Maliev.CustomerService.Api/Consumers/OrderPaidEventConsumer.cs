using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Application.Services;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Customers;
using Maliev.MessagingContracts.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Maliev.CustomerService.Api.Consumers;

/// <summary>
/// Consumer for OrderPaidEvent - updates company YTD values and recalculates tier when orders are paid
/// </summary>
public class OrderPaidEventConsumer : IConsumer<OrderPaidEvent>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ITierCalculationService _tierCalculationService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderPaidEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="OrderPaidEventConsumer"/>
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

                var payload = new CompanyTierChangedEventPayload(
                    CompanyId: companyId.Value,
                    PreviousTier: previousTier,
                    NewTier: updatedCompany?.Tier ?? previousTier,
                    CurrentYearPurchaseValue: (double)(updatedCompany?.CurrentYearPurchaseValue ?? company.CurrentYearPurchaseValue),
                    CurrentYearOrderCount: updatedCompany?.CurrentYearOrderCount ?? company.CurrentYearOrderCount,
                    OccurredAt: DateTimeOffset.UtcNow);

                var tierChangedEvent = new CompanyTierChangedEvent(
                    MessageId: Guid.NewGuid(),
                    MessageName: nameof(CompanyTierChangedEvent),
                    MessageType: MessageType.Event,
                    MessageVersion: "1.0",
                    PublishedBy: "CustomerService",
                    ConsumedBy: Array.Empty<string>(),
                    CorrelationId: message.CorrelationId,
                    CausationId: message.MessageId,
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    IsPublic: true,
                    Payload: payload);

                await _publishEndpoint.Publish(tierChangedEvent, context.CancellationToken);

                _logger.LogInformation(
                    "Company {CompanyId} tier changed from {PreviousTier} to {NewTier}",
                    companyId, previousTier, payload.NewTier);
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
