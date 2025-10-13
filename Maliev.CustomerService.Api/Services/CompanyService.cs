using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for company management operations
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<CompanyService> _logger;

    // VAT format: Country code (2 letters) followed by hyphen and digits
    private static readonly Regex VatFormatRegex = new Regex(@"^[A-Z]{2}-\d+$", RegexOptions.Compiled);

    public CompanyService(CustomerDbContext context, ILogger<CompanyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Creating company with name {Name} by actor {ActorId} ({ActorType})",
            request.Name, actorId, actorType);

        // Validate VAT format if provided
        if (!string.IsNullOrEmpty(request.VatNumber) && !VatFormatRegex.IsMatch(request.VatNumber))
        {
            _logger.LogWarning("Invalid VAT format {VatNumber}", request.VatNumber);
            throw new InvalidOperationException($"VAT number must be in format 'XX-NNNNNN' (e.g., 'TH-1234567890')");
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            VatNumber = request.VatNumber,
            RegistrationNumber = request.RegistrationNumber,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            Segment = request.Segment,
            Tier = request.Tier,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Companies.Add(company);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Create,
            EntityType = nameof(Company),
            EntityId = company.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                company.Name,
                company.VatNumber,
                company.RegistrationNumber,
                company.ContactEmail,
                company.ContactPhone,
                company.Segment,
                company.Tier
            })
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Company {CompanyId} created successfully", company.Id);

        return MapToResponse(company);
    }

    public async Task<CompanyResponse?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Retrieving company {CompanyId}", id);

        var company = await _context.Companies
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();

        if (company == null)
        {
            _logger.LogDebug("Company {CompanyId} not found", id);
            return null;
        }

        return MapToResponse(company);
    }

    public async Task<CompanyResponse> UpdateAsync(Guid id, UpdateCompanyRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Updating company {CompanyId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var company = await _context.Companies
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();

        if (company == null)
        {
            _logger.LogWarning("Company {CompanyId} not found for update", id);
            throw new KeyNotFoundException($"Company with ID '{id}' not found");
        }

        // Validate VAT format if provided
        if (!string.IsNullOrEmpty(request.VatNumber) && !VatFormatRegex.IsMatch(request.VatNumber))
        {
            _logger.LogWarning("Invalid VAT format {VatNumber}", request.VatNumber);
            throw new InvalidOperationException($"VAT number must be in format 'XX-NNNNNN' (e.g., 'TH-1234567890')");
        }

        // Store previous values for audit log
        var previousValues = new
        {
            company.Name,
            company.VatNumber,
            company.RegistrationNumber,
            company.ContactEmail,
            company.ContactPhone,
            company.Segment,
            company.Tier
        };

        // Track changed fields
        var changedFields = new Dictionary<string, object>();

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.Name) && request.Name != company.Name)
        {
            changedFields["Name"] = request.Name;
            company.Name = request.Name;
        }

        if (request.VatNumber != null && request.VatNumber != company.VatNumber)
        {
            changedFields["VatNumber"] = request.VatNumber;
            company.VatNumber = request.VatNumber;
        }

        if (request.RegistrationNumber != null && request.RegistrationNumber != company.RegistrationNumber)
        {
            changedFields["RegistrationNumber"] = request.RegistrationNumber;
            company.RegistrationNumber = request.RegistrationNumber;
        }

        if (request.ContactEmail != null && request.ContactEmail != company.ContactEmail)
        {
            changedFields["ContactEmail"] = request.ContactEmail;
            company.ContactEmail = request.ContactEmail;
        }

        if (request.ContactPhone != null && request.ContactPhone != company.ContactPhone)
        {
            changedFields["ContactPhone"] = request.ContactPhone;
            company.ContactPhone = request.ContactPhone;
        }

        if (!string.IsNullOrEmpty(request.Segment) && request.Segment != company.Segment)
        {
            changedFields["Segment"] = request.Segment;
            company.Segment = request.Segment;
        }

        if (!string.IsNullOrEmpty(request.Tier) && request.Tier != company.Tier)
        {
            changedFields["Tier"] = request.Tier;
            company.Tier = request.Tier;
        }

        if (changedFields.Count > 0)
        {
            company.UpdatedAt = DateTime.UtcNow;

            // Set the original row version for optimistic concurrency
            _context.Entry(company).Property(c => c.Version).OriginalValue = request.Version;

            // Create audit log
            var auditLog = new AuditLog
            {
                ActorId = actorId,
                ActorType = actorType,
                Action = AuditAction.Update,
                EntityType = nameof(Company),
                EntityId = company.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonSerializer.Serialize(changedFields),
                PreviousValues = JsonSerializer.Serialize(previousValues)
            };

            _context.AuditLogs.Add(auditLog);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Company {CompanyId} updated successfully with {FieldCount} field(s)",
                    id, changedFields.Count);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating company {CompanyId}", id);
                throw new InvalidOperationException("The company was modified by another user. Please refresh and try again.");
            }
        }
        else
        {
            _logger.LogInformation("No changes detected for company {CompanyId}", id);
        }

        return MapToResponse(company);
    }

    public async Task<(CompanyResponse Company, List<CustomerResponse> Customers)?> GetWithCustomersAsync(Guid id)
    {
        _logger.LogDebug("Retrieving company {CompanyId} with customers", id);

        var company = await _context.Companies
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();

        if (company == null)
        {
            _logger.LogDebug("Company {CompanyId} not found", id);
            return null;
        }

        var customers = await _context.Customers
            .Where(c => c.CompanyId == id && !c.IsDeleted)
            .ToListAsync();

        var customerResponses = customers.Select(customer => new CustomerResponse
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone,
            Segment = customer.Segment,
            Tier = customer.Tier,
            PreferredLanguage = customer.PreferredLanguage,
            Timezone = customer.Timezone,
            CommunicationPreferences = !string.IsNullOrEmpty(customer.CommunicationPreferences)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(customer.CommunicationPreferences)
                : null,
            CompanyId = customer.CompanyId,
            IsDeleted = customer.IsDeleted,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            Version = customer.Version
        }).ToList();

        _logger.LogDebug("Found {CustomerCount} customers for company {CompanyId}", customerResponses.Count, id);

        return (MapToResponse(company), customerResponses);
    }

    public async Task<(List<CompanyResponse> Companies, int TotalCount)> GetAllAsync(int page, int pageSize, string? segment = null, string? tier = null)
    {
        _logger.LogDebug("Retrieving companies - Page: {Page}, PageSize: {PageSize}, Segment: {Segment}, Tier: {Tier}",
            page, pageSize, segment ?? "all", tier ?? "all");

        var query = _context.Companies.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(segment))
        {
            query = query.Where(c => c.Segment == segment);
        }

        if (!string.IsNullOrEmpty(tier))
        {
            query = query.Where(c => c.Tier == tier);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var companies = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var companyResponses = companies.Select(MapToResponse).ToList();

        _logger.LogDebug("Retrieved {Count} companies out of {TotalCount}", companyResponses.Count, totalCount);

        return (companyResponses, totalCount);
    }

    private CompanyResponse MapToResponse(Company company)
    {
        return new CompanyResponse
        {
            Id = company.Id,
            Name = company.Name,
            VatNumber = company.VatNumber,
            RegistrationNumber = company.RegistrationNumber,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone,
            Segment = company.Segment,
            Tier = company.Tier,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt,
            Version = company.Version
        };
    }
}
