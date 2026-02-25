namespace Maliev.CustomerService.Application.Services;

/// <summary>
/// Processor interface for year-end tier operations.
/// Registered as scoped so it can safely use EF Core DbContext.
/// </summary>
public interface IYearEndTierProcessor
{
    /// <summary>
    /// Applies year-end demotions and resets yearly values for all companies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of companies demoted</returns>
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}
