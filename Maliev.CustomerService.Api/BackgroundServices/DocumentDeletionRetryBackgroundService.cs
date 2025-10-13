using Maliev.CustomerService.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maliev.CustomerService.Api.BackgroundServices;

public class DocumentDeletionRetryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentDeletionRetryBackgroundService> _logger;

    public DocumentDeletionRetryBackgroundService(IServiceProvider serviceProvider, ILogger<DocumentDeletionRetryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocumentDeletionRetryBackgroundService started");

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
