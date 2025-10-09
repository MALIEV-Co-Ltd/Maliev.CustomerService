using Maliev.CustomerService.Api.Models.Users;
using Maliev.CustomerService.Data;
using Maliev.CustomerService.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service implementation for user account management operations
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly CustomerDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly MetricsService _metricsService;

    public UserService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        CustomerDbContext context,
        ILogger<UserService> logger,
        MetricsService metricsService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Creating user {Username} by actor {ActorId} ({ActorType})",
            request.Username, actorId, actorType);

        // Create user entity
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            EmailConfirmed = false, // Can be set to true if auto-confirmed
            LinkedCustomerId = request.LinkedCustomerId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create user with password
        var createResult = await _userManager.CreateAsync(user, request.Password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Failed to create user {Username}: {Errors}", request.Username, errors);
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Assign roles
        if (request.Roles.Any())
        {
            var rolesResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!rolesResult.Succeeded)
            {
                var errors = string.Join(", ", rolesResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to assign roles to user {Username}: {Errors}", request.Username, errors);
                // Rollback: delete the user
                await _userManager.DeleteAsync(user);
                throw new InvalidOperationException($"Failed to assign roles: {errors}");
            }
        }

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = AuditAction.Create,
            EntityType = nameof(ApplicationUser),
            EntityId = user.Id,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new
            {
                user.UserName,
                user.Email,
                Roles = request.Roles,
                user.LinkedCustomerId
            })
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created successfully with username {Username}",
            user.Id, user.UserName);

        return await MapToResponseAsync(user);
    }

    public async Task<bool> UpdatePasswordAsync(string userId, UpdatePasswordRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Updating password for user {UserId} by actor {ActorId} ({ActorType})",
            userId, actorId, actorType);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for password update", userId);
            throw new KeyNotFoundException($"User with ID '{userId}' not found");
        }

        // Change password with current password verification
        var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!changeResult.Succeeded)
        {
            var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Failed to update password for user {UserId}: {Errors}", userId, errors);
            throw new InvalidOperationException($"Failed to update password: {errors}");
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Create audit log (without logging passwords)
        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = "UpdatePassword",
            EntityType = nameof(ApplicationUser),
            EntityId = user.Id,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new { PasswordChanged = true })
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password updated successfully for user {UserId}", userId);

        return true;
    }

    public async Task<UserResponse> UpdateRolesAsync(string userId, UpdateRolesRequest request, string actorId, string actorType)
    {
        _logger.LogInformation("Updating roles for user {UserId} by actor {ActorId} ({ActorType})",
            userId, actorId, actorType);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for roles update", userId);
            throw new KeyNotFoundException($"User with ID '{userId}' not found");
        }

        // Get current roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        var previousRoles = currentRoles.ToList();

        // Remove all current roles
        if (currentRoles.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to remove existing roles from user {UserId}: {Errors}", userId, errors);
                throw new InvalidOperationException($"Failed to remove existing roles: {errors}");
            }
        }

        // Add new roles
        if (request.Roles.Any())
        {
            var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to assign new roles to user {UserId}: {Errors}", userId, errors);
                throw new InvalidOperationException($"Failed to assign new roles: {errors}");
            }
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Create audit log
        var auditLog = new AuditLog
        {
            ActorId = actorId,
            ActorType = actorType,
            Action = "UpdateRoles",
            EntityType = nameof(ApplicationUser),
            EntityId = user.Id,
            Timestamp = DateTime.UtcNow,
            ChangedFields = JsonSerializer.Serialize(new { NewRoles = request.Roles }),
            PreviousValues = JsonSerializer.Serialize(new { PreviousRoles = previousRoles })
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Roles updated successfully for user {UserId}", userId);

        return await MapToResponseAsync(user);
    }

    public async Task<ValidationResponse> ValidateCredentialsAsync(ValidateCredentialsRequest request)
    {
        _logger.LogDebug("Validating credentials for username {Username}", request.Username);

        // Measure auth validation duration
        using var timer = _metricsService.MeasureAuthValidationDuration();

        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            _logger.LogWarning("User {Username} not found for credential validation", request.Username);
            _metricsService.RecordAuthValidation(false);
            return new ValidationResponse { IsValid = false };
        }

        // Check if account is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user {Username} attempted to login", request.Username);
            _metricsService.RecordAuthValidation(false);
            return new ValidationResponse { IsValid = false };
        }

        // Verify password
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
            {
                _logger.LogWarning("User {Username} account is locked out", request.Username);
            }
            else
            {
                _logger.LogWarning("Invalid password for user {Username}", request.Username);
            }

            _metricsService.RecordAuthValidation(false);
            return new ValidationResponse { IsValid = false };
        }

        // Update last_login_at
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Get roles
        var roles = await _userManager.GetRolesAsync(user);

        _logger.LogInformation("Credentials validated successfully for user {Username} ({UserId})",
            request.Username, user.Id);

        // Record successful auth validation
        _metricsService.RecordAuthValidation(true);

        return new ValidationResponse
        {
            IsValid = true,
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Roles = roles.ToList()
        };
    }

    public async Task<UserResponse?> GetByIdAsync(string userId)
    {
        _logger.LogDebug("Retrieving user {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogDebug("User {UserId} not found", userId);
            return null;
        }

        return await MapToResponseAsync(user);
    }

    public async Task<(List<UserResponse> Users, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        DateTime? lastLoginBefore = null,
        DateTime? lastLoginAfter = null)
    {
        _logger.LogDebug("Retrieving users: page={Page}, pageSize={PageSize}, lastLoginBefore={Before}, lastLoginAfter={After}",
            page, pageSize, lastLoginBefore, lastLoginAfter);

        var query = _context.Users.AsQueryable();

        // Apply filters
        if (lastLoginBefore.HasValue)
        {
            query = query.Where(u => u.LastLoginAt < lastLoginBefore.Value);
        }

        if (lastLoginAfter.HasValue)
        {
            query = query.Where(u => u.LastLoginAt > lastLoginAfter.Value);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var responses = new List<UserResponse>();
        foreach (var user in users)
        {
            responses.Add(await MapToResponseAsync(user));
        }

        _logger.LogDebug("Retrieved {Count} users out of {Total}", users.Count, totalCount);

        return (responses, totalCount);
    }

    private async Task<UserResponse> MapToResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserResponse
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList(),
            LinkedCustomerId = user.LinkedCustomerId,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
