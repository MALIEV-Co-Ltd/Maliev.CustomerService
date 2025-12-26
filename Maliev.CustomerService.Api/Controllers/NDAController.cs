using System.Security.Claims;
using Asp.Versioning;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for NDA lifecycle management operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("customer/v{version:apiVersion}/ndas")]
[Authorize]
public class NDAController : ControllerBase
{
    private readonly INDAService _ndaService;
    private readonly ILogger<NDAController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NDAController"/> class
    /// </summary>
    /// <param name="ndaService">NDA service</param>
    /// <param name="logger">Logger instance</param>
    public NDAController(
        INDAService ndaService,
        ILogger<NDAController> logger)
    {
        _ndaService = ndaService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new NDA record
    /// </summary>
    /// <param name="request">NDA creation request</param>
    /// <returns>Created NDA response</returns>
    /// <response code="201">NDA created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost]
    [ProducesResponseType(typeof(NDAResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NDAResponse>> Create([FromBody] CreateNDARequest request)
    {
        try
        {
            // ModelState validation via DataAnnotations
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for NDA creation: {Errors}",
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

            var nda = await _ndaService.CreateAsync(request, actorId, actorType);

            return CreatedAtAction(nameof(GetById), new { id = nda.Id }, nda);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating NDA");
            throw; // Let global exception handler deal with it
        }
    }

    /// <summary>
    /// Retrieves an NDA record by ID
    /// </summary>
    /// <param name="id">NDA ID</param>
    /// <returns>NDA response</returns>
    /// <response code="200">NDA found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">NDA not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NDAResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NDAResponse>> GetById(Guid id)
    {
        try
        {
            var nda = await _ndaService.GetByIdAsync(id);

            if (nda == null)
            {
                _logger.LogDebug("NDA {NDAId} not found", id);
                return NotFound(new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = $"NDA with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(nda);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving NDA {NDAId}", id);
            throw;
        }
    }

    /// <summary>
    /// Updates NDA status with lifecycle validation
    /// </summary>
    /// <param name="id">NDA ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated NDA response</returns>
    /// <response code="200">NDA status updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">NDA not found</response>
    /// <response code="409">Version conflict</response>
    /// <response code="422">Invalid lifecycle transition</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(NDAResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<NDAResponse>> UpdateStatus(Guid id, [FromBody] UpdateNDAStatusRequest request)
    {
        try
        {
            // ModelState validation via DataAnnotations
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for NDA status update: {Errors}",
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

            var nda = await _ndaService.UpdateStatusAsync(id, request, actorId, actorType);

            return Ok(nda);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "NDA {NDAId} not found for status update", id);
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
            _logger.LogWarning(ex, "Concurrency conflict for NDA {NDAId}", id);
            return Conflict(new ErrorResponse
            {
                Code = "VERSION_CONFLICT",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("transition") || ex.Message.Contains("lifecycle"))
        {
            _logger.LogWarning(ex, "Invalid lifecycle transition for NDA {NDAId}", id);
            return UnprocessableEntity(new ErrorResponse
            {
                Code = "INVALID_LIFECYCLE_TRANSITION",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict for NDA {NDAId}", id);
            return Conflict(new ErrorResponse
            {
                Code = "VERSION_CONFLICT",
                Message = "The NDA was modified by another user. Please refresh and try again.",
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating NDA status {NDAId}", id);
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
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var actorType = roles.Any(r => r.Equals("Employee", StringComparison.OrdinalIgnoreCase) ||
                                       r.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                                       r.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            ? "Employee"
            : "Customer";

        _logger.LogDebug("Actor info: ID={ActorId}, Type={ActorType}", actorId, actorType);

        return (actorId, actorType);
    }
}
