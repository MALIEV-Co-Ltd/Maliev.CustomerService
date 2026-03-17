using Maliev.CustomerService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maliev.CustomerService.Application.Services;

/// <summary>
/// Scoped processor for year-end tier demotion and YTD reset.
/// Injecting this into the singleton BackgroundService via IServiceScopeFactory
/// is intentional — BackgroundService is a singleton and cannot directly receive
/// scoped dependencies (EF Core DbContext). The scope is created per job execution.
/// </summary>
public class YearEndTierProcessor : IYearEndTierProcessor
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITierCalculationService _tierCalculationService;
    private readonly ILogger<YearEndTierProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="YearEndTierProcessor"/> class.
    /// </summary>
    /// <param name="companyRepository">Repository for company data access.</param>
    /// <param name="tierCalculationService">Service for tier calculation operations.</param>
    /// <param name="logger">Logger for recording processor operations.</param>
    public YearEndTierProcessor(
        ICompanyRepository companyRepository,
        ITierCalculationService tierCalculationService,
        ILogger<YearEndTierProcessor> logger)
    {
        _companyRepository = companyRepository;
        _tierCalculationService = tierCalculationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting year-end tier demotion processing");

        var demotedCount = await _tierCalculationService.ApplyYearEndDemotionsAsync(cancellationToken);
        await _tierCalculationService.ResetYearlyValuesAsync(cancellationToken);

        _logger.LogInformation("Year-end processing completed. Demoted {Count} companies", demotedCount);
        return demotedCount;
    }
}
