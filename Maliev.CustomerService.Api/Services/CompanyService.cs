using System.Text.Json;
using System.Text.RegularExpressions;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// Initializes a new instance of the CompanyService class
    /// </summary>
    /// <param name="context">Database context for Customer Service</param>
    /// <param name="logger">Logger instance</param>
    public CompanyService(CustomerDbContext context, ILogger<CompanyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new company with audit logging
    /// </summary>
    /// <param name="request">Company creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created company response</returns>
    /// <exception cref="InvalidOperationException">Thrown when VAT number format is invalid</exception>
    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
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

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Company {CompanyId} created successfully", company.Id);

        return company.ToCompanyResponse();
    }

    /// <summary>
    /// Retrieves a company by ID
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company response or null if not found</returns>
    public async Task<CompanyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving company {CompanyId}", id);

        var company = await _context.Companies
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (company == null)
        {
            _logger.LogDebug("Company {CompanyId} not found", id);
            return null;
        }

        return company.ToCompanyResponse();
    }

    /// <summary>
    /// Updates an existing company with optimistic concurrency control and audit logging
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="request">Company update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated company response</returns>
    /// <exception cref="KeyNotFoundException">Thrown when company is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when VAT number format is invalid or version conflict occurs</exception>
    public async Task<CompanyResponse> UpdateAsync(Guid id, UpdateCompanyRequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating company {CompanyId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var company = await _context.Companies
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

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
                await _context.SaveChangesAsync(cancellationToken);
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

        return company.ToCompanyResponse();
    }

    /// <summary>
    /// Retrieves a company with its associated active customers
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing company response and list of associated customer responses, or null if company not found</returns>
    public async Task<(CompanyResponse Company, List<CustomerResponse> Customers)?> GetWithCustomersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving company {CompanyId} with customers", id);

        var company = await _context.Companies
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (company == null)
        {
            _logger.LogDebug("Company {CompanyId} not found", id);
            return null;
        }

        var customers = await _context.Customers
            .Where(c => c.CompanyId == id && !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var customerResponses = customers.Select(c => c.ToCustomerResponse()).ToList();

        _logger.LogDebug("Found {CustomerCount} customers for company {CompanyId}", customerResponses.Count, id);

        return (company.ToCompanyResponse(), customerResponses);
    }

    /// <summary>
    /// Retrieves all companies with pagination and optional filtering by segment and tier
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="segment">Optional segment filter</param>
    /// <param name="tier">Optional tier filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing list of company responses and total count</returns>
    public async Task<(List<CompanyResponse> Companies, int TotalCount)> GetAllAsync(int page, int pageSize, string? segment = null, string? tier = null, CancellationToken cancellationToken = default)
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
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var companies = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var companyResponses = companies.Select(c => c.ToCompanyResponse()).ToList();

        _logger.LogDebug("Retrieved {Count} companies out of {TotalCount}", companyResponses.Count, totalCount);

        return (companyResponses, totalCount);
    }
}
