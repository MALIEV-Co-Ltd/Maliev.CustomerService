using System.Text.Json;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Infrastructure.Persistence;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Nda;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for NDA lifecycle management operations
/// </summary>
public class NDAService : INDAService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<NDAService> _logger;
    private readonly MetricsService _metricsService;
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the NDAService class
    /// </summary>
    /// <param name="context">Database context for Customer Service</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="metricsService">Metrics service for recording NDA operations</param>
    /// <param name="publishEndpoint">MassTransit publish endpoint</param>
    public NDAService(CustomerDbContext context, ILogger<NDAService> logger, MetricsService metricsService, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _logger = logger;
        _metricsService = metricsService;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Creates a new NDA record in Draft status with audit logging
    /// </summary>
    /// <param name="request">NDA creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="actorName">Name of the actor performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created NDA response</returns>
    public async Task<NDAResponse> CreateAsync(CreateNDARequest request, string actorId, string actorType, string actorName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating NDA for customer {CustomerId} by actor {ActorId} ({ActorName} - {ActorType})",
            request.CustomerId, actorId, actorName, actorType);

        var status = NDAStatus.Draft;
        if (!string.IsNullOrEmpty(request.Status) && NDAStatus.All.Contains(request.Status))
        {
            status = request.Status;
        }

        var nda = new NDARecord
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            DocumentReferenceId = request.DocumentReferenceId,
            Status = status,
            ExpiresAt = request.ExpiresAt?.Kind == DateTimeKind.Unspecified

                ? DateTime.SpecifyKind(request.ExpiresAt.Value, DateTimeKind.Utc)
                : request.ExpiresAt?.ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };


        _context.NDARecords.Add(nda);

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            ActorName = actorName,
            Action = AuditAction.Create,
            EntityType = nameof(NDARecord),
            EntityId = nda.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                nda.CustomerId,
                nda.DocumentReferenceId,
                nda.Status,
                nda.ExpiresAt
            })
        };

        _context.AuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error creating NDA for customer {CustomerId}. Inner: {InnerMessage}",
                request.CustomerId, ex.InnerException?.Message);
            throw;
        }

        _logger.LogInformation("NDA {NDAId} created successfully", nda.Id);
        var xminValue = _context.Entry(nda).Property<uint>("xmin").CurrentValue;
        return nda.ToNDAResponse(xminValue);
    }

    /// <summary>
    /// Retrieves an NDA record by ID
    /// </summary>
    /// <param name="id">NDA ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>NDA response or null if not found</returns>
    public async Task<NDAResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving NDA {NDAId}", id);

        var nda = await _context.NDARecords
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (nda == null)
        {
            _logger.LogDebug("NDA {NDAId} not found", id);
            return null;
        }

        var xminValue = _context.Entry(nda).Property<uint>("xmin").CurrentValue;
        return nda.ToNDAResponse(xminValue);
    }

    /// <inheritdoc />
    public async Task<List<NDAResponse>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving NDAs for customer {CustomerId}", customerId);

        var ndas = await _context.NDARecords
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return ndas.Select(n => n.ToNDAResponse(_context.Entry(n).Property<uint>("xmin").CurrentValue)).ToList();
    }

    /// <summary>
    /// Updates NDA status with lifecycle validation and audit logging
    /// </summary>
    /// <param name="id">NDA ID</param>
    /// <param name="request">Status update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="actorName">Name of the actor performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated NDA response</returns>
    /// <exception cref="KeyNotFoundException">Thrown when NDA is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when lifecycle transition is invalid or version conflict occurs</exception>
    public async Task<NDAResponse> UpdateStatusAsync(Guid id, UpdateNDAStatusRequest request, string actorId, string actorType, string actorName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating NDA {NDAId} status to {Status} by actor {ActorId} ({ActorName} - {ActorType})",
            id, request.Status, actorId, actorName, actorType);

        var nda = await _context.NDARecords
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (nda == null)
        {
            _logger.LogWarning("NDA {NDAId} not found for status update", id);
            throw new KeyNotFoundException($"NDA with ID '{id}' not found");
        }

        var previousValues = new
        {
            nda.Status,
            nda.SignedBy,
            nda.SignedAt,
            nda.RevokedAt,
            nda.ExpiresAt,
            nda.DocumentReferenceId,
            nda.RevokeReason
        };

        var oldStatus = nda.Status;
        ValidateLifecycleTransition(nda, request, actorType);

        nda.Status = request.Status;

        // Update document if provided
        if (request.DocumentReferenceId.HasValue)
        {
            nda.DocumentReferenceId = request.DocumentReferenceId;
        }

        // Update expiration date if provided
        if (request.ExpiresAt.HasValue)
        {
            nda.ExpiresAt = request.ExpiresAt.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.ExpiresAt.Value, DateTimeKind.Utc)
                : request.ExpiresAt.Value.ToUniversalTime();
        }

        if (request.Status == NDAStatus.Signed)
        {
            nda.SignedBy = request.SignedBy;
            nda.SignedAt = request.SignedAt?.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.SignedAt.Value, DateTimeKind.Utc)
                : request.SignedAt?.ToUniversalTime();
        }


        if (request.Status == NDAStatus.Revoked)
        {
            if (string.IsNullOrWhiteSpace(request.RevokeReason))
            {
                throw new InvalidOperationException("Revoke reason is required when revoking an NDA.");
            }

            nda.RevokeReason = request.RevokeReason;
            nda.RevokedAt = request.RevokedAt?.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.RevokedAt.Value, DateTimeKind.Utc)
                : request.RevokedAt?.ToUniversalTime();
        }


        nda.UpdatedAt = DateTime.UtcNow;

        _context.Entry(nda).Property("xmin").OriginalValue = request.xmin;

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            ActorName = actorName,
            Action = AuditAction.Update,
            EntityType = nameof(NDARecord),
            EntityId = nda.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                nda.Status,
                nda.SignedBy,
                nda.SignedAt,
                nda.RevokedAt,
                nda.ExpiresAt,
                nda.DocumentReferenceId,
                nda.RevokeReason
            }),
            PreviousValues = JsonSerializer.Serialize(previousValues)
        };

        _context.AuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("NDA {NDAId} status updated successfully to {Status}", id, request.Status);

            // Record NDA state transition metric
            _metricsService.RecordNdaTransition(oldStatus, request.Status);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating NDA {NDAId}", id);
            throw new InvalidOperationException("The record was modified by another user. Please refresh and try again.");
        }

        var xminValue = _context.Entry(nda).Property<uint>("xmin").CurrentValue;
        return nda.ToNDAResponse(xminValue);
    }

    /// <summary>
    /// Checks for expired NDAs and transitions them to Expired status (for background job processing)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of NDAs that were expired</returns>
    public async Task<int> CheckExpiredNDAsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for expired NDAs");

        var now = DateTime.UtcNow;

        var expiredNDAs = await _context.NDARecords
            .Where(n => n.Status == NDAStatus.Signed &&
                       n.ExpiresAt.HasValue &&
                       n.ExpiresAt.Value < now)
            .ToListAsync(cancellationToken);

        if (expiredNDAs.Count == 0)
        {
            _logger.LogDebug("No expired NDAs found");
            return 0;
        }

        foreach (var nda in expiredNDAs)
        {
            _logger.LogInformation("Expiring NDA {NDAId} for customer {CustomerId}", nda.Id, nda.CustomerId);

            nda.Status = NDAStatus.Expired;
            nda.UpdatedAt = DateTime.UtcNow;

            var auditLog = new AuditLog
            {
                ActorId = "System",
                ActorType = "System",
                Action = "AutoExpire",
                EntityType = nameof(NDARecord),
                EntityId = nda.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonSerializer.Serialize(new { Status = NDAStatus.Expired }),
                PreviousValues = JsonSerializer.Serialize(new { Status = NDAStatus.Signed })
            };

            _context.AuditLogs.Add(auditLog);

            // Publish event
            await _publishEndpoint.Publish(new NdaExpiredEvent(
                MessageId: Guid.NewGuid(),
                MessageName: "NdaExpiredEvent",
                MessageType: MessageType.Event,
                MessageVersion: "1.0.0",
                PublishedBy: "CustomerService",
                ConsumedBy: ["NotificationService"],
                CorrelationId: Guid.NewGuid(),
                CausationId: null,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                IsPublic: false,
                Payload: new NdaExpiredEventPayload(
                    NdaId: nda.Id,
                    CustomerId: nda.CustomerId,
                    ExpiredAt: nda.ExpiresAt ?? DateTimeOffset.UtcNow,
                    ProcessedAt: DateTimeOffset.UtcNow
                )
            ), cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expired {Count} NDAs", expiredNDAs.Count);

        return expiredNDAs.Count;
    }

    /// <inheritdoc />
    public async Task<int> CheckUpcomingExpirationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking for upcoming NDA expirations");

        var now = DateTime.UtcNow.Date;
        var thresholds = new[] { 30, 7, 1 }; // Days before expiration to warn
        int eventCount = 0;

        foreach (var days in thresholds)
        {
            var targetDate = now.AddDays(days);

            // Find NDAs expiring on the exact target date (ignoring time)
            var expiringNdas = await _context.NDARecords
                .Where(n => n.Status == NDAStatus.Signed &&
                            n.ExpiresAt.HasValue &&
                            n.ExpiresAt.Value.Date == targetDate)
                .ToListAsync(cancellationToken);

            foreach (var nda in expiringNdas)
            {
                await _publishEndpoint.Publish(new NdaExpiringEvent(
                    MessageId: Guid.NewGuid(),
                    MessageName: "NdaExpiringEvent",
                    MessageType: MessageType.Event,
                    MessageVersion: "1.0.0",
                    PublishedBy: "CustomerService",
                    ConsumedBy: ["NotificationService"],
                    CorrelationId: Guid.NewGuid(),
                    CausationId: null,
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    IsPublic: false,
                    Payload: new NdaExpiringEventPayload(
                        NdaId: nda.Id,
                        CustomerId: nda.CustomerId,
                        ExpiresAt: nda.ExpiresAt!.Value,
                        DaysUntilExpiration: days,
                        WarningGeneratedAt: DateTimeOffset.UtcNow
                    )
                ), cancellationToken);

                eventCount++;
            }
        }

        if (eventCount > 0)
            _logger.LogInformation("Published {Count} NDA expiration warnings", eventCount);

        return eventCount;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, uint xmin, string actorId, string actorType, string actorName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting NDA {NDAId} by actor {ActorId} ({ActorName} - {ActorType})",
            id, actorId, actorName, actorType);

        var nda = await _context.NDARecords
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (nda == null)
        {
            _logger.LogDebug("NDA {NDAId} not found for deletion", id);
            return false;
        }

        _context.Entry(nda).Property("xmin").OriginalValue = xmin;
        _context.NDARecords.Remove(nda);

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            ActorName = actorName,
            Action = AuditAction.Delete,
            EntityType = nameof(NDARecord),
            EntityId = nda.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            PreviousValues = JsonSerializer.Serialize(new
            {
                nda.CustomerId,
                nda.Status,
                nda.ExpiresAt
            })
        };

        _context.AuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("NDA {NDAId} deleted successfully", id);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict deleting NDA {NDAId}", id);
            throw new InvalidOperationException("The record was modified by another user. Please refresh and try again.");
        }
    }

    /// <inheritdoc />
    public async Task<List<NDAAuditLogResponse>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving history for NDA {NDAId}", id);

        var logs = await _context.AuditLogs
            .Where(l => l.EntityType == nameof(NDARecord) && l.EntityId == id.ToString())
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);

        // Collect all document IDs to fetch names
        var documentIds = new HashSet<Guid>();
        var tempLogs = new List<(AuditLog Log, Guid? DocId, Guid? PrevDocId)>();

        foreach (var log in logs)
        {
            Guid? docId = null;
            Guid? prevDocId = null;

            if (!string.IsNullOrEmpty(log.ChangedFields))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.ChangedFields);
                    if (doc.RootElement.TryGetProperty("DocumentReferenceId", out var dProp) || doc.RootElement.TryGetProperty("documentReferenceId", out dProp))
                    {
                        if (dProp.ValueKind != JsonValueKind.Null && dProp.TryGetGuid(out var guid))
                        {
                            docId = guid;
                            documentIds.Add(guid);
                        }
                    }
                }
                catch { /* Ignore */ }
            }

            if (!string.IsNullOrEmpty(log.PreviousValues))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.PreviousValues);
                    if (doc.RootElement.TryGetProperty("DocumentReferenceId", out var dProp) || doc.RootElement.TryGetProperty("documentReferenceId", out dProp))
                    {
                        if (dProp.ValueKind != JsonValueKind.Null && dProp.TryGetGuid(out var guid))
                        {
                            prevDocId = guid;
                            documentIds.Add(guid);
                        }
                    }
                }
                catch { /* Ignore */ }
            }

            tempLogs.Add((log, docId, prevDocId));
        }

        // Fetch document names
        var documentNames = new Dictionary<Guid, string>();
        if (documentIds.Any())
        {
            documentNames = await _context.DocumentReferences
                .Where(d => documentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Filename, cancellationToken);
        }

        var history = new List<NDAAuditLogResponse>();

        foreach (var (log, docId, prevDocId) in tempLogs)
        {
            string? status = null;
            string? previousStatus = null;
            DateTime? expiresAt = null;
            DateTime? revokedAt = null;

            if (!string.IsNullOrEmpty(log.ChangedFields))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.ChangedFields);
                    if (doc.RootElement.TryGetProperty("Status", out var sProp) || doc.RootElement.TryGetProperty("status", out sProp))
                        status = sProp.GetString();

                    if (doc.RootElement.TryGetProperty("ExpiresAt", out var eProp) || doc.RootElement.TryGetProperty("expiresAt", out eProp))
                        expiresAt = eProp.ValueKind != JsonValueKind.Null ? eProp.GetDateTime() : null;

                    if (doc.RootElement.TryGetProperty("RevokedAt", out var rProp) || doc.RootElement.TryGetProperty("revokedAt", out rProp))
                        revokedAt = rProp.ValueKind != JsonValueKind.Null ? rProp.GetDateTime() : null;
                }
                catch { /* Ignore JSON parse errors */ }
            }

            if (!string.IsNullOrEmpty(log.PreviousValues))
            {
                try
                {
                    using var doc = JsonDocument.Parse(log.PreviousValues);
                    if (doc.RootElement.TryGetProperty("Status", out var sProp) || doc.RootElement.TryGetProperty("status", out sProp))
                        previousStatus = sProp.GetString();
                }
                catch { /* Ignore JSON parse errors */ }
            }

            history.Add(new NDAAuditLogResponse
            {
                Id = log.Id,
                Action = log.Action,
                ActorId = log.ActorId,
                ActorType = log.ActorType,
                ActorName = log.ActorName,
                Timestamp = log.Timestamp,
                Status = status,
                PreviousStatus = previousStatus,
                ExpiresAt = expiresAt,
                RevokedAt = revokedAt,
                DocumentReferenceId = docId,
                DocumentName = docId.HasValue && documentNames.TryGetValue(docId.Value, out var name) ? name : null,
                PreviousDocumentReferenceId = prevDocId,
                PreviousDocumentName = prevDocId.HasValue && documentNames.TryGetValue(prevDocId.Value, out var prevName) ? prevName : null
            });
        }

        return history;
    }

    private void ValidateLifecycleTransition(NDARecord nda, UpdateNDAStatusRequest request, string actorType)

    {
        if (NDAStatus.TerminalStates.Contains(nda.Status))
        {
            throw new InvalidOperationException(
                $"Cannot transition from terminal state '{nda.Status}'. Terminal states (Expired, Revoked) cannot be changed.");
        }

        if (nda.Status == NDAStatus.Draft && request.Status == NDAStatus.Signed)
        {
            // T159: Allow signing without document if performed by an employee
            if (!nda.DocumentReferenceId.HasValue && !string.Equals(actorType, "Employee", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Cannot transition to Signed status without a document reference. Please upload and link a document first.");
            }
        }

        if (nda.Status == NDAStatus.Draft && request.Status == NDAStatus.Revoked)
        {
            throw new InvalidOperationException(
                $"Cannot transition from '{nda.Status}' to '{request.Status}'. NDAs must be Signed before they can be Revoked.");
        }

        if (nda.Status == NDAStatus.Draft && request.Status == NDAStatus.Expired)
        {
            throw new InvalidOperationException(
                $"Cannot transition from '{nda.Status}' to '{request.Status}'. Valid transitions from Draft: Signed, Revoked.");
        }
    }
}
