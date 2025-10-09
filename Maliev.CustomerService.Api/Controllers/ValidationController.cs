using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Users;
using Maliev.CustomerService.Api.Services;
using FluentValidation;

namespace Maliev.CustomerService.Api.Controllers;

/// <summary>
/// Controller for credential validation (used by Auth Service)
/// </summary>
[ApiController]
[Route("customers/v1")]
public class ValidationController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<ValidationController> _logger;
    private readonly IValidator<ValidateCredentialsRequest> _validator;

    public ValidationController(
        IUserService userService,
        ILogger<ValidationController> logger,
        IValidator<ValidateCredentialsRequest> validator)
    {
        _userService = userService;
        _logger = logger;
        _validator = validator;
    }

    /// <summary>
    /// Validates user credentials and updates last_login_at on success
    /// </summary>
    /// <param name="request">Credential validation request</param>
    /// <returns>Validation response with user details if valid</returns>
    /// <response code="200">Credentials validated (check isValid field)</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="429">Rate limit exceeded</response>
    /// <remarks>
    /// This endpoint is rate limited to 10 requests per minute per IP address.
    /// Returns generic error messages for security (does not reveal whether username exists).
    /// </remarks>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ValidationResponse>> ValidateCredentials([FromBody] ValidateCredentialsRequest request)
    {
        // Get source IP for security logging
        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var timestamp = DateTime.UtcNow;

        try
        {
            // Validate request using FluentValidation
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                // Security logging: Log validation attempt failure without password
                _logger.LogWarning(
                    "Credential validation failed: Invalid request format. Username: {Username}, SourceIP: {SourceIP}, Timestamp: {Timestamp}",
                    request.Username ?? "null",
                    sourceIp,
                    timestamp);

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
                    Timestamp = timestamp
                };

                return BadRequest(errorResponse);
            }

            // Attempt credential validation
            var response = await _userService.ValidateCredentialsAsync(request);

            if (response.IsValid)
            {
                // Security logging: Log successful validation WITHOUT password
                _logger.LogInformation(
                    "Credential validation SUCCESS. Username: {Username}, UserId: {UserId}, SourceIP: {SourceIP}, Timestamp: {Timestamp}",
                    request.Username,
                    response.UserId,
                    sourceIp,
                    timestamp);
            }
            else
            {
                // Security logging: Log failed validation WITHOUT password
                // Use generic message to not reveal if username exists
                _logger.LogWarning(
                    "Credential validation FAILED. Username: {Username}, SourceIP: {SourceIP}, Timestamp: {Timestamp}, Reason: Invalid credentials",
                    request.Username,
                    sourceIp,
                    timestamp);
            }

            // Always return 200 OK with isValid field to not reveal whether username exists
            return Ok(response);
        }
        catch (Exception ex)
        {
            // Security logging: Log unexpected error WITHOUT password
            _logger.LogError(ex,
                "Credential validation ERROR. Username: {Username}, SourceIP: {SourceIP}, Timestamp: {Timestamp}",
                request.Username ?? "null",
                sourceIp,
                timestamp);

            // Return generic error to not reveal internal details
            return Ok(new ValidationResponse { IsValid = false });
        }
    }
}
