using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.CustomerService.Api.Authorization;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Services;
using System.Security.Claims;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for customer management operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("customer/v{version:apiVersion}/customers")]
[Authorize]
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
    /// Creates a new customer
    /// </summary>
    /// <param name="request">Customer creation request</param>
    /// <returns>Created customer response</returns>
    /// <response code="201">Customer created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="409">Duplicate email</response>
    /// <response code="422">Domain validation failure</response>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request)
    {
        try
        {
            // ModelState validation via DataAnnotations
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for customer creation: {Errors}",
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

            var (actorId, actorType) = GetActorInfo();

            var customer = await _customerService.CreateAsync(request, actorId, actorType);

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
            throw; // Let global exception handler deal with it
        }
    }

    /// <summary>
    /// Retrieves a customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer response</returns>
    /// <response code="200">Customer found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Customer not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetById(Guid id)
    {
        try
        {
            var customer = await _customerService.GetByIdAsync(id);

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
    public async Task<ActionResult<CustomerResponse>> GetByPrincipalId(Guid principalId)
    {
        try
        {
            var customer = await _customerService.GetByPrincipalIdAsync(principalId);

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
    /// <returns>Updated customer response</returns>
    /// <response code="200">Customer updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Customer not found</response>
    /// <response code="409">Version conflict or duplicate email</response>
    /// <response code="422">Domain validation failure</response>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerResponse>> Update(Guid id, [FromBody] UpdateCustomerRequest request)
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

            var (actorId, actorType) = GetActorInfo();

            var customer = await _customerService.UpdateAsync(id, request, actorId, actorType);

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
    [ProducesResponseType(typeof(PaginatedResponse<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? segment = null,
        [FromQuery] string? tier = null,
        [FromQuery] string? preferredLanguage = null,
        [FromQuery] string? email = null,
        [FromQuery] Guid? companyId = null,
        [FromQuery] DateTime? createdFrom = null,
        [FromQuery] DateTime? createdTo = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Getting all customers with filters: segment={Segment}, tier={Tier}, language={Language}, email={Email}, companyId={CompanyId}",
                segment, tier, preferredLanguage, email, companyId);

            var result = await _customerService.GetAllAsync(
                segment,
                tier,
                preferredLanguage,
                email,
                companyId,
                createdFrom,
                createdTo,
                includeDeleted,
                page,
                pageSize);

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
    [ProducesResponseType(typeof(PaginatedResponse<GetCustomerPreferencesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            _logger.LogInformation("Getting customer preferences for compliance/audit: page={Page}, pageSize={PageSize}",
                page, pageSize);

            var result = await _customerService.GetPreferencesAsync(page, pageSize);

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
    /// <returns>No content on success</returns>
    /// <response code="204">Customer deleted successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Customer not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var (actorId, actorType) = GetActorInfo();

            var deleted = await _customerService.SoftDeleteAsync(id, actorId, actorType);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            throw;
        }
    }

    /// <summary>
    /// Extracts actor information from JWT claims
    /// </summary>
    /// <returns>Tuple of (actorId, actorType)</returns>
    private (string actorId, string actorType) GetActorInfo()
    {
        // Extract user ID from JWT claims (typically "sub" claim)
        var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? "unknown";

        // Determine actor type from role claims
        // Employee role = Employee actor type, otherwise Customer
        // Updated for GCP-style roles: roles.customer.{role-name}
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var actorType = roles.Any(r => r.Equals("Employee", StringComparison.OrdinalIgnoreCase) ||
                                       r.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                                       r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                       r.Equals("roles.customer.admin", StringComparison.OrdinalIgnoreCase) ||
                                       r.Equals("roles.customer.manager", StringComparison.OrdinalIgnoreCase) ||
                                       r.Equals("roles.customer.representative", StringComparison.OrdinalIgnoreCase))
            ? "Employee"
            : "Customer";

        _logger.LogDebug("Actor info: ID={ActorId}, Type={ActorType}", actorId, actorType);

        return (actorId, actorType);
    }
}
