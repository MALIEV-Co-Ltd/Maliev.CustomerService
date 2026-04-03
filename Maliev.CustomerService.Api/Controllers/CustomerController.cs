using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.CustomerService.Api.Authorization;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for customer management operations
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("customer/v{version:apiVersion}/customers")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomerController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerController"/> class
    /// </summary>
    /// <param name="customerService">Customer service</param>
    /// <param name="logger">Logger instance</param>
    public CustomerController(
        ICustomerService customerService,
        ILogger<CustomerController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Self-register as a new customer. No authentication required.
    /// </summary>
    /// <param name="request">Customer self-registration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created customer response</returns>
    /// <response code="201">Customer registered successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Duplicate email</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponse>> Register(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for customer self-registration: {Errors}", errors);

                var errorResponse = new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred: " + errors,
                    Details = ModelState
                        .Where(ms => ms.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()),
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                return BadRequest(errorResponse);
            }

            var customer = await _customerService.RegisterAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning(ex, "Duplicate email detected during self-registration");
            return Conflict(new ErrorResponse
            {
                Code = "DUPLICATE_EMAIL",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("identity in central system"))
        {
            _logger.LogWarning(ex, "Central identity failure during self-registration");
            return BadRequest(new ErrorResponse
            {
                Code = "IAM_FAILURE",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input during self-registration");
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_INPUT",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during customer self-registration");
            return StatusCode(500, new ErrorResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = "An unexpected error occurred while registering the customer: " + ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Creates a new customer
    /// </summary>
    /// <param name="request">Customer creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created customer response</returns>
    /// <response code="201">Customer created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="409">Duplicate email</response>
    /// <response code="422">Domain validation failure</response>
    [HttpPost]
    [RequirePermission(CustomerPermissions.CustomersCreate)]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // ModelState validation via DataAnnotations
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for customer creation: {Errors}", errors);

                var errorResponse = new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred: " + errors,
                    Details = ModelState
                        .Where(ms => ms.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()),
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                return BadRequest(errorResponse);
            }

            var (actorId, actorType) = User.GetActorInfo();

            var customer = await _customerService.CreateAsync(request, actorId, actorType, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning(ex, "Duplicate email detected");
            return Conflict(new ErrorResponse
            {
                Code = "DUPLICATE_EMAIL",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("identity in central system"))
        {
            _logger.LogWarning(ex, "Central identity failure");
            return BadRequest(new ErrorResponse
            {
                Code = "IAM_FAILURE",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed");
            return UnprocessableEntity(new ErrorResponse
            {
                Code = "DOMAIN_VALIDATION_ERROR",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating customer");
            return StatusCode(500, new ErrorResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = "An unexpected error occurred while creating the customer: " + ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Retrieves a customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer response</returns>
    /// <response code="200">Customer found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Customer not found</response>
    [HttpGet("{id:guid}")]
    [RequirePermission(CustomerPermissions.CustomersRead)]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customerService.GetByIdAsync(id, cancellationToken);

            if (customer == null)
            {
                _logger.LogDebug("Customer {CustomerId} not found", id);
                return NotFound(new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = $"Customer with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            Response.Headers.Append("ETag", customer.xmin.ToString());
            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a customer by their central IAM Principal ID
    /// </summary>
    /// <param name="principalId">The IAM Principal ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer response</returns>
    /// <response code="200">Customer found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - requires customer.customers.read permission</response>
    /// <response code="404">Customer not found</response>
    [HttpGet("by-principal/{principalId:guid}")]
    [RequirePermission(CustomerPermissions.CustomersRead)]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetByPrincipalId(Guid principalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customerService.GetByPrincipalIdAsync(principalId, cancellationToken);

            if (customer == null)
            {
                _logger.LogDebug("Customer with Principal ID {PrincipalId} not found", principalId);
                return NotFound(new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = $"Customer with Principal ID '{principalId}' not found",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer by Principal ID {PrincipalId}", principalId);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="request">Customer update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated customer response</returns>
    /// <response code="200">Customer updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Customer not found</response>
    /// <response code="409">Version conflict or duplicate email</response>
    /// <response code="422">Domain validation failure</response>
    [HttpPatch("{id:guid}")]
    [RequirePermission(CustomerPermissions.CustomersUpdate)]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerResponse>> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // ModelState validation via DataAnnotations
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for customer update: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                var errorResponse = new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred",
                    Details = ModelState
                        .Where(ms => ms.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()),
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                return BadRequest(errorResponse);
            }

            var (actorId, actorType) = User.GetActorInfo();

            var customer = await _customerService.UpdateAsync(id, request, actorId, actorType, cancellationToken);

            Response.Headers.Append("ETag", customer.xmin.ToString());
            return Ok(customer);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer {CustomerId} not found for update", id);
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            _logger.LogWarning(ex, "Concurrency conflict for customer {CustomerId}", id);
            return Conflict(new ErrorResponse
            {
                Code = "VERSION_CONFLICT",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning(ex, "Duplicate email detected");
            return Conflict(new ErrorResponse
            {
                Code = "DUPLICATE_EMAIL",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict for customer {CustomerId}", id);
            return Conflict(new ErrorResponse
            {
                Code = "VERSION_CONFLICT",
                Message = "The customer was modified by another user. Please refresh and try again.",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed");
            return UnprocessableEntity(new ErrorResponse
            {
                Code = "DOMAIN_VALIDATION_ERROR",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating customer {CustomerId}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets all customers with optional filtering and pagination (T120, T127)
    /// </summary>
    [HttpGet]
    [RequirePermission(CustomerPermissions.CustomersList)]
    [ProducesResponseType(typeof(PaginatedResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? query = null,
        [FromQuery] string? segment = null,
        [FromQuery] string? tier = null,
        [FromQuery] string? preferredLanguage = null,
        [FromQuery] string? email = null,
        [FromQuery] Guid? companyId = null,
        [FromQuery] DateTime? createdFrom = null,
        [FromQuery] DateTime? createdTo = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting all customers with filters: query={Query}, segment={Segment}, tier={Tier}, language={Language}, email={Email}, companyId={CompanyId}",
                query, segment, tier, preferredLanguage, email, companyId);

            var result = await _customerService.GetAllAsync(
                query,
                segment,
                tier,
                preferredLanguage,
                email,
                companyId,
                createdFrom,
                createdTo,
                includeDeleted,
                page,
                pageSize,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            throw;
        }
    }

    /// <summary>
    /// Gets customer preferences for compliance/audit purposes (T123)
    /// </summary>
    [HttpGet("preferences")]
    [RequirePermission(CustomerPermissions.CustomersList)]
    [ProducesResponseType(typeof(PaginatedResponse<GetCustomerPreferencesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting customer preferences for compliance/audit: page={Page}, pageSize={PageSize}",
                page, pageSize);

            var result = await _customerService.GetPreferencesAsync(page, pageSize, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer preferences");
            throw;
        }
    }

    /// <summary>
    /// Soft deletes a customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="request">Delete request containing xmin for concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Customer deleted successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Customer not found</response>
    /// <response code="409">Version conflict</response>
    [HttpDelete("{id:guid}")]
    [RequirePermission(CustomerPermissions.CustomersDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteCustomerRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var (actorId, actorType) = User.GetActorInfo();

            var deleted = await _customerService.SoftDeleteAsync(id, request.xmin, actorId, actorType, cancellationToken);

            if (!deleted)
            {
                _logger.LogWarning("Customer {CustomerId} not found for deletion", id);
                return NotFound(new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = $"Customer with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            return Conflict(new ErrorResponse
            {
                Code = "VERSION_CONFLICT",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            throw;
        }
    }

    /// <summary>
    /// Checks if a customer with the specified email already exists
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email existence check result</returns>
    /// <response code="200">Check completed successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("check-email")]
    [RequirePermission(CustomerPermissions.CustomersRead)]
    [ProducesResponseType(typeof(EmailExistsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EmailExistsResponse>> CheckEmail([FromQuery] string email, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Ok(new EmailExistsResponse { Exists = false });
            }

            // Trim whitespace and normalize email
            email = email.Trim().ToLowerInvariant();

            var exists = await _customerService.EmailExistsAsync(email, cancellationToken);

            return Ok(new EmailExistsResponse { Exists = exists, Email = email });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence for {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Gets activity history for a customer with pagination or skip/take
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [RequirePermission(CustomerPermissions.CustomersRead)]
    [ProducesResponseType(typeof(PaginatedResponse<CustomerActivityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<CustomerActivityResponse>>> GetHistory(
        Guid id,
        [FromQuery] int? skip = null,
        [FromQuery] int? take = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.GetActivityAsync(id, skip, take, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
