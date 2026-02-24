using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Maliev.MessagingContracts.Contracts.Uploads;
using Maliev.MessagingContracts.Generated;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Consumers;

/// <summary>
/// Consumes FileDeletedEvent to clean up local document references (FR-025).
/// </summary>
public class FileDeletedEventConsumer : IConsumer<FileDeletedEvent>
{
    private readonly CustomerDbContext _dbContext;
    private readonly ILogger<FileDeletedEventConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDeletedEventConsumer"/> class.
    /// </summary>
    public FileDeletedEventConsumer(CustomerDbContext dbContext, ILogger<FileDeletedEventConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Consumes the FileDeletedEvent.
    /// </summary>
    public async Task Consume(ConsumeContext<FileDeletedEvent> context)
    {
        var @event = context.Message;
        var payload = @event.Payload;

        if (payload.ServiceId != "customer-service")
        {
            return;
        }

        _logger.LogInformation("Processing FileDeletedEvent for FileId: {FileId}, StoragePath: {StoragePath}",
            payload.FileId, payload.StoragePath);

        // Find documents referencing this file
        var documents = await _dbContext.DocumentReferences
            .Where(d => d.FileReference == payload.FileId || d.FileReference == payload.StoragePath)
            .ToListAsync(context.CancellationToken);

        if (documents.Any())
        {
            foreach (var doc in documents)
            {
                doc.Status = DocumentStatus.MissingFile;
                doc.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("Updated {Count} document references to Deleted status.", documents.Count);
        }
    }
}
