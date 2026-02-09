using Maliev.CustomerService.Api.Services;

namespace Maliev.CustomerService.Api.BackgroundServices;

/// <summary>
/// Background service that periodically retries pending document deletions
/// </summary>
public class DocumentDeletionRetryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentDeletionRetryBackgroundService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentDeletionRetryBackgroundService"/> class
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scoped services</param>
    /// <param name="logger">Logger instance</param>
    public DocumentDeletionRetryBackgroundService(IServiceProvider serviceProvider, ILogger<DocumentDeletionRetryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background task that runs daily to retry pending document deletions
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocumentDeletionRetryBackgroundService starting - waiting 15 seconds for infrastructure");

        // Initial delay to allow infrastructure (database, external services) to warm up
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        // First execution with retry logic
        var maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (int attempt = 1; attempt <= maxRetries && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                _logger.LogInformation("Initial document deletion retry attempt {Attempt}/{MaxRetries}", attempt, maxRetries);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                    var count = await documentService.RetryPendingDeletionsAsync();
                    if (count > 0)
                    {
                        _logger.LogInformation("Retried {Count} pending document deletions", count);
                    }
                    else
                    {
                        _logger.LogDebug("No pending document deletions to retry");
                    }
                }

                _logger.LogInformation("Initial document deletion retry completed successfully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Initial document deletion retry attempt {Attempt}/{MaxRetries} failed", attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    _logger.LogInformation("Retrying in {RetryDelay} seconds", retryDelay.TotalSeconds);
                    await Task.Delay(retryDelay, stoppingToken);
                    retryDelay = TimeSpan.FromSeconds(retryDelay.TotalSeconds * 2); // Exponential backoff
                }
                else
                {
                    _logger.LogError(ex, "Initial document deletion retry failed after {MaxRetries} attempts", maxRetries);
                }
            }
        }

        // Continue with regular scheduled checks (every 24 hours)
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                    var count = await documentService.RetryPendingDeletionsAsync();
                    if (count > 0)
                    {
                        _logger.LogInformation("Retried {Count} pending document deletions", count);
                    }
                    else
                    {
                        _logger.LogDebug("No pending document deletions to retry");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DocumentDeletionRetryBackgroundService");
            }
        }

        _logger.LogInformation("DocumentDeletionRetryBackgroundService stopped");
    }
}
