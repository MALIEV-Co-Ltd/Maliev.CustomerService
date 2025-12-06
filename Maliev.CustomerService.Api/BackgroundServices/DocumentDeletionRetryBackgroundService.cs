using Maliev.CustomerService.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        _logger.LogInformation("DocumentDeletionRetryBackgroundService started");

        // Wait for application to fully start before first check
        // This prevents blocking startup with database queries
        _logger.LogInformation("Waiting 60 seconds before first document deletion retry");
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                    var count = await documentService.RetryPendingDeletionsAsync();
                    _logger.LogInformation("Retried {Count} pending document deletions", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DocumentDeletionRetryBackgroundService");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        _logger.LogInformation("DocumentDeletionRetryBackgroundService stopped");
    }
}
