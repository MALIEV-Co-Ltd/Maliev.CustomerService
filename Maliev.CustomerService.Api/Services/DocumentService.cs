using Maliev.CustomerService.Api.Models.Documents;
using Maliev.CustomerService.Api.Services.External;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Maliev.CustomerService.Api.Services;

public class DocumentService : IDocumentService
{
    private readonly CustomerDbContext _context;
    private readonly IUploadServiceClient _uploadServiceClient;
    private readonly ILogger<DocumentService> _logger;
    private readonly MetricsService _metricsService;

    public DocumentService(
        CustomerDbContext context,
        IUploadServiceClient uploadServiceClient,
        ILogger<DocumentService> logger,
        MetricsService metricsService)
    {
        _context = context;
        _uploadServiceClient = uploadServiceClient;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<DocumentResponse> CreateAsync(CreateDocumentRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Creating document for owner {OwnerType}/{OwnerId} by actor {ActorId} ({ActorType})",
            request.OwnerType, request.OwnerId, actorId, actorType);

        // Validate file reference with Upload Service
        var isValid = await _uploadServiceClient.ValidateFileReferenceAsync(request.FileReference);
        if (!isValid)
        {
            _logger.LogWarning("Invalid file reference {FileReference} from Upload Service", request.FileReference);
            throw new InvalidOperationException($"File reference '{request.FileReference}' is not valid in Upload Service");
        }

        var document = new DocumentReference
        {
            Id = Guid.NewGuid(),
            OwnerType = request.OwnerType,
            OwnerId = request.OwnerId,
            DocumentType = request.DocumentType,
            FileReference = request.FileReference,
            Filename = request.Filename,
            Status = DocumentStatus.Pending,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentReferences.Add(document);

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Create,
            EntityType = nameof(DocumentReference),
            EntityId = document.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                document.OwnerType,
                document.OwnerId,
                document.DocumentType,
                document.FileReference,
                document.Filename,
                document.Status,
                document.Version
            })
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document {DocumentId} created successfully", document.Id);

        // Record metrics
        _metricsService.RecordDocumentOperation("create");

