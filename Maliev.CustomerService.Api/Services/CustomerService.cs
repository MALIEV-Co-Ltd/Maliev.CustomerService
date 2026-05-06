using System.Text.Json;
using System.Text.RegularExpressions;
using Maliev.CustomerService.Api.Mapping;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Api.Search;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Infrastructure.Persistence;
using Maliev.MessagingContracts;
using Maliev.MessagingContracts.Contracts.Customers;
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
        if (!string.IsNullOrEmpty(request.Email))
        {
            // Validate email format
            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("Invalid email format for creation: {Email}", request.Email);
                throw new InvalidOperationException($"'{request.Email}' is not a valid email address.");
            }

            var existingCustomer = await _context.Customers
                .Where(c => c.Email == request.Email && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingCustomer != null)
            {
                _logger.LogWarning("Duplicate email {Email} for active customer", request.Email);
                throw new InvalidOperationException($"A customer with email '{request.Email}' already exists");
            }
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
            ThaiNationalId = request.ThaiNationalId,
            Segment = request.Segment,
            Tier = request.Tier,
            PreferredLanguage = request.PreferredLanguage,
            Timezone = request.Timezone,
            CommunicationPreferences = request.CommunicationPreferences != null
                ? JsonSerializer.Serialize(request.CommunicationPreferences)
                : null,
            PaymentTerms = NormalizePaymentTerms(request.PaymentTerms),
            CompanyId = request.CompanyId,
            AccountManagerEmployeeId = request.AccountManagerEmployeeId,
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
                ThaiNationalId = customer.ThaiNationalId != null ? "***REDACTED***" : null, // Don't log PII
                customer.Segment,
                customer.Tier,
                customer.PreferredLanguage,
                customer.Timezone,
                customer.CommunicationPreferences,
                customer.PaymentTerms,
                customer.CompanyId,
                customer.AccountManagerEmployeeId,
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
        await PublishCustomerSearchUpsertAsync(customer, DateTimeOffset.UtcNow, cancellationToken);

        var xminValue = _context.Entry(customer).Property<uint>("xmin").CurrentValue;
        return customer.ToCustomerResponse(xmin: xminValue);
    }

    /// <inheritdoc />
    public async Task<CustomerResponse> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Customer self-registration for {Email}", request.Email);

        if (string.IsNullOrEmpty(request.Email) || !Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
        {
            _logger.LogWarning("Invalid email format for self-registration: {Email}", request.Email);
            throw new ArgumentException($"'{request.Email}' is not a valid email address.");
        }

        var existingCustomer = await _context.Customers
            .Where(c => c.Email == request.Email && !c.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingCustomer != null)
        {
            _logger.LogWarning("Duplicate email {Email} for self-registration", request.Email);
            throw new InvalidOperationException($"A customer with email '{request.Email}' already exists");
        }

        _logger.LogInformation("Creating IAM principal for self-registered customer with email {Email}", request.Email);
        Guid principalId;
        bool isNewPrincipal = false;
        try
        {
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
            Mobile = request.Phone,
            Segment = "Retail",
            Tier = "Bronze",
            PreferredLanguage = request.PreferredLanguage,
            Timezone = request.Timezone,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);

        var auditLog = new AuditLog
        {
            ActorId = customer.Id.ToString(),
            ActorType = "Customer",
            Action = AuditAction.SelfRegister,
            EntityType = nameof(Customer),
            EntityId = customer.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                customer.FirstName,
                customer.LastName,
                customer.Email,
                customer.Segment,
                customer.Tier,
                customer.PreferredLanguage,
                customer.Timezone,
                RegistrationMethod = request.RegistrationMethod
            })
        };

        _context.AuditLogs.Add(auditLog);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Customer {CustomerId} self-registered successfully", customer.Id);
        }
        catch (Exception ex)
        {
            if (isNewPrincipal)
            {
                _logger.LogError(ex, "Failed to save customer to database. Attempting to delete orphaned IAM principal {PrincipalId}", principalId);
                await _iamClient.DeletePrincipalAsync(principalId, cancellationToken);
            }
            else
            {
                _logger.LogError(ex, "Failed to save customer to database. Keeping existing IAM principal {PrincipalId}", principalId);
            }

            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException($"Failed to create customer due to database error: {innerMessage}", ex);
        }

        _metricsService.RecordCustomerRegistration(customer.Segment);

        await _publishEndpoint.Publish(new CustomerRegisteredEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "CustomerRegisteredEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "CustomerService",
            ConsumedBy: ["NotificationService", "AuthService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new CustomerRegisteredEventPayload(
                CustomerId: customer.Id,
                PrincipalId: principalId,
                Email: customer.Email,
                FirstName: customer.FirstName,
                LastName: customer.LastName,
                RegistrationMethod: request.RegistrationMethod,
                RegisteredAtUtc: DateTimeOffset.UtcNow)
        ), cancellationToken);

        _logger.LogInformation("Published CustomerRegisteredEvent for customer {CustomerId}", customer.Id);
        await PublishCustomerSearchUpsertAsync(customer, DateTimeOffset.UtcNow, cancellationToken);

        var xminValue = _context.Entry(customer).Property<uint>("xmin").CurrentValue;
        return customer.ToCustomerResponse(xmin: xminValue);
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

        var xminValue = _context.Entry(customer).Property<uint>("xmin").CurrentValue;
        var response = customer.ToCustomerResponse(company, nda, xminValue);

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

        var xminValue = _context.Entry(customer).Property<uint>("xmin").CurrentValue;
        var response = customer.ToCustomerResponse(company, nda, xminValue);

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
            customer.Landline,
            ThaiNationalId = customer.ThaiNationalId != null ? "***REDACTED***" : null, // Don't log PII
            customer.Segment,
            customer.Tier,
            customer.PreferredLanguage,
            customer.Timezone,
            customer.CommunicationPreferences,
            customer.PaymentTerms,
            customer.CompanyId,
            customer.AccountManagerEmployeeId
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
            // Validate email format
            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("Invalid email format: {Email}", request.Email);
                throw new InvalidOperationException($"'{request.Email}' is not a valid email address.");
            }

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

        if (request.ThaiNationalId != null && request.ThaiNationalId != customer.ThaiNationalId)
        {
            changedFields["ThaiNationalId"] = "***REDACTED***"; // Don't log PII
            customer.ThaiNationalId = request.ThaiNationalId;
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

        if (!string.IsNullOrWhiteSpace(request.PaymentTerms))
        {
            var paymentTerms = NormalizePaymentTerms(request.PaymentTerms);
            if (!string.Equals(paymentTerms, customer.PaymentTerms, StringComparison.Ordinal))
            {
                changedFields["PaymentTerms"] = paymentTerms;
                customer.PaymentTerms = paymentTerms;
            }
        }

        if (request.CompanyId.HasValue && request.CompanyId != customer.CompanyId)
        {
            changedFields["CompanyId"] = request.CompanyId;
            customer.CompanyId = request.CompanyId;
        }

        if (request.ClearAccountManager && customer.AccountManagerEmployeeId.HasValue)
        {
            changedFields["AccountManagerEmployeeId"] = null!;
            customer.AccountManagerEmployeeId = null;
        }
        else if (request.AccountManagerEmployeeId.HasValue && request.AccountManagerEmployeeId != customer.AccountManagerEmployeeId)
        {
            changedFields["AccountManagerEmployeeId"] = request.AccountManagerEmployeeId.Value;
            customer.AccountManagerEmployeeId = request.AccountManagerEmployeeId.Value;
        }

        if (changedFields.Count > 0)
        {
            customer.UpdatedAt = DateTime.UtcNow;

            // Set the original xmin for optimistic concurrency
            _context.Entry(customer).Property("xmin").OriginalValue = request.xmin;

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
                await PublishCustomerSearchUpsertAsync(customer, DateTimeOffset.UtcNow, cancellationToken);
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

        var xminValue = _context.Entry(customer).Property<uint>("xmin").CurrentValue;
        return customer.ToCustomerResponse(xmin: xminValue);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentTermResponse>> GetPaymentTermsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PaymentTerms
            .AsNoTracking()
            .OrderBy(term => term.SortOrder)
            .ThenBy(term => term.Name)
            .Select(term => new PaymentTermResponse
            {
                Code = term.Code,
                Name = term.Name,
                Category = term.Category,
                Description = term.Description,
                TypicalUse = term.TypicalUse,
                DueDays = term.DueDays,
                DiscountPercent = term.DiscountPercent,
                DiscountDays = term.DiscountDays,
                IsDefault = term.IsDefault,
                SortOrder = term.SortOrder
            })
            .ToListAsync(cancellationToken);
    }

    private static string NormalizePaymentTerms(string? paymentTerms)
    {
        if (string.IsNullOrWhiteSpace(paymentTerms))
        {
            return PaymentTerms.DueOnReceipt;
        }

        var normalized = paymentTerms.Trim();
        return PaymentTerms.All.Contains(normalized, StringComparer.Ordinal)
            ? normalized
            : throw new InvalidOperationException($"Payment terms '{paymentTerms}' are not supported.");
    }

    /// <summary>
    /// Soft deletes a customer with optimistic concurrency control and audit logging
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="xmin">PostgreSQL xmin for concurrency control</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when version conflict occurs</exception>
    public async Task<bool> SoftDeleteAsync(Guid id, uint xmin, string actorId, string actorType, CancellationToken cancellationToken = default)
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

        _context.Entry(customer).Property("xmin").OriginalValue = xmin;

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

        try
        {
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
            await _publishEndpoint.Publish(
                CustomerSearchDocumentMapper.ToDeletedEvent(customer.Id, DateTimeOffset.UtcNow),
                cancellationToken);

            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict deleting customer {CustomerId}", id);
            throw new InvalidOperationException("The customer was modified by another user. Please refresh and try again.");
        }
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
            var terms = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (terms.Length >= 2)
            {
                // If two or more terms are provided, optimize for "FirstName LastName" search
                var first = $"{terms[0]}%";
                var last = $"{terms[1]}%";

                customersQuery = customersQuery.Where(c =>
                    (EF.Functions.ILike(c.FirstName, first) && EF.Functions.ILike(c.LastName, last)) ||
                    EF.Functions.ILike(c.FirstName, searchTerm) ||
                    EF.Functions.ILike(c.LastName, searchTerm) ||
                    EF.Functions.ILike(c.Email, searchTerm));
            }
            else
            {
                customersQuery = customersQuery.Where(c =>
                    EF.Functions.ILike(c.FirstName, searchTerm) ||
                    EF.Functions.ILike(c.LastName, searchTerm) ||
                    EF.Functions.ILike(c.Email, searchTerm));
            }
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
                ndas.TryGetValue(c.Id, out var nda) ? nda : null,
                _context.Entry(c).Property<uint>("xmin").CurrentValue)).ToList(),
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

    /// <inheritdoc />
    public async Task<PaginatedResponse<CustomerActivityResponse>> GetActivityAsync(Guid id, int? skip = null, int? take = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving activity history for customer {CustomerId} (Skip {Skip}, Take {Take}, Page {Page}, Size {PageSize})", id, skip, take, page, pageSize);

        var customerIdStr = id.ToString();

        var query = _context.AuditLogs
            .Where(a => (a.EntityType == nameof(Customer) && a.EntityId == customerIdStr) ||
                        (a.EntityType != nameof(Customer) && a.ChangedFields != null && a.ChangedFields.Contains($"\"{customerIdStr}\"")) ||
                        (a.EntityType != nameof(Customer) && a.PreviousValues != null && a.PreviousValues.Contains($"\"{customerIdStr}\"")));

        var totalCount = await query.CountAsync(cancellationToken);

        int finalSkip = skip ?? (page - 1) * pageSize;
        int finalTake = take ?? pageSize;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var auditLogs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(finalSkip)
            .Take(finalTake)
            .ToListAsync(cancellationToken);

        var activities = new List<CustomerActivityResponse>();
        var companyNameById = await ResolveCompanyNamesForAuditLogsAsync(auditLogs, cancellationToken);

        // Resolve actor names
        var actorIds = auditLogs
            .Where(a => Guid.TryParse(a.ActorId, out _))
            .Select(a => Guid.Parse(a.ActorId))
            .Distinct()
            .ToList();

        var actorMap = new Dictionary<Guid, Maliev.CustomerService.Api.Models.IAM.PrincipalResponse>();
        foreach (var actorId in actorIds)
        {
            var principal = await _iamClient.GetPrincipalByIdAsync(actorId, cancellationToken);
            if (principal != null) actorMap[actorId] = principal;
        }

        foreach (var audit in auditLogs)
        {
            string description = audit.Action switch
            {
                AuditAction.Create => GetCreateDescription(audit),
                AuditAction.Update => GetUpdateDescription(audit.EntityType, audit.ChangedFields, audit.PreviousValues, companyNameById),
                AuditAction.SoftDelete => $"{GetEntityDisplayName(audit.EntityType)} deactivated",
                AuditAction.Delete => $"{GetEntityDisplayName(audit.EntityType)} deleted",
                _ => $"Action: {audit.Action} on {GetEntityDisplayName(audit.EntityType)}"
            };

            string? actorName = null;
            string? actorEmail = null;

            if (Guid.TryParse(audit.ActorId, out var pId) && actorMap.TryGetValue(pId, out var p))
            {
                actorName = p.DisplayName;
                actorEmail = p.Email;
            }

            activities.Add(new CustomerActivityResponse
            {
                Action = audit.Action,
                Description = description,
                ActorId = audit.ActorId,
                ActorName = actorName,
                ActorEmail = actorEmail,
                Timestamp = audit.Timestamp,
                Details = audit.ChangedFields
            });
        }

        return new PaginatedResponse<CustomerActivityResponse>
        {
            Items = activities,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private async Task<Dictionary<Guid, string>> ResolveCompanyNamesForAuditLogsAsync(
        IReadOnlyCollection<AuditLog> auditLogs,
        CancellationToken cancellationToken)
    {
        var companyIds = new HashSet<Guid>();

        foreach (var audit in auditLogs)
        {
            CollectCompanyIds(audit.ChangedFields, companyIds);
            CollectCompanyIds(audit.PreviousValues, companyIds);
        }

        if (companyIds.Count == 0)
        {
            return [];
        }

        return await _context.Companies
            .AsNoTracking()
            .Where(company => companyIds.Contains(company.Id))
            .ToDictionaryAsync(company => company.Id, company => company.Name, cancellationToken);
    }

    private static void CollectCompanyIds(string? json, ISet<Guid> companyIds)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            var fields = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (fields is null)
            {
                return;
            }

            if (fields.TryGetValue("Fields", out var nestedFields) && nestedFields is JsonElement nestedElement)
            {
                fields = JsonSerializer.Deserialize<Dictionary<string, object>>(nestedElement.GetRawText()) ?? fields;
            }

            if (fields.TryGetValue("CompanyId", out var companyId) && TryReadGuid(companyId, out var id))
            {
                companyIds.Add(id);
            }
        }
        catch
        {
            // Audit history should remain readable even if an old row has malformed details.
        }
    }

    private static bool TryReadGuid(object? value, out Guid id)
    {
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return Guid.TryParse(element.GetString(), out id);
            }

            id = default;
            return false;
        }

        return Guid.TryParse(value?.ToString(), out id);
    }

    private async Task PublishCustomerSearchUpsertAsync(Customer customer, DateTimeOffset occurredAtUtc, CancellationToken cancellationToken)
    {
        Company? company = null;
        if (customer.CompanyId.HasValue)
        {
            company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == customer.CompanyId.Value, cancellationToken);
        }

        await _publishEndpoint.Publish(
            CustomerSearchDocumentMapper.ToUpsertEvent(customer, company, occurredAtUtc),
            cancellationToken);
    }

    private static string GetEntityDisplayName(string entityType) => entityType switch
    {
        nameof(Customer) => "Customer profile",
        nameof(Address) => "Address",
        nameof(InternalNote) => "Internal note",
        nameof(NDARecord) => "NDA record",
        nameof(DocumentReference) => "Document",
        _ => entityType
    };

    private static string GetCreateDescription(AuditLog audit)
    {
        return audit.EntityType switch
        {
            nameof(Customer) => "Customer profile created",
            nameof(InternalNote) => "Added an internal note",
            nameof(Address) => GetAddressCreateDescription(audit.ChangedFields),
            nameof(NDARecord) => "Created NDA record",
            nameof(DocumentReference) => "Uploaded a document",
            _ => $"Created {GetEntityDisplayName(audit.EntityType)}"
        };
    }

    private static string GetAddressCreateDescription(string? changedFieldsJson)
    {
        if (!string.IsNullOrEmpty(changedFieldsJson))
        {
            try
            {
                var fields = JsonSerializer.Deserialize<Dictionary<string, object>>(changedFieldsJson);
                if (fields != null && fields.TryGetValue("Type", out var typeObj))
                {
                    var type = typeObj.ToString();
                    return $"Added a new {type?.ToLowerInvariant()} address";
                }
            }
            catch { }
        }
        return "Added a new address";
    }

    private static string GetUpdateDescription(
        string entityType,
        string? changedFieldsJson,
        string? previousValuesJson,
        IReadOnlyDictionary<Guid, string>? companyNameById = null)
    {
        var entityDisplayName = GetEntityDisplayName(entityType);
        if (string.IsNullOrEmpty(changedFieldsJson)) return $"{entityDisplayName} updated";

        try
        {
            var changedFields = JsonSerializer.Deserialize<Dictionary<string, object>>(changedFieldsJson);
            if (changedFields == null || changedFields.Count == 0) return $"{entityDisplayName} updated";

            // Support both flat and nested structure (T240 fix for AddressService inconsistency)
            if (changedFields.ContainsKey("Fields") && changedFields["Fields"] is JsonElement fieldsElement)
            {
                changedFields = JsonSerializer.Deserialize<Dictionary<string, object>>(fieldsElement.GetRawText()) ?? changedFields;
            }

            var previousValues = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(previousValuesJson))
            {
                var prevObj = JsonSerializer.Deserialize<Dictionary<string, object>>(previousValuesJson);
                if (prevObj != null)
                {
                    if (prevObj.ContainsKey("Fields") && prevObj["Fields"] is JsonElement prevFieldsElement)
                    {
                        previousValues = JsonSerializer.Deserialize<Dictionary<string, object>>(prevFieldsElement.GetRawText()) ?? previousValues;
                    }
                    else
                    {
                        previousValues = prevObj;
                    }
                }
            }

            // Detect address type for better description
            string prefix = entityDisplayName;
            if (entityType == nameof(Address))
            {
                object? typeObj = null;
                if (changedFields.TryGetValue("Type", out typeObj) || previousValues.TryGetValue("Type", out typeObj))
                {
                    prefix = $"{typeObj?.ToString()} address";
                }
            }

            var lines = new List<string>();

            foreach (var field in changedFields.Keys)
            {
                var newValueObj = changedFields[field];
                var newValue = newValueObj?.ToString() ?? "empty";

                object? oldValueObj = null;
                previousValues?.TryGetValue(field, out oldValueObj);
                var oldValue = oldValueObj?.ToString() ?? "empty";

                // Robust comparison to skip unchanged fields
                if (newValue.Equals(oldValue, StringComparison.OrdinalIgnoreCase)) continue;

                // Special handling for GUIDs (OwnerId, etc.) - compare as GUIDs if possible
                if (field.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(newValue, out var newGuid) && Guid.TryParse(oldValue, out var oldGuid))
                    {
                        if (newGuid == oldGuid) continue;
                    }
                }

                string fieldDisplayName = field switch
                {
                    "FirstName" => "first name",
                    "LastName" => "last name",
                    "Email" => "email",
                    "Mobile" => "mobile number",
                    "Landline" => "landline",
                    "Segment" => "segment",
                    "Tier" => "tier",
                    "PreferredLanguage" => "language",
                    "Timezone" => "timezone",
                    "NoteText" => "note content",
                    "Status" => "status",
                    "AddressLine1" => "address line 1",
                    "AddressLine2" => "address line 2",
                    "District" => "district",
                    "City" => "city",
                    "PostalCode" => "postal code",
                    "IsDefault" => "default status",
                    "CompanyId" => "company",
                    "AccountManagerEmployeeId" => "account manager",
                    _ => field.ToLowerInvariant()
                };

                // Truncate long values for readability
                var displayOld = oldValue.Length > 30 ? oldValue.Substring(0, 27) + "..." : oldValue;
                var displayNew = newValue.Length > 30 ? newValue.Substring(0, 27) + "..." : newValue;

                if (field == "CompanyId")
                {
                    displayOld = FormatCompanyAuditValue(oldValue, companyNameById);
                    displayNew = FormatCompanyAuditValue(newValue, companyNameById);
                }
                else if (field == "AccountManagerEmployeeId")
                {
                    displayOld = FormatAccountManagerAuditValue(oldValue);
                    displayNew = FormatAccountManagerAuditValue(newValue);
                }

                // Humanize boolean values
                if (field == "IsDefault")
                {
                    displayOld = displayOld.ToLower() == "true" ? "default" : "non-default";
                    displayNew = displayNew.ToLower() == "true" ? "default" : "non-default";
                }

                if (oldValue == "empty")
                {
                    lines.Add($"set {fieldDisplayName} to '**{displayNew}**'");
                }
                else
                {
                    lines.Add($"changed {fieldDisplayName} from '**{displayOld}**' to '**{displayNew}**'");
                }
            }

            if (lines.Count == 0) return $"{prefix} updated";

            if (lines.Count > 1)
            {
                return $"Updated {prefix.ToLowerInvariant()}:\n* {string.Join("\n* ", lines.Select(l => char.ToUpper(l[0]) + l.Substring(1)))}";
            }

            return char.ToUpper(prefix[0]) + prefix.Substring(1) + " update: " + lines[0];
        }
        catch
        {
            return $"{GetEntityDisplayName(entityType)} updated";
        }
    }

    private static string FormatCompanyAuditValue(string value, IReadOnlyDictionary<Guid, string>? companyNameById)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("empty", StringComparison.OrdinalIgnoreCase))
        {
            return "No company";
        }

        if (Guid.TryParse(value, out var companyId))
        {
            return companyNameById != null && companyNameById.TryGetValue(companyId, out var companyName)
                ? companyName
                : "Unknown company";
        }

        return value;
    }

    private static string FormatAccountManagerAuditValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("empty", StringComparison.OrdinalIgnoreCase))
        {
            return "No account manager";
        }

        return Guid.TryParse(value, out _)
            ? "assigned employee"
            : value;
    }
}
