using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Users;
using Maliev.CustomerService.Api.Services;
using FluentValidation;
using System.Security.Claims;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for user account management operations
/// </summary>
[ApiController]
[Route("v1/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdatePasswordRequest> _updatePasswordValidator;
    private readonly IValidator<UpdateRolesRequest> _updateRolesValidator;

    public UserController(
        IUserService userService,
        ILogger<UserController> logger,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdatePasswordRequest> updatePasswordValidator,
        IValidator<UpdateRolesRequest> updateRolesValidator)
    {
        _userService = userService;
        _logger = logger;
        _createValidator = createValidator;
        _updatePasswordValidator = updatePasswordValidator;
        _updateRolesValidator = updateRolesValidator;
    }

    /// <summary>
    /// Creates a new user account
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user response</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="409">Username or email already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            // Validate request using FluentValidation
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for user creation: {Errors}",
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                var errorResponse = new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred",
                    Details = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()),
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                return BadRequest(errorResponse);
            }

            var (actorId, actorType) = GetActorInfo();

            var user = await _userService.CreateAsync(request, actorId, actorType);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists") ||
                                                     ex.Message.Contains("is already taken"))
        {
            _logger.LogWarning(ex, "Duplicate username or email");
            return Conflict(new ErrorResponse
            {
                Code = "DUPLICATE_USER",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating user");
            throw; // Let global exception handler deal with it
        }
    }

    /// <summary>
    /// Retrieves a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User response</returns>
    /// <response code="200">User found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(string id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                _logger.LogDebug("User {UserId} not found", id);
                return NotFound(new ErrorResponse
                {
                    Code = "NOT_FOUND",
                    Message = $"User with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all users with pagination and optional filtering by last_login_at
    /// </summary>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="lastLoginBefore">Optional: Filter users who last logged in before this date (ISO 8601 format)</param>
    /// <param name="lastLoginAfter">Optional: Filter users who last logged in after this date (ISO 8601 format)</param>
    /// <returns>Paginated list of users</returns>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? lastLoginBefore = null,
        [FromQuery] DateTime? lastLoginAfter = null)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1)
                page = 1;

            if (pageSize < 1)
                pageSize = 20;

            if (pageSize > 100)
                pageSize = 100;

            var (users, totalCount) = await _userService.GetAllAsync(page, pageSize, lastLoginBefore, lastLoginAfter);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new
            {
                users,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            throw;
        }
    }

    /// <summary>
    /// Updates a user's password
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Password update request</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Password updated successfully</response>
    /// <response code="400">Invalid request data or incorrect current password</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePassword(string id, [FromBody] UpdatePasswordRequest request)
    {
        try
        {
            // Validate request using FluentValidation
            var validationResult = await _updatePasswordValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for password update: {Errors}",
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                var errorResponse = new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred",
                    Details = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()),
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                return BadRequest(errorResponse);
            }

            var (actorId, actorType) = GetActorInfo();

            await _userService.UpdatePasswordAsync(id, request, actorId, actorType);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found for password update", id);
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update password for user {UserId}", id);
            return BadRequest(new ErrorResponse
            {
                Code = "PASSWORD_UPDATE_FAILED",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating password for user {UserId}", id);
            throw;
        }
    }

    /// <summary>
    /// Updates a user's roles
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Roles update request</param>
    /// <returns>Updated user response</returns>
    /// <response code="200">Roles updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id}/roles")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateRoles(string id, [FromBody] UpdateRolesRequest request)
    {
        try
        {
            // Validate request using FluentValidation
            var validationResult = await _updateRolesValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for roles update: {Errors}",
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                var errorResponse = new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred",
                    Details = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()),
                    TraceId = HttpContext.TraceIdentifier,
                    Timestamp = DateTime.UtcNow
                };

                return BadRequest(errorResponse);
            }

            var (actorId, actorType) = GetActorInfo();

            var user = await _userService.UpdateRolesAsync(id, request, actorId, actorType);

            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {UserId} not found for roles update", id);
            return NotFound(new ErrorResponse
            {
                Code = "NOT_FOUND",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update roles for user {UserId}", id);
            return BadRequest(new ErrorResponse
            {
                Code = "ROLES_UPDATE_FAILED",
                Message = ex.Message,
                TraceId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating roles for user {UserId}", id);
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
