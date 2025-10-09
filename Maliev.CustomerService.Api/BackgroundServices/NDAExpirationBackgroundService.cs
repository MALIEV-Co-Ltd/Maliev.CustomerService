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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting NDA expiration check");

                // Create a scope to resolve scoped services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var ndaService = scope.ServiceProvider.GetRequiredService<INDAService>();

                    var expiredCount = await ndaService.CheckExpiredNDAsAsync();

                    _logger.LogInformation("NDA expiration check completed. {ExpiredCount} NDAs expired", expiredCount);
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
