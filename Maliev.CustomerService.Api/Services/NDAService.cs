using System.Text.Json;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
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

    /// <summary>
    /// Initializes a new instance of the NDAService class
    /// </summary>
    /// <param name="context">Database context for Customer Service</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="metricsService">Metrics service for recording NDA operations</param>
    public NDAService(CustomerDbContext context, ILogger<NDAService> logger, MetricsService metricsService)
    {
        _context = context;
        _logger = logger;
        _metricsService = metricsService;
    }

    /// <summary>
    /// Creates a new NDA record in Draft status with audit logging
    /// </summary>
    /// <param name="request">NDA creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created NDA response</returns>
    public async Task<NDAResponse> CreateAsync(CreateNDARequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating NDA for customer {CustomerId} by actor {ActorId} ({ActorType})",
            request.CustomerId, actorId, actorType);

        var nda = new NDARecord
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            DocumentReferenceId = request.DocumentReferenceId,
            Status = NDAStatus.Draft,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NDARecords.Add(nda);

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
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
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("NDA {NDAId} created successfully", nda.Id);
        return nda.ToNDAResponse();
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

        return nda.ToNDAResponse();
    }

    /// <inheritdoc />
    public async Task<List<NDAResponse>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving NDAs for customer {CustomerId}", customerId);

        var ndas = await _context.NDARecords
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return ndas.Select(n => n.ToNDAResponse()).ToList();
    }

    /// <summary>
    /// Updates NDA status with lifecycle validation and audit logging
    /// </summary>
    /// <param name="id">NDA ID</param>
    /// <param name="request">Status update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated NDA response</returns>
    /// <exception cref="KeyNotFoundException">Thrown when NDA is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when lifecycle transition is invalid or version conflict occurs</exception>
    public async Task<NDAResponse> UpdateStatusAsync(Guid id, UpdateNDAStatusRequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating NDA {NDAId} status to {Status} by actor {ActorId} ({ActorType})",
            id, request.Status, actorId, actorType);

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
            nda.RevokedAt
        };

        var oldStatus = nda.Status;
        ValidateLifecycleTransition(nda, request);

        nda.Status = request.Status;

        if (request.Status == NDAStatus.Signed)
        {
            nda.SignedBy = request.SignedBy;
            nda.SignedAt = request.SignedAt;
        }

        if (request.Status == NDAStatus.Revoked)
        {
            nda.RevokedAt = request.RevokedAt;
        }

        nda.UpdatedAt = DateTime.UtcNow;

        _context.Entry(nda).Property(n => n.Version).OriginalValue = request.Version;

        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Update,
            EntityType = nameof(NDARecord),
            EntityId = nda.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                nda.Status,
                nda.SignedBy,
                nda.SignedAt,
                nda.RevokedAt
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

        return nda.ToNDAResponse();
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
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expired {Count} NDAs", expiredNDAs.Count);

        return expiredNDAs.Count;
    }

    private void ValidateLifecycleTransition(NDARecord nda, UpdateNDAStatusRequest request)
    {
        if (NDAStatus.TerminalStates.Contains(nda.Status))
        {
            throw new InvalidOperationException(
                $"Cannot transition from terminal state '{nda.Status}'. Terminal states (Expired, Revoked) cannot be changed.");
        }

        if (nda.Status == NDAStatus.Draft && request.Status == NDAStatus.Signed)
        {
            if (!nda.DocumentReferenceId.HasValue)
            {
                throw new InvalidOperationException(
                    "Cannot transition to Signed status without a document reference. Please link a document first.");
            }
        }

        if (nda.Status == NDAStatus.Draft &&
            (request.Status == NDAStatus.Expired || request.Status == NDAStatus.Revoked))
        {
            throw new InvalidOperationException(
                $"Cannot transition from '{nda.Status}' to '{request.Status}'. Valid transitions from Draft: Signed only.");
        }
    }
}
