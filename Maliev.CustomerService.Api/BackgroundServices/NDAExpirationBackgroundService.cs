using Maliev.CustomerService.Api.Services;

namespace Maliev.CustomerService.Api.BackgroundServices;

/// <summary>
/// Background service that periodically checks for expired NDAs and updates their status
/// </summary>
public class NDAExpirationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NDAExpirationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    /// <summary>
    /// Initializes a new instance of the <see cref="NDAExpirationBackgroundService"/> class
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped services</param>
    /// <param name="logger">Logger instance</param>
    public NDAExpirationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NDAExpirationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background task that runs daily to check for expired NDAs
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NDA Expiration Background Service started");

        // Calculate time until next midnight UTC
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddDays(1);
        var initialDelay = nextRun - now;

        _logger.LogInformation("NDA Expiration Service waiting {Delay} until next run at {NextRun} UTC", initialDelay, nextRun);
        await Task.Delay(initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Starting NDA expiration check");

                // Create a scope to resolve scoped services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var ndaService = scope.ServiceProvider.GetRequiredService<INDAService>();

                    // Check for expired NDAs
                    var expiredCount = await ndaService.CheckExpiredNDAsAsync();
                    if (expiredCount > 0)
                        _logger.LogInformation("NDA expiration check completed. {ExpiredCount} NDAs expired", expiredCount);

                    // Check for upcoming expirations
                    var upcomingCount = await ndaService.CheckUpcomingExpirationsAsync();
                    if (upcomingCount > 0)
                        _logger.LogInformation("NDA upcoming expiration check completed. {UpcomingCount} warnings sent", upcomingCount);

                    if (expiredCount == 0 && upcomingCount == 0)
                    {
                        _logger.LogDebug("NDA expiration check completed. No actions needed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking for expired NDAs");
            }

            // Wait 24 hours before the next check
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("NDA Expiration Background Service stopped");
    }
}