        return MapToResponse(document);
    }

    public async Task<List<DocumentResponse>> GetByOwnerAsync(string ownerType, Guid ownerId)
    {
        _logger.LogDebug("Retrieving documents for owner {OwnerType}/{OwnerId}", ownerType, ownerId);

        var documents = await _context.DocumentReferences
            .Where(d => d.OwnerType == ownerType && d.OwnerId == ownerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(MapToResponse).ToList();
    }

    public async Task<DocumentResponse> UpdateAsync(Guid id, UpdateDocumentRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Updating document {DocumentId} with new file reference by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var document = await _context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == id);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found for update", id);
            throw new KeyNotFoundException($"Document with ID '{id}' not found");
        }

        // Validate new file reference with Upload Service
        var isValid = await _uploadServiceClient.ValidateFileReferenceAsync(request.FileReference);
        if (!isValid)
        {
            _logger.LogWarning("Invalid file reference {FileReference} from Upload Service", request.FileReference);
            throw new InvalidOperationException($"File reference '{request.FileReference}' is not valid in Upload Service");
        }

        var previousValues = new
        {
            document.FileReference,
            document.Filename,
            document.Version
        };

        // Increment version
        document.FileReference = request.FileReference;
        document.Filename = request.Filename;
        document.Version++;
        document.UpdatedAt = DateTime.UtcNow;

        _context.Entry(document).Property(d => d.RowVersion).OriginalValue = request.RowVersion;

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Update,
            EntityType = nameof(DocumentReference),
            EntityId = document.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                document.FileReference,
                document.Filename,
                document.Version
            }),
            PreviousValues = JsonSerializer.Serialize(previousValues)
        };

        _context.AuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Document {DocumentId} updated successfully to version {Version}", id, document.Version);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating document {DocumentId}", id);
            throw new InvalidOperationException("The document was modified by another user. Please refresh and try again.");
        }

        return MapToResponse(document);
    }

    public async Task<DocumentResponse> MarkCompleteAsync(Guid id, string? signedBy, DateTime? signedAt, string actorId, string actorType)
    {
        _logger.LogInformation("Marking document {DocumentId} as complete by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var document = await _context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == id);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found", id);
            throw new KeyNotFoundException($"Document with ID '{id}' not found");
        }

        var previousValues = new
        {
            document.Status,
            document.SignedBy,
            document.SignedAt
        };

        document.Status = DocumentStatus.Complete;
        document.SignedBy = signedBy;
        document.SignedAt = signedAt;
        document.UpdatedAt = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Update,
            EntityType = nameof(DocumentReference),
            EntityId = document.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                document.Status,
                document.SignedBy,
                document.SignedAt
            }),
            PreviousValues = JsonSerializer.Serialize(previousValues)
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document {DocumentId} marked as complete", id);

        // Record metrics
        _metricsService.RecordDocumentOperation("complete");

        return MapToResponse(document);
    }

    public async Task DeleteAsync(Guid id, string actorId, string actorType)
    {
        _logger.LogInformation("Deleting document {DocumentId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var document = await _context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == id);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found for deletion", id);
            throw new KeyNotFoundException($"Document with ID '{id}' not found");
        }

        // Try to delete from Upload Service
        var deleted = await _uploadServiceClient.DeleteFileAsync(document.FileReference);
        if (!deleted)
        {
            _logger.LogWarning("Failed to delete file {FileReference} from Upload Service, marking as PendingDeletion",
                document.FileReference);
            document.Status = DocumentStatus.PendingDeletion;
            document.UpdatedAt = DateTime.UtcNow;

            var auditLog = new AuditLog
            {
                ActorId = actorId,
                ActorType = actorType,
                Action = "MarkPendingDeletion",
                EntityType = nameof(DocumentReference),
                EntityId = document.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonSerializer.Serialize(new { Status = DocumentStatus.PendingDeletion }),
                PreviousValues = JsonSerializer.Serialize(new { document.Status })
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Document {DocumentId} marked as PendingDeletion", id);
            return;
        }

        // Successfully deleted from Upload Service, remove from database
        _context.DocumentReferences.Remove(document);

        var deleteAuditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Delete,
            EntityType = nameof(DocumentReference),
            EntityId = document.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            PreviousValues = JsonSerializer.Serialize(new
            {
                document.OwnerType,
                document.OwnerId,
                document.DocumentType,
                document.FileReference,
                document.Filename,
                document.Status
            })
        };

        _context.AuditLogs.Add(deleteAuditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document {DocumentId} deleted successfully", id);

        // Record metrics
        _metricsService.RecordDocumentOperation("delete");
    }

    public async Task<int> RetryPendingDeletionsAsync()
    {
        _logger.LogInformation("Retrying pending document deletions");

        var pendingDeletions = await _context.DocumentReferences
            .Where(d => d.Status == DocumentStatus.PendingDeletion)
            .ToListAsync();

        if (pendingDeletions.Count == 0)
        {
            _logger.LogInformation("No pending deletions found");
            return 0;
        }

        var successCount = 0;

        foreach (var document in pendingDeletions)
        {
            _logger.LogInformation("Retrying deletion for document {DocumentId} with file {FileReference}",
                document.Id, document.FileReference);

            var deleted = await _uploadServiceClient.DeleteFileAsync(document.FileReference);
            if (deleted)
            {
                _context.DocumentReferences.Remove(document);

                var auditLog = new AuditLog
                {
                    ActorId = "System",
                    ActorType = "System",
                    Action = "RetryDeletion",
                    EntityType = nameof(DocumentReference),
                    EntityId = document.Id.ToString(),
                    Timestamp = DateTime.UtcNow,
                    PreviousValues = JsonSerializer.Serialize(new
                    {
                        document.OwnerType,
                        document.OwnerId,
                        document.DocumentType,
                        document.FileReference,
                        document.Status
                    })
                };

                _context.AuditLogs.Add(auditLog);
                successCount++;

                // Record successful retry metric
                _metricsService.RecordDocumentDeletionRetry(true);
            }
            else
            {
                _logger.LogWarning("Retry deletion failed for document {DocumentId}", document.Id);

                // Record failed retry metric
                _metricsService.RecordDocumentDeletionRetry(false);
            }
        }

        if (successCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Retried {SuccessCount} out of {TotalCount} pending deletions",
            successCount, pendingDeletions.Count);

        return successCount;
    }

    private DocumentResponse MapToResponse(DocumentReference document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            OwnerType = document.OwnerType,
            OwnerId = document.OwnerId,
            DocumentType = document.DocumentType,
            FileReference = document.FileReference,
            Filename = document.Filename,
            Status = document.Status,
            Version = document.Version,
            SignedBy = document.SignedBy,
            SignedAt = document.SignedAt,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            RowVersion = document.RowVersion
        };
    }
}
