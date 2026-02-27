using Maliev.CustomerService.Api.Services;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Customers;
using MassTransit;

namespace Maliev.CustomerService.Api.Consumers;

/// <summary>
/// Consumer for GetCustomerDetailsRequest to support async service-to-service communication.
/// </summary>
public class GetCustomerDetailsConsumer(
    ICustomerService customerService,
    ILogger<GetCustomerDetailsConsumer> logger) : IConsumer<GetCustomerDetailsRequest>
{
    private readonly ICustomerService _customerService = customerService;
    private readonly ILogger<GetCustomerDetailsConsumer> _logger = logger;

    /// <summary>
    /// Consumes the GetCustomerDetailsRequest message and responds with customer details.
    /// </summary>
    /// <param name="context">The consume context containing the request message.</param>
    /// <returns>A task that represents the asynchronous consume operation.</returns>
    public async Task Consume(ConsumeContext<GetCustomerDetailsRequest> context)
    {
        _logger.LogInformation("Processing GetCustomerDetailsRequest for CustomerId: {CustomerId}", context.Message.Payload.CustomerId);

        var customer = await _customerService.GetByIdAsync(context.Message.Payload.CustomerId);

        if (customer == null)
        {
            _logger.LogWarning("Customer not found for ID: {CustomerId}", context.Message.Payload.CustomerId);
            // Optionally respond with a Not Found or null payload if the contract supports it.
            // For now, we just don't respond or respond with an error if expected.
            // MassTransit Request/Response will timeout if no response is sent.
            // A better pattern is to respond with a CustomerNotFound event or a response with a null payload field if nullable.
            // Looking at the contract: CustomerDetailsResponsePayload fields are non-nullable strings.
            // We'll throw an exception to trigger a fault response.
            throw new KeyNotFoundException($"Customer with ID {context.Message.Payload.CustomerId} not found.");
        }

        await context.RespondAsync(new CustomerDetailsResponse(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(CustomerDetailsResponse),
            MessageType: MessageType.Response,
            MessageVersion: "1.0",
            PublishedBy: "Maliev.CustomerService",
            ConsumedBy: new[] { context.Message.PublishedBy },
            CorrelationId: context.Message.CorrelationId,
            CausationId: context.Message.MessageId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new CustomerDetailsResponsePayload(
                CustomerId: customer.Id,
                FullName: $"{customer.FirstName} {customer.LastName}",
                Email: customer.Email
            )
        ));
    }
}
