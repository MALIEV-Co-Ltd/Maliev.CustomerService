using System.Text.Json;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Services.External;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for address management operations
/// </summary>
public class AddressService : IAddressService
{
    private readonly CustomerDbContext _context;
    private readonly ICountryServiceClient _countryServiceClient;
    private readonly ILogger<AddressService> _logger;

    /// <summary>
    /// Initializes a new instance of the AddressService class
    /// </summary>
    /// <param name="context">Database context for Customer Service</param>
    /// <param name="countryServiceClient">Client for Country Service validation</param>
    /// <param name="logger">Logger instance</param>
    public AddressService(
        CustomerDbContext context,
        ICountryServiceClient countryServiceClient,
        ILogger<AddressService> logger)
    {
        _context = context;
        _countryServiceClient = countryServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new address with country validation and audit logging
    /// </summary>
    /// <param name="request">Address creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created address response</returns>
    /// <exception cref="InvalidOperationException">Country Service unavailable or invalid country ID</exception>
    public async Task<AddressResponse> CreateAsync(CreateAddressRequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
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
            IsDefault = request.IsDefault,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            AddressLine3 = request.AddressLine3,
            District = request.District,
            City = request.City,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode,
            CountryId = request.CountryId,
            RecipientName = request.RecipientName,
            RecipientPhone = request.RecipientPhone,
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
                address.IsDefault,
                address.AddressLine1,
                address.AddressLine2,
                address.AddressLine3,
                address.District,
                address.City,
                address.StateProvince,
                address.PostalCode,
                address.CountryId,
                address.RecipientName,
                address.RecipientPhone
            })
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Address {AddressId} created successfully", address.Id);

