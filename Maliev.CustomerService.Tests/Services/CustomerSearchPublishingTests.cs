using Maliev.CustomerService.Api.Consumers;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Maliev.MessagingContracts.Contracts.Search;
using Maliev.MessagingContracts.Contracts.Shared;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.CustomerService.Tests.Services;

/// <summary>
/// Tests proving CustomerService publishes centralized search events.
/// </summary>
[Collection("Database Collection")]
public class CustomerSearchPublishingTests
{
    private readonly TestWebApplicationFactory _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerSearchPublishingTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared database fixture.</param>
    public CustomerSearchPublishingTests(TestWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Creating a customer should publish a search upsert document.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithCustomer_PublishesSearchUpsert()
    {
        await _fixture.ClearDatabaseAsync();
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var service = CreateService(publishEndpoint);

        var result = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Kanya",
            LastName = "Larsson",
            Email = "kanya.search@example.com",
            Segment = "Enterprise",
            Tier = "VIP",
            PreferredLanguage = "th",
            Timezone = "Asia/Bangkok"
        }, "employee-1", "Employee");

        publishEndpoint.Verify(
            endpoint => endpoint.Publish(
                It.Is<SearchDocumentUpsertedEvent>(message =>
                    message.Payload.SourceService == "CustomerService" &&
                    message.Payload.ResourceType == "customer" &&
                    message.Payload.ResourceId == result.Id.ToString() &&
                    message.Payload.Title == "Kanya Larsson" &&
                    message.Payload.RequiredPermission == "customer.customers.read"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Soft deleting a customer should publish a search tombstone event.
    /// </summary>
    [Fact]
    public async Task SoftDeleteAsync_WithCustomer_PublishesSearchDelete()
    {
        await _fixture.ClearDatabaseAsync();
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var service = CreateService(publishEndpoint);
        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Delete",
            LastName = "Me",
            Email = "delete.search@example.com",
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = "en",
            Timezone = "UTC"
        }, "employee-1", "Employee");
        publishEndpoint.Invocations.Clear();

        var deleted = await service.SoftDeleteAsync(created.Id, created.xmin, "employee-1", "Employee");

        Assert.True(deleted);
        publishEndpoint.Verify(
            endpoint => endpoint.Publish(
                It.Is<SearchDocumentDeletedEvent>(message =>
                    message.Payload.SourceService == "CustomerService" &&
                    message.Payload.ResourceType == "customer" &&
                    message.Payload.ResourceId == created.Id.ToString()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Reindex requests should republish current active customers.
    /// </summary>
    [Fact]
    public async Task Consume_WithGlobalReindexRequest_PublishesActiveCustomerDocuments()
    {
        await _fixture.ClearDatabaseAsync();
        await using var dbContext = _fixture.CreateDbContext();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Wanasriwilai Engineering",
            VatNumber = "TH-0123456789",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            FirstName = "Kanya",
            LastName = "Larsson",
            Email = "seed.customer@example.com",
            Segment = "Enterprise",
            Tier = "VIP",
            PreferredLanguage = "th",
            Timezone = "Asia/Bangkok",
            CompanyId = company.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Companies.Add(company);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        var publishEndpoint = new Mock<IPublishEndpoint>();
        var logger = Mock.Of<ILogger<SearchReindexRequestedConsumer>>();
        var consumer = new SearchReindexRequestedConsumer(dbContext, publishEndpoint.Object, logger);
        var consumeContext = new Mock<ConsumeContext<SearchReindexRequestedCommand>>();
        consumeContext.SetupGet(context => context.Message).Returns(CreateReindexCommand(sourceService: null));
        consumeContext.SetupGet(context => context.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);

        publishEndpoint.Verify(
            endpoint => endpoint.Publish(
                It.Is<SearchDocumentUpsertedEvent>(message =>
                    message.Payload.ResourceId == customer.Id.ToString() &&
                    message.Payload.Title == "Kanya Larsson" &&
                    message.Payload.Subtitle != null &&
                    message.Payload.Subtitle.Contains("Wanasriwilai Engineering", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Reindex requests scoped to another service should be ignored.
    /// </summary>
    [Fact]
    public async Task Consume_WithOtherServiceReindexRequest_DoesNotPublishCustomerDocuments()
    {
        await _fixture.ClearDatabaseAsync();
        await using var dbContext = _fixture.CreateDbContext();
        dbContext.Customers.Add(new Customer
        {
            Id = Guid.NewGuid(),
            PrincipalId = Guid.NewGuid(),
            FirstName = "Kanya",
            LastName = "Larsson",
            Email = "seed.ignore@example.com",
            Segment = "Enterprise",
            Tier = "VIP",
            PreferredLanguage = "th",
            Timezone = "Asia/Bangkok",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var publishEndpoint = new Mock<IPublishEndpoint>();
        var logger = Mock.Of<ILogger<SearchReindexRequestedConsumer>>();
        var consumer = new SearchReindexRequestedConsumer(dbContext, publishEndpoint.Object, logger);
        var consumeContext = new Mock<ConsumeContext<SearchReindexRequestedCommand>>();
        consumeContext.SetupGet(context => context.Message).Returns(CreateReindexCommand("InvoiceService"));
        consumeContext.SetupGet(context => context.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);

        publishEndpoint.Verify(
            endpoint => endpoint.Publish(It.IsAny<SearchDocumentUpsertedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static SearchReindexRequestedCommand CreateReindexCommand(string? sourceService)
    {
        var occurredAtUtc = DateTimeOffset.UtcNow;
        return new SearchReindexRequestedCommand(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(SearchReindexRequestedCommand),
            MessageType: MessageType.Command,
            MessageVersion: "1.0.0",
            PublishedBy: "SearchService",
            ConsumedBy: ["CustomerService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: occurredAtUtc,
            IsPublic: false,
            Payload: new SearchReindexRequestedCommandPayload(
                SourceService: sourceService,
                RequestedBy: "test",
                RequestedAtUtc: occurredAtUtc));
    }

    private Api.Services.CustomerService CreateService(Mock<IPublishEndpoint> publishEndpoint)
    {
        return new Api.Services.CustomerService(
            _fixture.CreateDbContext(),
            CreateIamClient(),
            CreateConfiguration(),
            Mock.Of<ILogger<Api.Services.CustomerService>>(),
            new Mock<Api.Services.MetricsService>(MockBehavior.Loose, Mock.Of<IHostEnvironment>()).Object,
            publishEndpoint.Object);
    }

    private static IIAMClient CreateIamClient()
    {
        var iamClient = new Mock<IIAMClient>();
        iamClient.Setup(client => client.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new CreatePrincipalResponse
            {
                PrincipalId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            });
        return iamClient.Object;
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:PrincipalBasedAuthEnabled"] = "true"
            })
            .Build();
    }
}
