using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Services.External;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for address management operations
/// </summary>
public class AddressService : IAddressService
{
    private readonly CustomerDbContext _context;
    private readonly ICountryServiceClient _countryServiceClient;
    private readonly ILogger<AddressService> _logger;

    public AddressService(
        CustomerDbContext context,
        ICountryServiceClient countryServiceClient,
        ILogger<AddressService> logger)
    {
        _context = context;
        _countryServiceClient = countryServiceClient;
        _logger = logger;
    }

    public async Task<AddressResponse> CreateAsync(CreateAddressRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Creating address for {OwnerType} {OwnerId} by actor {ActorId} ({ActorType})",
            request.OwnerType, request.OwnerId, actorId, actorType);

        // Validate country ID via Country Service
        bool isValidCountry;
        try
        {
            isValidCountry = await _countryServiceClient.ValidateCountryIdAsync(request.CountryId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Country Service unavailable during address creation");
            throw; // Re-throw to be handled by controller
        }

        if (!isValidCountry)
        {
            _logger.LogWarning("Invalid country ID {CountryId} for address creation", request.CountryId);
            throw new InvalidOperationException($"Country ID '{request.CountryId}' is not valid");
        }

        var address = new Address
        {
            Id = Guid.NewGuid(),
            OwnerType = request.OwnerType,
            OwnerId = request.OwnerId,
            Type = request.Type,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            CountryId = request.CountryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Addresses.Add(address);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Create,
            EntityType = nameof(Address),
            EntityId = address.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                address.OwnerType,
                address.OwnerId,
                address.Type,
                address.AddressLine1,
                address.AddressLine2,
                address.City,
                address.Province,
                address.PostalCode,
                address.CountryId
            })
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Address {AddressId} created successfully", address.Id);

        return MapToResponse(address);
    }

    public async Task<List<AddressResponse>> GetByOwnerAsync(string ownerType, Guid ownerId)
    {
        _logger.LogDebug("Retrieving addresses for {OwnerType} {OwnerId}", ownerType, ownerId);

        var addresses = await _context.Addresses
            .Where(a => a.OwnerType == ownerType && a.OwnerId == ownerId)
            .OrderBy(a => a.Type)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync();

        _logger.LogDebug("Found {Count} addresses for {OwnerType} {OwnerId}",
            addresses.Count, ownerType, ownerId);

        return addresses.Select(MapToResponse).ToList();
    }

    public async Task<AddressResponse> UpdateAsync(Guid id, UpdateAddressRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Updating address {AddressId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id);

        if (address == null)
        {
            _logger.LogWarning("Address {AddressId} not found for update", id);
            throw new KeyNotFoundException($"Address with ID '{id}' not found");
        }

        // Store previous values for audit log
        var previousValues = new
        {
            address.Type,
            address.AddressLine1,
            address.AddressLine2,
            address.City,
            address.Province,
            address.PostalCode,
            address.CountryId
        };

        // Track changed fields
        var changedFields = new Dictionary<string, object>();

        // Validate country ID if it's being changed
        if (request.CountryId.HasValue && request.CountryId != address.CountryId)
        {
            bool isValidCountry;
            try
            {
                isValidCountry = await _countryServiceClient.ValidateCountryIdAsync(request.CountryId.Value);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Country Service unavailable during address update");
                throw;
            }

            if (!isValidCountry)
            {
                _logger.LogWarning("Invalid country ID {CountryId} for address update", request.CountryId);
                throw new InvalidOperationException($"Country ID '{request.CountryId}' is not valid");
            }

            changedFields["CountryId"] = request.CountryId.Value;
            address.CountryId = request.CountryId.Value;
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.Type) && request.Type != address.Type)
        {
            changedFields["Type"] = request.Type;
            address.Type = request.Type;
        }

        if (!string.IsNullOrEmpty(request.AddressLine1) && request.AddressLine1 != address.AddressLine1)
        {
            changedFields["AddressLine1"] = request.AddressLine1;
            address.AddressLine1 = request.AddressLine1;
        }

        if (request.AddressLine2 != null && request.AddressLine2 != address.AddressLine2)
        {
            changedFields["AddressLine2"] = request.AddressLine2;
            address.AddressLine2 = request.AddressLine2;
        }

        if (!string.IsNullOrEmpty(request.City) && request.City != address.City)
        {
            changedFields["City"] = request.City;
            address.City = request.City;
        }

        if (!string.IsNullOrEmpty(request.Province) && request.Province != address.Province)
        {
            changedFields["Province"] = request.Province;
            address.Province = request.Province;
        }

        if (!string.IsNullOrEmpty(request.PostalCode) && request.PostalCode != address.PostalCode)
        {
            changedFields["PostalCode"] = request.PostalCode;
            address.PostalCode = request.PostalCode;
        }

        if (changedFields.Count > 0)
        {
            address.UpdatedAt = DateTime.UtcNow;

            // Set the original row version for optimistic concurrency
            _context.Entry(address).Property(a => a.Version).OriginalValue = request.Version;

            // Create audit log
            var auditLog = new AuditLog
            {
                ActorId = actorId,
                ActorType = actorType,
                Action = AuditAction.Update,
                EntityType = nameof(Address),
                EntityId = address.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonSerializer.Serialize(changedFields),
                PreviousValues = JsonSerializer.Serialize(previousValues)
            };

            _context.AuditLogs.Add(auditLog);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Address {AddressId} updated successfully with {FieldCount} field(s)",
                    id, changedFields.Count);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating address {AddressId}", id);
                throw new InvalidOperationException("The address was modified by another user. Please refresh and try again.");
            }
        }
        else
        {
            _logger.LogInformation("No changes detected for address {AddressId}", id);
        }

        return MapToResponse(address);
    }

    public async Task<bool> DeleteAsync(Guid id, string actorId, string actorType)
    {
        _logger.LogInformation("Deleting address {AddressId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id);

        if (address == null)
        {
            _logger.LogDebug("Address {AddressId} not found for deletion", id);
            return false;
        }

        _context.Addresses.Remove(address);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Delete,
            EntityType = nameof(Address),
            EntityId = address.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            PreviousValues = JsonSerializer.Serialize(new
            {
                address.OwnerType,
                address.OwnerId,
                address.Type,
                address.AddressLine1,
                address.AddressLine2,
                address.City,
                address.Province,
                address.PostalCode,
                address.CountryId
            })
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Address {AddressId} deleted successfully", id);

        return true;
    }

    private AddressResponse MapToResponse(Address address)
    {
        return new AddressResponse
        {
            Id = address.Id,
            OwnerType = address.OwnerType,
            OwnerId = address.OwnerId,
            Type = address.Type,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            Province = address.Province,
            PostalCode = address.PostalCode,
            CountryId = address.CountryId,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt,
            Version = address.Version
        };
    }
}