        return address.ToAddressResponse();
    }

    /// <summary>
    /// Retrieves all addresses for a specific owner
    /// </summary>
    /// <param name="ownerType">Type of owner (Customer or Company)</param>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of addresses</returns>
    public async Task<List<AddressResponse>> GetByOwnerAsync(string ownerType, Guid ownerId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving addresses for {OwnerType} {OwnerId}", ownerType, ownerId);

        var addresses = await _context.Addresses
            .Where(a => a.OwnerId == ownerId && a.OwnerType.ToLower() == ownerType.ToLower())
            .OrderBy(a => a.Type)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return addresses.Select(a => a.ToAddressResponse()).ToList();
    }

    /// <summary>
    /// Updates an existing address with optimistic concurrency control, country validation, and audit logging
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="request">Address update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated address response</returns>
    /// <exception cref="KeyNotFoundException">Address not found</exception>
    /// <exception cref="InvalidOperationException">Country Service unavailable or invalid country ID, or version conflict</exception>
    public async Task<AddressResponse> UpdateAsync(Guid id, UpdateAddressRequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating address {AddressId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (address == null)
        {
            _logger.LogWarning("Address {AddressId} not found for update", id);
            throw new KeyNotFoundException($"Address with ID '{id}' not found");
        }

        // Store previous values for audit log
        var previousValues = new
        {
            address.Type,
            address.IsDefault,
            address.AddressLine1,
            address.AddressLine2,
            address.AddressLine3,
            address.District,
            address.City,
            address.StateProvince,
            address.PostalCode,
            address.CountryId,
            address.RecipientName,
            address.RecipientPhone
        };

        // Track changed fields
        var changedFields = new Dictionary<string, object>();

        if (request.Type != null && request.Type != address.Type)
        {
            changedFields["Type"] = request.Type;
            address.Type = request.Type;
        }

        if (request.IsDefault.HasValue && request.IsDefault != address.IsDefault)
        {
            changedFields["IsDefault"] = request.IsDefault.Value;
            address.IsDefault = request.IsDefault.Value;
        }

        if (request.AddressLine1 != null && request.AddressLine1 != address.AddressLine1)
        {
            changedFields["AddressLine1"] = request.AddressLine1;
            address.AddressLine1 = request.AddressLine1;
        }

        if (request.AddressLine2 != null && request.AddressLine2 != address.AddressLine2)
        {
            changedFields["AddressLine2"] = request.AddressLine2;
            address.AddressLine2 = request.AddressLine2;
        }

        if (request.AddressLine3 != null && request.AddressLine3 != address.AddressLine3)
        {
            changedFields["AddressLine3"] = request.AddressLine3;
            address.AddressLine3 = request.AddressLine3;
        }

        if (request.District != null && request.District != address.District)
        {
            changedFields["District"] = request.District;
            address.District = request.District;
        }

        if (request.City != null && request.City != address.City)
        {
            changedFields["City"] = request.City;
            address.City = request.City;
        }

        if (request.StateProvince != null && request.StateProvince != address.StateProvince)
        {
            changedFields["StateProvince"] = request.StateProvince;
            address.StateProvince = request.StateProvince;
        }

        if (request.CountryId.HasValue && request.CountryId != address.CountryId)
        {
            // Validate country ID via Country Service
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
                _logger.LogWarning("Invalid country ID {CountryId} for address update", request.CountryId.Value);
                throw new InvalidOperationException($"Country ID '{request.CountryId.Value}' is not valid");
            }

            changedFields["CountryId"] = request.CountryId.Value;
            address.CountryId = request.CountryId.Value;
        }

        if (!string.IsNullOrEmpty(request.PostalCode) && request.PostalCode != address.PostalCode)

        {
            changedFields["PostalCode"] = request.PostalCode;
            address.PostalCode = request.PostalCode;
        }

        if (request.RecipientName != null && request.RecipientName != address.RecipientName)
        {
            changedFields["RecipientName"] = request.RecipientName;
            address.RecipientName = request.RecipientName;
        }

        if (request.RecipientPhone != null && request.RecipientPhone != address.RecipientPhone)
        {
            changedFields["RecipientPhone"] = request.RecipientPhone;
            address.RecipientPhone = request.RecipientPhone;
        }

        if (changedFields.Count > 0)
        {
            if (address.IsDefault)
            {
                await EnsureSingleDefaultAsync(address.OwnerType, address.OwnerId, address.Type, address.Id, cancellationToken);
            }

            address.UpdatedAt = DateTime.UtcNow;


            // Set the original xmin for optimistic concurrency
            _context.Entry(address).Property(a => a.xmin).OriginalValue = request.xmin;

            // Create audit log
            var auditLog = new AuditLog
            {
                ActorId = actorId,
                ActorType = actorType,
                Action = AuditAction.Update,
                EntityType = nameof(Address),
                EntityId = address.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonSerializer.Serialize(new { Fields = changedFields, address.OwnerId, address.OwnerType }),
                PreviousValues = JsonSerializer.Serialize(new { Fields = previousValues, address.OwnerId, address.OwnerType })
            };

            _context.AuditLogs.Add(auditLog);

            try
            {
                if (address.IsDefault)
                {
                    await EnsureSingleDefaultAsync(address.OwnerType, address.OwnerId, address.Type, address.Id, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);

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

        return address.ToAddressResponse();
    }

    /// <summary>
    /// Deletes an address with audit logging
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="xmin">PostgreSQL xmin for concurrency control</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when version conflict occurs</exception>
    public async Task<bool> DeleteAsync(Guid id, uint xmin, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting address {AddressId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (address == null)
        {
            _logger.LogDebug("Address {AddressId} not found for deletion", id);
            return false;
        }

        _context.Entry(address).Property(a => a.xmin).OriginalValue = xmin;

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
                address.IsDefault,
                address.AddressLine1,
                address.AddressLine2,
                address.AddressLine3,
                address.District,
                address.City,
                address.StateProvince,
                address.PostalCode,
                address.CountryId,
                address.RecipientName,
                address.RecipientPhone
            })
        };

        _context.AuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Address {AddressId} deleted successfully", id);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict deleting address {AddressId}", id);
            throw new InvalidOperationException("The address was modified by another user. Please refresh and try again.");
        }
    }

    private async Task EnsureSingleDefaultAsync(string ownerType, Guid ownerId, string addressType, Guid currentAddressId, CancellationToken ct)
    {
        var otherDefaults = await _context.Addresses
            .Where(a => a.OwnerId == ownerId &&
                        a.OwnerType.ToLower() == ownerType.ToLower() &&
                        a.Type == addressType &&
                        a.IsDefault &&
                        a.Id != currentAddressId)
            .ToListAsync(ct);

        if (otherDefaults.Any())
        {
            _logger.LogInformation("Resetting {Count} existing default {AddressType} addresses for {OwnerType} {OwnerId}",
                otherDefaults.Count, addressType, ownerType, ownerId);

            foreach (var other in otherDefaults)
            {
                other.IsDefault = false;
                other.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
