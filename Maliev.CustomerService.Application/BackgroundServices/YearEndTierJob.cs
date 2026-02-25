using Maliev.CustomerService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maliev.CustomerService.Application.BackgroundServices;

/// <summary>
/// Background service for year-end tier demotion processing.
/// Runs at UTC midnight on January 1st each year.
///
/// Design note: <see cref="BackgroundService"/> is registered as a singleton by the host.
/// To safely consume scoped services (e.g. EF Core DbContext), we resolve
/// <see cref="IYearEndTierProcessor"/> from a new DI scope per execution.
/// This is the recommended Microsoft pattern for singleton background services.
/// See: https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services#consuming-a-scoped-service-in-a-background-task
/// </summary>
public class YearEndTierJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<YearEndTierJob> _logger;

    public YearEndTierJob(IServiceScopeFactory scopeFactory, ILogger<YearEndTierJob> logger)
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

                // Task.Delay supports a maximum of ~49 days (uint.MaxValue - 1 ms).
                // Wait in 30-day chunks to avoid ArgumentOutOfRangeException.
                while (delay.TotalDays > 30 && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromDays(30), stoppingToken);
                    now = DateTime.UtcNow;
                    delay = nextRun - now;
                }

                if (delay > TimeSpan.Zero && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ProcessYearEndDemotionsAsync(stoppingToken);
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

    private async Task ProcessYearEndDemotionsAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Create a new DI scope per execution — required for singleton BackgroundService
            // consuming scoped services (EF Core DbContext via IYearEndTierProcessor).
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IYearEndTierProcessor>();

            var demotedCount = await processor.RunAsync(stoppingToken);

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
        var nextRun = new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Safety check: if we somehow missed this year's run, schedule for next year
        if (now >= nextRun)
        {
            nextRun = new DateTime(now.Year + 2, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        return nextRun;
    }
}
