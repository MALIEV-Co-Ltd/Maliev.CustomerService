using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Maliev.CustomerService.Api.Services;

public class NDAService : INDAService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<NDAService> _logger;
    private readonly MetricsService _metricsService;

    public NDAService(CustomerDbContext context, ILogger<NDAService> logger, MetricsService metricsService)
    {
        _context = context;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<NDAResponse> CreateAsync(CreateNDARequest request, string actorId, string actorType)
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
        await _context.SaveChangesAsync();

        _logger.LogInformation("NDA {NDAId} created successfully", nda.Id);
        return MapToResponse(nda);
    }

    public async Task<NDAResponse?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Retrieving NDA {NDAId}", id);

        var nda = await _context.NDARecords
            .FirstOrDefaultAsync(n => n.Id == id);

        if (nda == null)
        {
            _logger.LogDebug("NDA {NDAId} not found", id);
            return null;
        }

        return MapToResponse(nda);
    }

    public async Task<NDAResponse> UpdateStatusAsync(Guid id, UpdateNDAStatusRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Updating NDA {NDAId} status to {Status} by actor {ActorId} ({ActorType})",
            id, request.Status, actorId, actorType);

        var nda = await _context.NDARecords
            .FirstOrDefaultAsync(n => n.Id == id);

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
            await _context.SaveChangesAsync();
            _logger.LogInformation("NDA {NDAId} status updated successfully to {Status}", id, request.Status);

            // Record NDA state transition metric
            _metricsService.RecordNdaTransition(oldStatus, request.Status);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating NDA {NDAId}", id);
            throw new InvalidOperationException("The NDA was modified by another user. Please refresh and try again.");
        }

        return MapToResponse(nda);
    }

    public async Task<int> CheckExpiredNDAsAsync()
    {
        _logger.LogInformation("Checking for expired NDAs");

        var now = DateTime.UtcNow;

        var expiredNDAs = await _context.NDARecords
            .Where(n => n.Status == NDAStatus.Signed &&
                       n.ExpiresAt.HasValue &&
                       n.ExpiresAt.Value < now)
            .ToListAsync();

        if (expiredNDAs.Count == 0)
        {
            _logger.LogInformation("No expired NDAs found");
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

        await _context.SaveChangesAsync();

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

    private NDAResponse MapToResponse(NDARecord nda)
    {
        return new NDAResponse
        {
            Id = nda.Id,
            CustomerId = nda.CustomerId,
            DocumentReferenceId = nda.DocumentReferenceId,
            Status = nda.Status,
            SignedBy = nda.SignedBy,
            SignedAt = nda.SignedAt,
            RevokedAt = nda.RevokedAt,
            ExpiresAt = nda.ExpiresAt,
            CreatedAt = nda.CreatedAt,
            UpdatedAt = nda.UpdatedAt,
            Version = nda.Version
        };
    }
}
