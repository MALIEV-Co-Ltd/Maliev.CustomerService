using Maliev.CustomerService.Api.Models.Users;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Service interface for user account management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Creates a new user account with audit logging
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <returns>Created user response</returns>
    /// <exception cref="InvalidOperationException">Username or email already exists, or role assignment fails</exception>
    Task<UserResponse> CreateAsync(CreateUserRequest request, string actorId, string actorType);

    /// <summary>
    /// Updates a user's password with audit logging
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Password update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <returns>True if successful</returns>
    /// <exception cref="KeyNotFoundException">User not found</exception>
    /// <exception cref="InvalidOperationException">Current password is incorrect or password update fails</exception>
    Task<bool> UpdatePasswordAsync(string userId, UpdatePasswordRequest request, string actorId, string actorType);

    /// <summary>
    /// Updates a user's roles with audit logging
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Roles update request</param>
    /// <param name="actorId">ID of the actor performing the action</param>
    /// <param name="actorType">Type of actor (Customer, Employee, System)</param>
    /// <returns>Updated user response</returns>
    /// <exception cref="KeyNotFoundException">User not found</exception>
    /// <exception cref="InvalidOperationException">Role update fails</exception>
    Task<UserResponse> UpdateRolesAsync(string userId, UpdateRolesRequest request, string actorId, string actorType);

    /// <summary>
    /// Validates user credentials and updates last_login_at on success
    /// </summary>
    /// <param name="request">Credential validation request</param>
    /// <returns>Validation response with user details if valid</returns>
    Task<ValidationResponse> ValidateCredentialsAsync(ValidateCredentialsRequest request);

    /// <summary>
    /// Retrieves a user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User response or null if not found</returns>
    Task<UserResponse?> GetByIdAsync(string userId);

    /// <summary>
    /// Retrieves all users with pagination and optional filtering by last_login_at
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="lastLoginBefore">Optional filter: users who last logged in before this date</param>
    /// <param name="lastLoginAfter">Optional filter: users who last logged in after this date</param>
    /// <returns>Paginated list of users</returns>
    Task<(List<UserResponse> Users, int TotalCount)> GetAllAsync(int page, int pageSize, DateTime? lastLoginBefore = null, DateTime? lastLoginAfter = null);
}
