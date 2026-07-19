using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for durable customer-scoped memories.
/// </summary>
public class CustomerMemoryService : ICustomerMemoryService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<CustomerMemoryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerMemoryService"/> class.
    /// </summary>
    /// <param name="context">Customer database context.</param>
    /// <param name="logger">Logger instance.</param>
    public CustomerMemoryService(CustomerDbContext context, ILogger<CustomerMemoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CustomerMemoryQueryResponse> GetAsync(
        Guid customerId,
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        await EnsureCustomerExistsAsync(customerId, cancellationToken);

        var normalizedQuery = NormalizeOptional(query);
        var normalizedLimit = Math.Clamp(limit, 1, 25);
        var memoriesQuery = _context.CustomerMemories
            .AsNoTracking()
            .Where(memory => memory.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            var pattern = $"%{normalizedQuery}%";
            memoriesQuery = memoriesQuery.Where(memory =>
                EF.Functions.ILike(memory.MemoryType, pattern) ||
                EF.Functions.ILike(memory.Key, pattern) ||
                EF.Functions.ILike(memory.Value, pattern) ||
                EF.Functions.ILike(memory.Source, pattern));
        }

        var items = await memoriesQuery
            .OrderByDescending(memory => memory.Confidence)
            .ThenByDescending(memory => memory.HitCount)
            .ThenByDescending(memory => memory.LastObservedAt)
            .Take(normalizedLimit)
            .Select(memory => Map(memory))
            .ToListAsync(cancellationToken);

        return new CustomerMemoryQueryResponse
        {
            CustomerId = customerId,
            Query = normalizedQuery,
            Limit = normalizedLimit,
            Items = items
        };
    }

    /// <inheritdoc />
    public async Task<CustomerMemoryResponse> ObserveAsync(
        Guid customerId,
        CustomerMemoryObserveRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCustomerExistsAsync(customerId, cancellationToken);

        var now = DateTime.UtcNow;
        var memoryType = NormalizeRequired(request.MemoryType);
        var key = NormalizeRequired(request.Key);
        var source = NormalizeRequired(request.Source);
        var value = request.Value.Trim();
        var confidence = Math.Clamp(request.Confidence, 0m, 1m);

        var memory = await _context.CustomerMemories
            .SingleOrDefaultAsync(item =>
                item.CustomerId == customerId &&
                item.MemoryType == memoryType &&
                item.Key == key,
                cancellationToken);

        if (memory is null)
        {
            memory = new CustomerMemory
            {
                CustomerId = customerId,
                MemoryType = memoryType,
                Key = key,
                Value = value,
                Confidence = confidence,
                Source = source,
                HitCount = 1,
                CreatedAt = now,
                UpdatedAt = now,
                LastObservedAt = now
            };
            _context.CustomerMemories.Add(memory);
        }
        else
        {
            memory.Value = value;
            memory.Confidence = confidence;
            memory.Source = source;
            memory.HitCount++;
            memory.UpdatedAt = now;
            memory.LastObservedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Observed customer memory {MemoryType}/{MemoryKey} for customer {CustomerId}.",
            memory.MemoryType,
            memory.Key,
            customerId);

        return Map(memory);
    }

    private async Task EnsureCustomerExistsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var exists = await _context.Customers
            .AsNoTracking()
            .AnyAsync(customer => customer.Id == customerId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException($"Customer {customerId:D} was not found.");
        }
    }

    private static CustomerMemoryResponse Map(CustomerMemory memory)
    {
        return new CustomerMemoryResponse
        {
            Id = memory.Id,
            CustomerId = memory.CustomerId,
            MemoryType = memory.MemoryType,
            Key = memory.Key,
            Value = memory.Value,
            Confidence = memory.Confidence,
            Source = memory.Source,
            HitCount = memory.HitCount,
            LastObservedAt = memory.LastObservedAt,
            CreatedAt = memory.CreatedAt,
            UpdatedAt = memory.UpdatedAt
        };
    }

    private static string NormalizeRequired(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : value.Trim();
}
