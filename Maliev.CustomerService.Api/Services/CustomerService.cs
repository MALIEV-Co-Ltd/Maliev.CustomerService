using System.Text.Json;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for customer management operations
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly CustomerDbContext _context;
    private readonly IIAMClient _iamClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CustomerService> _logger;
    private readonly MetricsService _metricsService;

    /// <summary>
    /// Initializes a new instance of the CustomerService class
    /// </summary>
    /// <param name="context">Database context for Customer Service</param>
    /// <param name="iamClient">IAM service client</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="metricsService">Metrics service for recording customer operations</param>
    public CustomerService(
        CustomerDbContext context,
        IIAMClient iamClient,
        IConfiguration configuration,
        ILogger<CustomerService> logger,
        MetricsService metricsService)
    {
        _context = context;
        _iamClient = iamClient;
        _configuration = configuration;
        _logger = logger;
        _metricsService = metricsService;
    }

    /// <summary>
    /// Creates a new customer with audit logging
    /// </summary>
    /// <param name="request">Customer creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <returns>Created customer response</returns>
    /// <exception cref="InvalidOperationException">Thrown when duplicate email is found for active customer</exception>
    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Creating customer with email {Email} by actor {ActorId} ({ActorType})",
            request.Email, actorId, actorType);

        // Race Condition Handling
        // =======================
        // The duplicate email check below is NOT thread-safe. Concurrent requests could both:
        // 1. Query and find no existing customer with the email
        // 2. Pass the validation check
        // 3. Both attempt to insert a customer with the same email
        //
        // This is SAFE and HANDLED because:
        // - Database has a unique constraint on the Email column (see CustomerConfiguration.cs)
        // - One request will succeed, the other will get a database constraint violation
        // - ExceptionHandlingMiddleware detects Npgsql.PostgresException (error code 23505)
        // - Automatically maps to 409 Conflict with "A record with this email already exists"
        // - This is a rare edge case (requires precise timing of concurrent requests)
        //
        // The application-level check below provides fast-fail for the common case,
        // while the database constraint provides ultimate safety for race conditions.
        //
        // Reviewed: 2025-12-26 - Race condition is HANDLED by DB constraint + middleware

        // Check for duplicate email (active customers only)
        var existingCustomer = await _context.Customers
            .Where(c => c.Email == request.Email && !c.IsDeleted)
            .FirstOrDefaultAsync();

        if (existingCustomer != null)
        {
            _logger.LogWarning("Duplicate email {Email} for active customer", request.Email);
            throw new InvalidOperationException($"A customer with email '{request.Email}' already exists");
        }

        _logger.LogInformation("Creating IAM principal for new customer with email {Email}", request.Email);
        Guid principalId;
        try
        {
            var principalResponse = await _iamClient.CreatePrincipalAsync(new CreatePrincipalRequest
            {
                Email = request.Email,
                DisplayName = $"{request.FirstName} {request.LastName}",
                PrincipalType = "user",
                LinkedService = "CustomerService"
            });
            principalId = principalResponse.PrincipalId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create IAM principal for email {Email}", request.Email);
            throw new InvalidOperationException("Failed to create customer identity in central system", ex);
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Segment = request.Segment,
            Tier = request.Tier,
            PreferredLanguage = request.PreferredLanguage,
            Timezone = request.Timezone,
            CommunicationPreferences = request.CommunicationPreferences != null
                ? JsonSerializer.Serialize(request.CommunicationPreferences)
                : null,
            CompanyId = request.CompanyId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Create,
            EntityType = nameof(Customer),
            EntityId = customer.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                customer.FirstName,
                customer.LastName,
                customer.Email,
                customer.Phone,
                customer.Segment,
                customer.Tier,
                customer.PreferredLanguage,
                customer.Timezone,
                customer.CommunicationPreferences,
                customer.CompanyId
            })
        };

        _context.AuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Customer {CustomerId} created successfully", customer.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save customer to database. Attempting to delete orphaned IAM principal {PrincipalId}", principalId);

            // Compensation: Delete the IAM principal to avoid orphaned identity
            await _iamClient.DeletePrincipalAsync(principalId);

            throw new InvalidOperationException("Failed to create customer due to database error. IAM principal has been cleaned up.", ex);
        }

        // Record metrics
        _metricsService.RecordCustomerRegistration(customer.Segment);

        return customer.ToCustomerResponse();
    }

    /// <summary>
    /// Retrieves a customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer response or null if not found</returns>
    public async Task<CustomerResponse?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Retrieving customer {CustomerId}", id);

        var customer = await _context.Customers
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync();

        if (customer == null)
        {
            _logger.LogDebug("Customer {CustomerId} not found or deleted", id);
            return null;
        }

        return customer.ToCustomerResponse();
    }

    /// <inheritdoc/>
    public async Task<CustomerResponse?> GetByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving customer by Principal ID {PrincipalId}", principalId);

        var customer = await _context.Customers
            .Where(c => c.PrincipalId == principalId && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (customer == null)
        {
            _logger.LogDebug("Customer with Principal ID {PrincipalId} not found", principalId);
            return null;
        }

        return customer.ToCustomerResponse();
    }

    /// <summary>
    /// Updates an existing customer with optimistic concurrency control and audit logging
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="request">Customer update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <returns>Updated customer response</returns>
    /// <exception cref="KeyNotFoundException">Customer not found</exception>
    /// <exception cref="InvalidOperationException">Version conflict or duplicate email</exception>
    public async Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Updating customer {CustomerId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var customer = await _context.Customers
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync();

        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found for update", id);
            throw new KeyNotFoundException($"Customer with ID '{id}' not found");
        }

        // Store previous values for audit log
        var previousValues = new
        {
            customer.FirstName,
            customer.LastName,
            customer.Email,
            customer.Phone,
            customer.Segment,
            customer.Tier,
            customer.PreferredLanguage,
            customer.Timezone,
            customer.CommunicationPreferences,
            customer.CompanyId
        };

        // Track changed fields
        var changedFields = new Dictionary<string, object>();

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.FirstName) && request.FirstName != customer.FirstName)
        {
            changedFields["FirstName"] = request.FirstName;
            customer.FirstName = request.FirstName;
        }

        if (!string.IsNullOrEmpty(request.LastName) && request.LastName != customer.LastName)
        {
            changedFields["LastName"] = request.LastName;
            customer.LastName = request.LastName;
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != customer.Email)
        {
            // Check for duplicate email
            var existingCustomer = await _context.Customers
                .Where(c => c.Email == request.Email && c.Id != id && !c.IsDeleted)
                .FirstOrDefaultAsync();

            if (existingCustomer != null)
            {
                _logger.LogWarning("Duplicate email {Email} for customer update", request.Email);
                throw new InvalidOperationException($"A customer with email '{request.Email}' already exists");
            }

            changedFields["Email"] = request.Email;
            customer.Email = request.Email;
        }

        if (request.Phone != null && request.Phone != customer.Phone)
        {
            changedFields["Phone"] = request.Phone;
            customer.Phone = request.Phone;
        }

        if (!string.IsNullOrEmpty(request.Segment) && request.Segment != customer.Segment)
        {
            changedFields["Segment"] = request.Segment;
            customer.Segment = request.Segment;
        }

        if (!string.IsNullOrEmpty(request.Tier) && request.Tier != customer.Tier)
        {
            changedFields["Tier"] = request.Tier;
            customer.Tier = request.Tier;
        }

        if (!string.IsNullOrEmpty(request.PreferredLanguage) && request.PreferredLanguage != customer.PreferredLanguage)
        {
            changedFields["PreferredLanguage"] = request.PreferredLanguage;
            customer.PreferredLanguage = request.PreferredLanguage;
        }

        if (!string.IsNullOrEmpty(request.Timezone) && request.Timezone != customer.Timezone)
        {
            changedFields["Timezone"] = request.Timezone;
            customer.Timezone = request.Timezone;
        }

        if (request.CommunicationPreferences != null)
        {
            var newPrefs = JsonSerializer.Serialize(request.CommunicationPreferences);
            if (newPrefs != customer.CommunicationPreferences)
            {
                changedFields["CommunicationPreferences"] = request.CommunicationPreferences;
                customer.CommunicationPreferences = newPrefs;
            }
        }

        if (request.CompanyId.HasValue && request.CompanyId != customer.CompanyId)
        {
            changedFields["CompanyId"] = request.CompanyId;
            customer.CompanyId = request.CompanyId;
        }

        if (changedFields.Count > 0)
        {
            customer.UpdatedAt = DateTime.UtcNow;

            // Set the original row version for optimistic concurrency
            _context.Entry(customer).Property(c => c.Version).OriginalValue = request.Version;

            // Create audit log
            var auditLog = new AuditLog
            {
                ActorId = actorId,
                ActorType = actorType,
                Action = AuditAction.Update,
                EntityType = nameof(Customer),
                EntityId = customer.Id.ToString(),
                Timestamp = DateTime.UtcNow,
                ChangedFields = JsonSerializer.Serialize(changedFields),
                PreviousValues = JsonSerializer.Serialize(previousValues)
            };

            _context.AuditLogs.Add(auditLog);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Customer {CustomerId} updated successfully with {FieldCount} field(s)",
                    id, changedFields.Count);

                // Record metrics
                _metricsService.RecordCustomerUpdate(actorType);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating customer {CustomerId}", id);
                throw new InvalidOperationException("The customer was modified by another user. Please refresh and try again.");
            }
        }
        else
        {
            _logger.LogInformation("No changes detected for customer {CustomerId}", id);
        }

        return customer.ToCustomerResponse();
    }

    /// <summary>
    /// Soft deletes a customer with audit logging
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <returns>True if deleted, false if not found</returns>
    public async Task<bool> SoftDeleteAsync(Guid id, string actorId, string actorType)
    {
        _logger.LogInformation("Soft deleting customer {CustomerId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var customer = await _context.Customers
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync();

        if (customer == null)
        {
            _logger.LogDebug("Customer {CustomerId} not found for deletion", id);
            return false;
        }

        customer.IsDeleted = true;
        customer.UpdatedAt = DateTime.UtcNow;

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.SoftDelete,
            EntityType = nameof(Customer),
            EntityId = customer.Id.ToString(),
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer {CustomerId} soft deleted successfully", id);

        return true;
    }

    /// <summary>
    /// Gets all customers with optional filtering and pagination
    /// </summary>
    /// <param name="segment">Optional segment filter</param>
    /// <param name="tier">Optional tier filter</param>
    /// <param name="preferredLanguage">Optional preferred language filter</param>
    /// <param name="email">Optional email partial match filter</param>
    /// <param name="companyId">Optional company ID filter</param>
    /// <param name="createdFrom">Optional filter for customers created after this date</param>
    /// <param name="createdTo">Optional filter for customers created before this date</param>
    /// <param name="includeDeleted">Include soft-deleted customers in results</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated response containing customer list and pagination metadata</returns>
    public async Task<PaginatedResponse<CustomerResponse>> GetAllAsync(
        string? segment = null,
        string? tier = null,
        string? preferredLanguage = null,
        string? email = null,
        Guid? companyId = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        bool includeDeleted = false,
        int page = 1,
        int pageSize = 50)
    {
        _logger.LogDebug("Querying customers with filters: segment={Segment}, tier={Tier}, language={Language}, email={Email}, companyId={CompanyId}, page={Page}, pageSize={PageSize}",
            segment, tier, preferredLanguage, email, companyId, page, pageSize);

        // Log for integration tracking (T125)
        if (!string.IsNullOrEmpty(segment) || !string.IsNullOrEmpty(tier))
        {
            _logger.LogDebug("Segmentation query: segment={Segment}, tier={Tier} (potential downstream service usage)",
                segment, tier);
        }

        // Base query with isDeleted filter (T126, T128)
        var query = _context.Customers
            .Where(c => includeDeleted || !c.IsDeleted)
            .AsQueryable();

        // Apply filters (T119, T126)
        if (!string.IsNullOrEmpty(segment))
        {
            query = query.Where(c => c.Segment == segment);
        }

        if (!string.IsNullOrEmpty(tier))
        {
            query = query.Where(c => c.Tier == tier);
        }

        if (!string.IsNullOrEmpty(preferredLanguage))
        {
            query = query.Where(c => c.PreferredLanguage == preferredLanguage);
        }

        // Email partial match using LIKE (T126, T128)
        if (!string.IsNullOrEmpty(email))
        {
            query = query.Where(c => EF.Functions.Like(c.Email, $"%{email}%"));
        }

        // Filter by company ID (T126)
        if (companyId.HasValue)
        {
            query = query.Where(c => c.CompanyId == companyId.Value);
        }

        // Filter by creation date range (T126, T128)
        if (createdFrom.HasValue)
        {
            query = query.Where(c => c.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            query = query.Where(c => c.CreatedAt <= createdTo.Value);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var skip = (page - 1) * pageSize;

        // Get page data with default ordering by created_at DESC
        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogDebug("Retrieved {Count} customers (page {Page}/{TotalPages}, total {TotalCount})",
            customers.Count, page, totalPages, totalCount);

        return new PaginatedResponse<CustomerResponse>
        {
            Items = customers.Select(c => c.ToCustomerResponse()).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// Gets customer preferences for compliance/audit purposes
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated response containing customer preferences</returns>
    public async Task<PaginatedResponse<GetCustomerPreferencesResponse>> GetPreferencesAsync(
        int page = 1,
        int pageSize = 100)
    {
        _logger.LogDebug("Querying customer preferences for compliance/audit: page={Page}, pageSize={PageSize}",
            page, pageSize);

        var query = _context.Customers
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Calculate pagination
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var skip = (page - 1) * pageSize;

        // Get page data
        var customers = await query
            .OrderBy(c => c.Email)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogDebug("Retrieved {Count} customer preferences (page {Page}/{TotalPages}, total {TotalCount})",
            customers.Count, page, totalPages, totalCount);

        return new PaginatedResponse<GetCustomerPreferencesResponse>
        {
            Items = customers.Select(c => new GetCustomerPreferencesResponse
            {
                Id = c.Id,
                Email = c.Email,
                Segment = c.Segment,
                Tier = c.Tier,
                PreferredLanguage = c.PreferredLanguage,
                Timezone = c.Timezone,
                CommunicationPreferences = !string.IsNullOrEmpty(c.CommunicationPreferences)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(c.CommunicationPreferences)
                    : null
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }
}
