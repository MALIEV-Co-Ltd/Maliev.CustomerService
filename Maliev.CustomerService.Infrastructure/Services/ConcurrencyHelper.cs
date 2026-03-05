using Maliev.CustomerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Infrastructure.Services;

/// <summary>
/// Helper interface for checking entity concurrency using PostgreSQL xmin
/// </summary>
public interface IConcurrencyHelper
{
    /// <summary>
    /// Gets the current xmin value for an entity
    /// </summary>
    Task<uint> GetCurrentXminAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks if an entity has changed since the cached xmin value
    /// </summary>
    Task<bool> HasChangedAsync<T>(Guid id, uint cachedXmin, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Implementation of concurrency helper using PostgreSQL xmin
/// </summary>
public class ConcurrencyHelper : IConcurrencyHelper
{
    private readonly CustomerDbContext _context;

    /// <summary>
    /// Initializes a new instance of the ConcurrencyHelper class
    /// </summary>
    /// <param name="context">Database context</param>
    public ConcurrencyHelper(CustomerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<uint> GetCurrentXminAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : class
    {
        var entity = await _context.Set<T>().FindAsync([id], cancellationToken);
        if (entity == null)
        {
            return 0;
        }

        var xminProperty = typeof(T).GetProperty("xmin");
        if (xminProperty == null)
        {
            return 0;
        }

        return (uint)xminProperty.GetValue(entity)!;
    }

    /// <inheritdoc />
    public async Task<bool> HasChangedAsync<T>(Guid id, uint cachedXmin, CancellationToken cancellationToken = default) where T : class
    {
        var currentXmin = await GetCurrentXminAsync<T>(id, cancellationToken);
        return currentXmin != cachedXmin;
    }
}
