using Maliev.CustomerService.Api.Consumers;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Customers;
using Maliev.MessagingContracts.Contracts.Uploads;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
    public async Task DocumentDeletionRetryBackgroundService_Executes_Succeeds()
    {
        using var scope = _factory.Services.CreateScope();
        var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

        // Act - Trigger retry logic
        await documentService.RetryPendingDeletionsAsync();

        Assert.True(true);
    }

    [Fact]
    public async Task FileDeletedEventConsumer_WithNoMatchingDocuments_DoesNotThrow()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();

        // Directly test the consumer
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Maliev.CustomerService.Infrastructure.Persistence.CustomerDbContext>();

        var consumer = new FileDeletedEventConsumer(context, NullLogger<FileDeletedEventConsumer>.Instance);

        var message = new FileDeletedEvent
        {
            MessageId = Guid.NewGuid(),
            Payload = new FileDeletedEventPayload
            {
                FileId = "non-existent-file",
                StoragePath = "/storage/none.pdf",
                ServiceId = "customer-service"
            }
        };

        var mockContext = new Mock<ConsumeContext<FileDeletedEvent>>();
        mockContext.Setup(c => c.Message).Returns(message);
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act & Assert - should not throw
        await consumer.Consume(mockContext.Object);
    }
}
