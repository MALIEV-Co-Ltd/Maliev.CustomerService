using Maliev.CustomerService.Api.Search;
using Maliev.CustomerService.Infrastructure.Persistence;
using Maliev.MessagingContracts.Contracts.Search;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Consumers;

/// <summary>
/// Republishes customer search documents when SearchService requests a reindex.
/// </summary>
public class SearchReindexRequestedConsumer : IConsumer<SearchReindexRequestedCommand>
{
    private const string SourceService = "CustomerService";
    private readonly CustomerDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SearchReindexRequestedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchReindexRequestedConsumer"/> class.
    /// </summary>
    /// <param name="context">Customer database context.</param>
    /// <param name="publishEndpoint">MassTransit publish endpoint.</param>
    /// <param name="logger">Logger instance.</param>
    public SearchReindexRequestedConsumer(
        CustomerDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<SearchReindexRequestedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SearchReindexRequestedCommand> context)
    {
        var payload = context.Message.Payload;
        if (payload is null)
        {
            _logger.LogWarning("Ignoring SearchReindexRequestedCommand without payload");
            return;
        }

        if (!ShouldHandle(payload.SourceService))
        {
            return;
        }

        var customers = await _context.Customers
            .AsNoTracking()
            .Where(customer => !customer.IsDeleted)
            .ToListAsync(context.CancellationToken);

        var companyIds = customers
            .Where(customer => customer.CompanyId.HasValue)
            .Select(customer => customer.CompanyId!.Value)
            .Distinct()
            .ToArray();

        var companies = companyIds.Length == 0
            ? []
            : await _context.Companies
                .AsNoTracking()
                .Where(company => companyIds.Contains(company.Id))
                .ToDictionaryAsync(company => company.Id, context.CancellationToken);

        var occurredAtUtc = DateTimeOffset.UtcNow;
        foreach (var customer in customers)
        {
            companies.TryGetValue(customer.CompanyId ?? Guid.Empty, out var company);
            await _publishEndpoint.Publish(
                CustomerSearchDocumentMapper.ToUpsertEvent(customer, company, occurredAtUtc),
                context.CancellationToken);
        }

        _logger.LogInformation("Republished {Count} customer search documents", customers.Count);
    }

    private static bool ShouldHandle(string? sourceService)
    {
        return string.IsNullOrWhiteSpace(sourceService) ||
            string.Equals(sourceService, SourceService, StringComparison.OrdinalIgnoreCase);
    }
}
