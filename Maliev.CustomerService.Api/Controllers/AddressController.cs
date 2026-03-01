using Asp.Versioning;
using Maliev.CustomerService.Api.Authorization;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Services;
using Maliev.CustomerService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for address management operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("customer/v{version:apiVersion}/addresses")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;
    private readonly ILogger<AddressController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressController"/> class
    /// </summary>
    /// <param name="addressService">Address service</param>
    /// <param name="logger">Logger instance</param>
    public AddressController(
        IAddressService addressService,
        ILogger<AddressController> logger)
    {
        _addressService = addressService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new address with country validation
    /// </summary>
    /// <param name="request">Address creation request</param>
    /// <returns>Created address response</returns>
    /// <response code="201">Address created successfully</response>
    /// <response code="400">Invalid request data or invalid country ID</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="503">Country Service unavailable</response>
    [HttpPost]
    [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AddressResponse>> Create([FromBody] CreateAddressRequest request)
    {
        try
        {
            // ModelState validation via DataAnnotations
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("Validation failed for address creation: {Errors}", errors);

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

            var address = await _addressService.CreateAsync(request, actorId, actorType);

            return CreatedAtAction(nameof(GetByOwner),
                new { ownerType = address.OwnerType, ownerId = address.OwnerId },
                address);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Country Service is unavailable") ||
                                                     ex.Message.Contains("timed out"))
        {
            _logger.LogError(ex, "Country Service unavailable during address creation");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse
            {
                Code = "SERVICE_UNAVAILABLE",
                Message = "Country Service is temporarily unavailable. Please try again later.",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("is not valid"))
        {
            _logger.LogWarning(ex, "Invalid country ID for address creation");
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_COUNTRY_ID",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating address");
            throw; // Let global exception handler deal with it
        }
    }

    /// <summary>
    /// Retrieves all addresses for a specific owner
    /// </summary>
    /// <param name="ownerType">Type of owner (Customer or Company)</param>
    /// <param name="ownerId">Owner ID</param>
    /// <returns>List of addresses</returns>
    /// <response code="200">Addresses retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<AddressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<AddressResponse>>> GetByOwner(
        [FromQuery] string ownerType,
        [FromQuery] Guid ownerId)
    {
        try
        {
            var addresses = await _addressService.GetByOwnerAsync(ownerType, ownerId);
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving addresses for {OwnerType} {OwnerId}",
                ownerType, ownerId);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing address
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <param name="request">Address update request</param>
    /// <returns>Updated address response</returns>
    /// <response code="200">Address updated successfully</response>
    /// <response code="400">Invalid request data or invalid country ID</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Address not found</response>
    /// <response code="409">Version conflict</response>
    /// <response code="503">Country Service unavailable</response>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AddressResponse>> Update(
        Guid id,
        [FromBody] UpdateAddressRequest request)
    {
        try
        {
            // ModelState validation via DataAnnotations
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for address update: {Errors}",
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

            var address = await _addressService.UpdateAsync(id, request, actorId, actorType);

            return Ok(address);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Address {AddressId} not found for update", id);
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Country Service is unavailable") ||
                                                     ex.Message.Contains("timed out"))
        {
            _logger.LogError(ex, "Country Service unavailable during address update");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorResponse
            {
                Code = "SERVICE_UNAVAILABLE",
                Message = "Country Service is temporarily unavailable. Please try again later.",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("is not valid"))
        {
            _logger.LogWarning(ex, "Invalid country ID for address update");
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_COUNTRY_ID",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("modified by another user"))
        {
            _logger.LogWarning(ex, "Concurrency conflict for address {AddressId}", id);
            return Conflict(new ErrorResponse
            {
                Code = "VERSION_CONFLICT",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict for address {AddressId}", id);
            return Conflict(new ErrorResponse
            {
                Code = "VERSION_CONFLICT",
                Message = "The address was modified by another user. Please refresh and try again.",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating address {AddressId}", id);
            throw;
        }
    }

    /// <summary>
    /// Deletes an address
    /// </summary>
    /// <param name="id">Address ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Address deleted successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Address not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var (actorId, actorType) = User.GetActorInfo();

            var deleted = await _addressService.DeleteAsync(id, actorId, actorType);

            if (!deleted)
            {
                _logger.LogWarning("Address {AddressId} not found for deletion", id);
                return NotFound(new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = $"Address with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId}", id);
            throw;
        }
    }
}
