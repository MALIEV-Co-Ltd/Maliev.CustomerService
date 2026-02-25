using Maliev.CustomerService.Application.Interfaces;
using Maliev.CustomerService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maliev.CustomerService.Application.BackgroundServices;

/// <summary>
/// Background service for year-end tier demotion processing
/// Runs at UTC midnight on January 1st
/// </summary>
public class YearEndTierJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<YearEndTierJob> _logger;

    public YearEndTierJob(
        IServiceScopeFactory scopeFactory,
        ILogger<YearEndTierJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = GetNextRunTime(now);

                _logger.LogInformation("YearEndTierJob scheduled to run at {NextRun}", nextRun);

                var delay = nextRun - now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessYearEndDemotions(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("YearEndTierJob cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in YearEndTierJob, retrying in 1 hour");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task ProcessYearEndDemotions(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting year-end tier demotion processing");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tierService = scope.ServiceProvider.GetRequiredService<ITierCalculationService>();

            var demotedCount = await tierService.ApplyYearEndDemotionsAsync(stoppingToken);
            await tierService.ResetYearlyValuesAsync(stoppingToken);

            _logger.LogInformation("Year-end tier demotion completed. Demoted {Count} companies", demotedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process year-end tier demotions");
            throw;
        }
    }

    private static DateTime GetNextRunTime(DateTime now)
    {
        var nextYear = now.Year + 1;
        var nextRun = new DateTime(nextYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        if (now >= nextRun)
        {
            nextRun = new DateTime(nextYear + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        return nextRun;
    }
}
