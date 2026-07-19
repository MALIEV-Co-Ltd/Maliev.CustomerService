using Maliev.CustomerService.Api.Consumers;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Customers;
using Maliev.MessagingContracts.Contracts.Uploads;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class FileDeletedEventConsumerTests
{
    private readonly TestWebApplicationFactory _factory;

    public FileDeletedEventConsumerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FileDeletedEventConsumer_WithMatchingDocuments_UpdatesStatus()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();

        // Create document with matching file reference
        var fileId = "test-file-123";
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Maliev.CustomerService.Infrastructure.Persistence.CustomerDbContext>();

        context.DocumentReferences.Add(new DocumentReference
        {
            Id = Guid.NewGuid(),
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = fileId,
            Filename = "test.pdf",
            Status = DocumentStatus.Complete
        });
        await context.SaveChangesAsync();

        // Directly test the consumer
        var consumer = new FileDeletedEventConsumer(context, NullLogger<FileDeletedEventConsumer>.Instance);

        var message = new FileDeletedEvent
        {
            MessageId = Guid.NewGuid(),
            Payload = new FileDeletedEventPayload
            {
                FileId = fileId,
                StoragePath = "/storage/test.pdf",
                ServiceId = "customer-service"
            }
        };

        // Act
        await consumer.Consume(CreateConsumeContext(message));

        // Verify document status was updated
        var document = await context.DocumentReferences.FirstOrDefaultAsync(d => d.FileReference == fileId);
        Assert.NotNull(document);
        Assert.Equal(DocumentStatus.MissingFile, document.Status);
    }

    private static ConsumeContext<FileDeletedEvent> CreateConsumeContext(FileDeletedEvent message)
    {
        var mockContext = new Mock<ConsumeContext<FileDeletedEvent>>();
        mockContext.Setup(c => c.Message).Returns(message);
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return mockContext.Object;
    }
}
