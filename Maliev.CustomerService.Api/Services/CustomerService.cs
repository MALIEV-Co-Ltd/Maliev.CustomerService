using System.Text.Json;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Maliev.MessagingContracts.Generated;
using MassTransit;
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
    private readonly IPublishEndpoint _publishEndpoint;

    /// <summary>
    /// Initializes a new instance of the CustomerService class
    /// </summary>
    /// <param name="context">Database context for Customer Service</param>
    /// <param name="iamClient">IAM service client</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="metricsService">Metrics service for recording customer operations</param>
    /// <param name="publishEndpoint">MassTransit publish endpoint for domain events</param>
    public CustomerService(
        CustomerDbContext context,
        IIAMClient iamClient,
        IConfiguration configuration,
        ILogger<CustomerService> logger,
        MetricsService metricsService,
        IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _iamClient = iamClient;
        _configuration = configuration;
        _logger = logger;
        _metricsService = metricsService;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Creates a new customer with audit logging
    /// </summary>
    /// <param name="request">Customer creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created customer response</returns>
    /// <exception cref="InvalidOperationException">Thrown when duplicate email is found for active customer</exception>
    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating customer with email {Email} by actor {ActorId} ({ActorType})",
            request.Email, actorId, actorType);

        // Race condition for duplicate emails is handled by a unique database constraint
        // on the Email column (see CustomerConfiguration.cs) and caught by ExceptionHandlingMiddleware.
        // This application-level check provides a fast-fail for common cases.

        // Check for duplicate email (active customers only)
        var existingCustomer = await _context.Customers
            .Where(c => c.Email == request.Email && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingCustomer != null)
        {
            _logger.LogWarning("Duplicate email {Email} for active customer", request.Email);
            throw new InvalidOperationException($"A customer with email '{request.Email}' already exists");
        }

        _logger.LogInformation("Creating IAM principal for new customer with email {Email}", request.Email);
        Guid principalId;
        bool isNewPrincipal = false;
        try
        {
            // Check if principal already exists (e.g. for employees)
            var existingPrincipal = await _iamClient.GetPrincipalByEmailAsync(request.Email, cancellationToken);
            if (existingPrincipal != null)
            {
                _logger.LogInformation("Using existing IAM principal {PrincipalId} for email {Email}", existingPrincipal.PrincipalId, request.Email);
                principalId = existingPrincipal.PrincipalId;
            }
            else
            {
                var principalResponse = await _iamClient.CreatePrincipalAsync(new CreatePrincipalRequest
                {
                    Email = request.Email,
                    DisplayName = $"{request.FirstName} {request.LastName}",
                    PrincipalType = "user",
                    LinkedService = "CustomerService"
                }, cancellationToken);
                principalId = principalResponse.PrincipalId;
                isNewPrincipal = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or retrieve IAM principal for email {Email}", request.Email);
            throw new InvalidOperationException("Failed to create customer identity in central system", ex);
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Mobile = request.Mobile,
            Extension = request.Extension,
            Landline = request.Landline,
            Segment = request.Segment,
            Tier = request.Tier,
            PreferredLanguage = request.PreferredLanguage,
            Timezone = request.Timezone,
            CommunicationPreferences = request.CommunicationPreferences != null
                ? JsonSerializer.Serialize(request.CommunicationPreferences)
                : null,
            CompanyId = request.CompanyId,
            UsesCompanyBillingAddress = request.UsesCompanyBillingAddress,
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
                customer.Mobile,
                customer.Extension,
                customer.Landline,
                customer.Segment,
                customer.Tier,
                customer.PreferredLanguage,
                customer.Timezone,
                customer.CommunicationPreferences,
                customer.CompanyId,
                customer.UsesCompanyBillingAddress
            })
        };

        _context.AuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Customer {CustomerId} created successfully", customer.Id);
        }
        catch (Exception ex)
        {
            if (isNewPrincipal)
            {
                _logger.LogError(ex, "Failed to save customer to database. Attempting to delete orphaned IAM principal {PrincipalId}", principalId);
                // Compensation: Delete the IAM principal to avoid orphaned identity
                await _iamClient.DeletePrincipalAsync(principalId, cancellationToken);
            }
            else
            {
                _logger.LogError(ex, "Failed to save customer to database. Keeping existing IAM principal {PrincipalId}", principalId);
            }

            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException($"Failed to create customer due to database error: {innerMessage}", ex);
        }

        // Record metrics
        _metricsService.RecordCustomerRegistration(customer.Segment);

        // Publish CustomerCreatedEvent
        await _publishEndpoint.Publish(new CustomerCreatedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "CustomerCreatedEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "CustomerService",
            ConsumedBy: ["NotificationService", "AnalyticsService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new CustomerCreatedEventPayload(
                CustomerId: customer.Id,
                PrincipalId: customer.PrincipalId,
                FirstName: customer.FirstName,
                LastName: customer.LastName,
                Email: customer.Email,
                Mobile: customer.Mobile,
                Extension: customer.Extension,
                Landline: customer.Landline,
                Segment: customer.Segment,
                Tier: customer.Tier,
                CompanyId: customer.CompanyId,
                CreatedAt: new DateTimeOffset(customer.CreatedAt, TimeSpan.Zero)
            )
        ), cancellationToken);

        _logger.LogInformation("Published CustomerCreatedEvent for customer {CustomerId}", customer.Id);

        return customer.ToCustomerResponse();
    }

    /// <summary>
    /// Retrieves a customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer response or null if not found</returns>
    public async Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving customer {CustomerId}", id);

        var customer = await _context.Customers
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (customer == null)
        {
            _logger.LogDebug("Customer {CustomerId} not found or deleted", id);
            return null;
        }

        Company? company = null;
        if (customer.CompanyId.HasValue)
        {
            company = await _context.Companies
                .Where(c => c.Id == customer.CompanyId.Value)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var nda = await _context.NDARecords
            .Where(n => n.CustomerId == id)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Fetch creator from audit logs (T240)
        var creationAudit = await _context.AuditLogs
            .Where(a => a.EntityType == nameof(Customer) && a.EntityId == id.ToString() && a.Action == AuditAction.Create)
            .FirstOrDefaultAsync(cancellationToken);

        var response = customer.ToCustomerResponse(company, nda);

        if (creationAudit != null && Guid.TryParse(creationAudit.ActorId, out var creatorPrincipalId))
        {
            var creator = await _iamClient.GetPrincipalByIdAsync(creatorPrincipalId, cancellationToken);
            if (creator != null)
            {
                response.CreatedBy = creator.PrincipalId.ToString();
                response.CreatedByName = creator.DisplayName;
                response.CreatedByEmail = creator.Email;
            }
            else
            {
                response.CreatedBy = creationAudit.ActorId;
            }
        }
        else
        {
            response.CreatedBy = creationAudit?.ActorId ?? "System";
        }

        return response;
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

        Company? company = null;
        if (customer.CompanyId.HasValue)
        {
            company = await _context.Companies
                .Where(c => c.Id == customer.CompanyId.Value)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var nda = await _context.NDARecords
            .Where(n => n.CustomerId == customer.Id)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Fetch creator from audit logs (T240)
        var creationAudit = await _context.AuditLogs
            .Where(a => a.EntityType == nameof(Customer) && a.EntityId == customer.Id.ToString() && a.Action == AuditAction.Create)
            .FirstOrDefaultAsync(cancellationToken);

        var response = customer.ToCustomerResponse(company, nda);

        if (creationAudit != null && Guid.TryParse(creationAudit.ActorId, out var creatorPrincipalId))
        {
            var creator = await _iamClient.GetPrincipalByIdAsync(creatorPrincipalId, cancellationToken);
            if (creator != null)
            {
                response.CreatedBy = creator.PrincipalId.ToString();
                response.CreatedByName = creator.DisplayName;
                response.CreatedByEmail = creator.Email;
            }
            else
            {
                response.CreatedBy = creationAudit.ActorId;
            }
        }
        else
        {
            response.CreatedBy = creationAudit?.ActorId ?? "System";
        }

        return response;
    }

    /// <summary>
    /// Updates an existing customer with optimistic concurrency control and audit logging
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="request">Customer update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated customer response</returns>
    /// <exception cref="KeyNotFoundException">Customer not found</exception>
    /// <exception cref="InvalidOperationException">Version conflict or duplicate email</exception>
    public async Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating customer {CustomerId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var customer = await _context.Customers
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

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
            customer.Mobile,
            customer.Extension,
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
                .FirstOrDefaultAsync(cancellationToken);

            if (existingCustomer != null)
            {
                _logger.LogWarning("Duplicate email {Email} for customer update", request.Email);
                throw new InvalidOperationException($"A customer with email '{request.Email}' already exists");
            }

            changedFields["Email"] = request.Email;
            customer.Email = request.Email;
        }

        if (request.Mobile != null && request.Mobile != customer.Mobile)
        {
            changedFields["Mobile"] = request.Mobile;
            customer.Mobile = request.Mobile;
        }

        if (request.Extension != null && request.Extension != customer.Extension)
        {
            changedFields["Extension"] = request.Extension;
            customer.Extension = request.Extension;
        }

        if (request.Landline != null && request.Landline != customer.Landline)
        {
            changedFields["Landline"] = request.Landline;
            customer.Landline = request.Landline;
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
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Customer {CustomerId} updated successfully with {FieldCount} field(s)",
                    id, changedFields.Count);

                // Record metrics
                _metricsService.RecordCustomerUpdate(actorType);

                // Publish CustomerUpdatedEvent
                await _publishEndpoint.Publish(new CustomerUpdatedEvent(
                    MessageId: Guid.NewGuid(),
                    MessageName: "CustomerUpdatedEvent",
                    MessageType: MessageType.Event,
                    MessageVersion: "1.0.0",
                    PublishedBy: "CustomerService",
                    ConsumedBy: ["NotificationService", "AnalyticsService"],
                    CorrelationId: Guid.NewGuid(),
                    CausationId: null,
                    OccurredAtUtc: DateTimeOffset.UtcNow,
                    IsPublic: false,
                    Payload: new CustomerUpdatedEventPayload(
                        CustomerId: customer.Id,
                        UpdatedFields: changedFields,
                        UpdatedBy: actorId,
                        ActorType: actorType,
                        UpdatedAt: new DateTimeOffset(customer.UpdatedAt, TimeSpan.Zero)
                    )
                ), cancellationToken);

                _logger.LogInformation("Published CustomerUpdatedEvent for customer {CustomerId}", id);
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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    public async Task<bool> SoftDeleteAsync(Guid id, string actorId, string actorType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Soft deleting customer {CustomerId} by actor {ActorId} ({ActorType})",
            id, actorId, actorType);

        var customer = await _context.Customers
            .Where(c => c.Id == id && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

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

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Customer {CustomerId} soft deleted successfully", id);

        // Publish CustomerDeletedEvent
        await _publishEndpoint.Publish(new CustomerDeletedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "CustomerDeletedEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "CustomerService",
            ConsumedBy: ["NotificationService", "AnalyticsService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new CustomerDeletedEventPayload(
                CustomerId: customer.Id,
                DeletedBy: actorId,
                ActorType: actorType,
                DeletedAt: new DateTimeOffset(customer.UpdatedAt, TimeSpan.Zero)
            )
        ), cancellationToken);

        _logger.LogInformation("Published CustomerDeletedEvent for customer {CustomerId}", id);

        return true;
    }

    /// <summary>
    /// Gets all customers with optional filtering and pagination
    /// </summary>
    /// <param name="query">Optional search query for name or email</param>
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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated response containing customer list and pagination metadata</returns>
    public async Task<PaginatedResponse<CustomerResponse>> GetAllAsync(
        string? query = null,
        string? segment = null,
        string? tier = null,
        string? preferredLanguage = null,
        string? email = null,
        Guid? companyId = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        bool includeDeleted = false,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying customers with filters: query={Query}, segment={Segment}, tier={Tier}, language={Language}, email={Email}, companyId={CompanyId}, page={Page}, pageSize={PageSize}",
            query, segment, tier, preferredLanguage, email, companyId, page, pageSize);

        // Log for integration tracking (T125)
        if (!string.IsNullOrEmpty(segment) || !string.IsNullOrEmpty(tier))
        {
            _logger.LogDebug("Segmentation query: segment={Segment}, tier={Tier} (potential downstream service usage)",
                segment, tier);
        }

        // Base query with isDeleted filter (T126, T128)
        var customersQuery = _context.Customers
            .Where(c => includeDeleted || !c.IsDeleted)
            .AsQueryable();

        // Apply search query (T126, T128)
        if (!string.IsNullOrEmpty(query))
        {
            var searchTerm = $"%{query}%";
            customersQuery = customersQuery.Where(c =>
                EF.Functions.ILike(c.FirstName, searchTerm) ||
                EF.Functions.ILike(c.LastName, searchTerm) ||
                EF.Functions.ILike(c.Email, searchTerm) ||
                (c.FirstName + " " + c.LastName).Contains(query)); // Fallback for name concat
        }

        // Apply filters (T119, T126)
        if (!string.IsNullOrEmpty(segment))
        {
            customersQuery = customersQuery.Where(c => c.Segment == segment);
        }

        if (!string.IsNullOrEmpty(tier))
        {
            customersQuery = customersQuery.Where(c => c.Tier == tier);
        }

        if (!string.IsNullOrEmpty(preferredLanguage))
        {
            customersQuery = customersQuery.Where(c => c.PreferredLanguage == preferredLanguage);
        }

        // Email partial match using LIKE (T126, T128)
        if (!string.IsNullOrEmpty(email))
        {
            customersQuery = customersQuery.Where(c => EF.Functions.ILike(c.Email, $"%{email}%"));
        }

        // Filter by company ID (T126)
        if (companyId.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CompanyId == companyId.Value);
        }

        // Filter by creation date range (T126, T128)
        if (createdFrom.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedAt >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedAt <= createdTo.Value);
        }

        // Get total count for pagination
        var totalCount = await customersQuery.CountAsync(cancellationToken);

        // Calculate pagination
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var skip = (page - 1) * pageSize;

        // Get page data with default ordering by created_at DESC
        var customers = await customersQuery
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Batch fetch related data for performance
        var customerIds = customers.Select(c => c.Id).ToList();
        var companyIds = customers
            .Where(c => c.CompanyId.HasValue)
            .Select(c => c.CompanyId!.Value)
            .Distinct()
            .ToList();

        var companies = await _context.Companies
            .Where(comp => companyIds.Contains(comp.Id))
            .ToDictionaryAsync(comp => comp.Id, cancellationToken);

        var ndas = await _context.NDARecords
            .Where(n => customerIds.Contains(n.CustomerId))
            .GroupBy(n => n.CustomerId)
            .Select(g => g.OrderByDescending(n => n.CreatedAt).First())
            .ToDictionaryAsync(n => n.CustomerId, cancellationToken);

        _logger.LogDebug("Retrieved {Count} customers (page {Page}/{TotalPages}, total {TotalCount})",
            customers.Count, page, totalPages, totalCount);

        return new PaginatedResponse<CustomerResponse>
        {
            Items = customers.Select(c => c.ToCustomerResponse(
                c.CompanyId.HasValue && companies.TryGetValue(c.CompanyId.Value, out var comp) ? comp : null,
                ndas.TryGetValue(c.Id, out var nda) ? nda : null)).ToList(),
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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated response containing customer preferences</returns>
    public async Task<PaginatedResponse<GetCustomerPreferencesResponse>> GetPreferencesAsync(
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Querying customer preferences for compliance/audit: page={Page}, pageSize={PageSize}",
            page, pageSize);

        var query = _context.Customers
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Calculate pagination
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var skip = (page - 1) * pageSize;

        // Get page data
        var customers = await query
            .OrderBy(c => c.Email)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

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

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();

        // Check in local Customers table
        var existsLocally = await _context.Customers
            .Where(c => !c.IsDeleted)
            .AnyAsync(c => c.Email.ToLower() == normalizedEmail, cancellationToken);

        if (existsLocally)
        {
            _logger.LogDebug("Email check for {Email}: Exists in Customers table", normalizedEmail);
            return true;
        }

        // Check in IAM system (central identity)
        var iamPrincipal = await _iamClient.GetPrincipalByEmailAsync(normalizedEmail, cancellationToken);
        if (iamPrincipal != null)
        {
            // If it exists in IAM but NOT in Customers table, it means they are an employee or similar
            // We return false here because THEY CAN be registered as a customer
            // But we should probably distinguish between "Exists as Customer" and "Exists in System"
            _logger.LogDebug("Email check for {Email}: Found in IAM but not in Customers table (allowed for onboarding)", normalizedEmail);
            return false;
        }

        _logger.LogDebug("Email existence check for {Email}: Not found in system", normalizedEmail);

        return false;
    }
}
