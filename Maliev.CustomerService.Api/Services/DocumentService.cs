using System.Text.Json;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.Documents;
using Maliev.CustomerService.Api.Services.External;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for document reference management operations
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly CustomerDbContext _context;
    private readonly IIAMClient _iamClient;
    private readonly IUploadServiceClient _uploadServiceClient;
    private readonly ILogger<DocumentService> _logger;
    private readonly MetricsService _metricsService;

    /// <summary>
    /// Initializes a new instance of the DocumentService class
    /// </summary>
    /// <param name="context">Database context for Customer Service</param>
    /// <param name="iamClient">Client for IAM integration</param>
    /// <param name="uploadServiceClient">Client for Upload Service integration</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="metricsService">Metrics service for recording operations</param>
    public DocumentService(
        CustomerDbContext context,
        IIAMClient iamClient,
        IUploadServiceClient uploadServiceClient,
        ILogger<DocumentService> logger,
        MetricsService metricsService)
    {
        _context = context;
        _iamClient = iamClient;
        _uploadServiceClient = uploadServiceClient;
        _logger = logger;
        _metricsService = metricsService;
    }


    /// <summary>
    /// Creates a new document reference with Upload Service validation
    /// </summary>
    /// <param name="request">Document creation request containing owner details and file reference</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document response</returns>
    /// <exception cref="InvalidOperationException">Thrown when file reference is invalid in Upload Service</exception>
    public async Task<DocumentResponse> CreateAsync(CreateDocumentRequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
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
            FileSize = request.FileSize,
            MimeType = request.MimeType,
            Status = DocumentStatus.Pending,
            Version = 1,
            CreatedBy = actorId,
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
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} created successfully", document.Id);

        // Record metrics
        _metricsService.RecordDocumentOperation("create");

        return document.ToDocumentResponse();
    }

    /// <summary>
    /// Retrieves all document references for a specific owner
    /// </summary>
    /// <param name="ownerType">Type of owner (Customer or Company)</param>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document responses ordered by creation date descending</returns>
    public async Task<List<DocumentResponse>> GetByOwnerAsync(string ownerType, Guid ownerId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving documents for owner {OwnerType}/{OwnerId}", ownerType, ownerId);

        var documents = await _context.DocumentReferences
            .Where(d => d.OwnerId == ownerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        var filteredDocs = documents
            .Where(d => d.OwnerType.Equals(ownerType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Resolve creator names
        var creatorIds = filteredDocs
            .Where(d => Guid.TryParse(d.CreatedBy, out _))
            .Select(d => Guid.Parse(d.CreatedBy))
            .Distinct()
            .ToList();

        var creatorMap = new Dictionary<Guid, Maliev.CustomerService.Api.Models.IAM.PrincipalResponse>();
        foreach (var cId in creatorIds)
        {
            var principal = await _iamClient.GetPrincipalByIdAsync(cId, cancellationToken);
            if (principal != null) creatorMap[cId] = principal;
        }

        var responses = new List<DocumentResponse>();
        foreach (var doc in filteredDocs)
        {
            var res = doc.ToDocumentResponse();
            if (Guid.TryParse(doc.CreatedBy, out var pId) && creatorMap.TryGetValue(pId, out var p))
            {
                res.CreatedByName = p.DisplayName;
                res.CreatedByEmail = p.Email;
            }
            responses.Add(res);
        }

        return responses;
    }

    /// <summary>
    /// Updates a document reference with a new file (creates new version)
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="request">Update request containing new file reference and filename</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document response with incremented version</returns>
    /// <exception cref="KeyNotFoundException">Thrown when document is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when file reference is invalid or version conflict occurs</exception>
    public async Task<DocumentResponse> UpdateAsync(Guid id, UpdateDocumentRequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating document {DocumentId} with new file reference by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var document = await _context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
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
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Document {DocumentId} updated successfully to version {Version}", id, document.Version);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating document {DocumentId}", id);
            throw new InvalidOperationException("The record was modified by another user. Please refresh and try again.");
        }

        return document.ToDocumentResponse();
    }

    /// <summary>
    /// Marks a document as complete with optional signature information
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="signedBy">Optional name of the person who signed the document</param>
    /// <param name="signedAt">Optional timestamp when the document was signed</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document response with Complete status</returns>
    /// <exception cref="KeyNotFoundException">Thrown when document is not found</exception>
    public async Task<DocumentResponse> MarkCompleteAsync(Guid id, string? signedBy, DateTime? signedAt, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Marking document {DocumentId} as complete by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var document = await _context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
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
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Document {DocumentId} marked as complete", id);

        // Record metrics
        _metricsService.RecordDocumentOperation("complete");

        return document.ToDocumentResponse();
    }

    /// <summary>
    /// Deletes a document reference and associated file from Upload Service
    /// </summary>
    /// <param name="id">Document ID</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="KeyNotFoundException">Thrown when document is not found</exception>
    /// <remarks>
    /// If deletion from Upload Service fails, the document is marked as PendingDeletion for retry
    /// </remarks>
    public async Task DeleteAsync(Guid id, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting document {DocumentId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var document = await _context.DocumentReferences.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found for deletion", id);
            throw new KeyNotFoundException($"Document with ID '{id}' not found");
        }

        // Standard Pattern: Mark as PendingDeletion first to ensure atomicity
        var oldStatus = document.Status;
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
            PreviousValues = JsonSerializer.Serialize(new { Status = oldStatus })
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        // Try to delete from Upload Service
        var deleted = await _uploadServiceClient.DeleteFileAsync(document.FileReference);
        if (deleted)
        {
            _logger.LogInformation("Successfully deleted file {FileReference} from Upload Service, removing local record",
                document.FileReference);

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
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document {DocumentId} deleted successfully", id);

            // Record metrics
            _metricsService.RecordDocumentOperation("delete");
        }
        else
        {
            _logger.LogWarning("Failed to delete file {FileReference} from Upload Service, will retry in background",
                document.FileReference);
        }
    }

    /// <summary>
    /// Retries deletion of documents marked as PendingDeletion (for background job processing)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of successfully deleted documents</returns>
    public async Task<int> RetryPendingDeletionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrying pending document deletions");

        var pendingDeletions = await _context.DocumentReferences
            .Where(d => d.Status == DocumentStatus.PendingDeletion)
            .ToListAsync(cancellationToken);

        if (pendingDeletions.Count == 0)
        {
            _logger.LogDebug("No pending deletions found");
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
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Retried {SuccessCount} out of {TotalCount} pending deletions",
            successCount, pendingDeletions.Count);

        return successCount;
    }
}
