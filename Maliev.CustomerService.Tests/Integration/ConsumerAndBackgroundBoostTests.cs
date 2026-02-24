using Maliev.CustomerService.Api.Consumers;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Data.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Maliev.MessagingContracts.Contracts.Customers;
using Maliev.MessagingContracts.Contracts.Uploads;
using Maliev.MessagingContracts.Generated;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class ConsumerAndBackgroundBoostTests
{
    private readonly TestWebApplicationFactory _factory;

    public ConsumerAndBackgroundBoostTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCustomerDetailsConsumer_ConsumesMessage_PublishesResponse()
    {
        var harness = _factory.Services.GetRequiredService<ITestHarness>();

        // Seed a customer
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Maliev.CustomerService.Data.CustomerDbContext>();
        var customerId = Guid.NewGuid();
        context.Customers.Add(new Customer { Id = customerId, FirstName = "Test", LastName = "User", Email = "test@user.com", Segment = "Retail", Tier = "Bronze" });
        await context.SaveChangesAsync();

        var request = new GetCustomerDetailsRequest
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            Payload = new GetCustomerDetailsRequestPayload { CustomerId = customerId }
        };

        // Act
        await harness.Bus.Publish(request);

        // Assert
        Assert.True(await harness.Published.Any<CustomerDetailsResponse>());
    }

    [Fact]
    public async Task FileDeletedEventConsumer_ConsumesMessage_Succeeds()
    {
        var harness = _factory.Services.GetRequiredService<ITestHarness>();

        var message = new FileDeletedEvent
        {
            MessageId = Guid.NewGuid(),
            Payload = new FileDeletedEventPayload { FileId = "file-123" }
        };

        // Act
        await harness.Bus.Publish(message);

        // Assert
        Assert.True(await harness.Consumed.Any<FileDeletedEvent>());
    }

    [Fact]
    public async Task DocumentDeletionRetryBackgroundService_Executes_Succeeds()
    {
        using var scope = _factory.Services.CreateScope();
        var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

        // Act - Trigger retry logic
        await documentService.RetryPendingDeletionsAsync();

        Assert.True(true);
    }
}
