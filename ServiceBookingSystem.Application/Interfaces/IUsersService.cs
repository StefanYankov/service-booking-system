using Microsoft.AspNetCore.Identity;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.Application.Interfaces;

/// <summary>
/// Defines the contract for a service that handles administrative user management operations.
/// </summary>
public interface IUsersService
{
    /// <summary>
    /// Asynchronously creates a new user and assigns them to their initial roles.
    /// This method is intended for administrative use.
    /// </summary>
    /// <param name="dto">A Data Transfer Object containing the new user's information, including password and roles.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an <see cref="IdentityResult"/>
    /// containing the outcome of the create operation, including any validation errors.
    /// </returns>
    Task<IdentityResult> CreateUserAsync(UserCreateDto dto);

    /// <summary>
    /// Asynchronously registers a new user (Customer or Provider) via the public API.
    /// </summary>
    /// <param name="dto">The registration details.</param>
    /// <returns>The result of the registration operation.</returns>
    Task<IdentityResult> RegisterUserAsync(RegisterDto dto);
    
    /// <summary>
    /// Asynchronously updates an existing user's profile information.
    /// </summary>
    /// <remarks>
    /// This method updates properties like name and phone number. It does not handle password or role changes.
    /// </remarks>
    /// <param name="dto">A Data Transfer Object containing the updated user profile information.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an <see cref="IdentityResult"/>
    /// containing the outcome of the update operation.
    /// </returns>
    /// <exception cref="EntityNotFoundException">Thrown if a user with the ID specified in the DTO does not exist.</exception>
    Task<IdentityResult> UpdateUserAsync(UserUpdateDto dto);

    /// <summary>
    /// Asynchronously changes the password for a user.
    /// </summary>
    /// <param name="userId">The ID of the user changing their password.</param>
    /// <param name="dto">The password change details (old and new password).</param>
    /// <returns>The result of the password change operation.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the user is not found.</exception>
    Task<IdentityResult> ChangePasswordAsync(string userId, ChangePasswordDto dto);

    /// <summary>
    /// Asynchronously confirms a user's email address using a token.
    /// </summary>
    /// <param name="dto">The confirmation details (UserId and Token).</param>
    /// <returns>The result of the confirmation operation.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the user is not found.</exception>
    Task<IdentityResult> ConfirmEmailAsync(ConfirmEmailDto dto);

    /// <summary>
    /// Asynchronously retrieves a paginated list of users, with optional filtering and sorting.
    /// </summary>
    /// <param name="parameters">
    /// An object containing the query parameters:
    /// - <c>PageNumber</c>: The page number to retrieve.
    /// - <c>PageSize</c>: The number of items per page.
    /// - <c>SortBy</c>: The property to sort the results by (e.g., "username", "email").
    /// - <c>SortDirection</c>: The direction to sort ("asc" or "desc").
    /// - <c>SearchTerm</c>: An optional search term to filter users by first name, last name, username, or email.
    /// </param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a paged result
    /// with the list of matching users for the current page and pagination metadata.
    /// </returns>
    Task<PagedResult<UserViewDto>> GetAllUsersAsync(UserQueryParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a single user by their unique identifier.
    /// </summary>
    /// <param name="id">The user's unique identifier (typically the GUID stored as a string).</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the user view DTO
    /// if a user with the specified ID is found; otherwise, null.
    /// </returns>
    Task<UserViewDto?> GetUserByIdAsync(string id);

    /// <summary>
    /// Asynchronously retrieves a list of all users who belong to a specific role.
    /// </summary>
    /// <param name="roleName">The name of the role (e.g., "Provider", "Customer").</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection
    /// of user view DTOs for all users found in the specified role.
    /// </returns>
    Task<IEnumerable<UserViewDto>> GetUsersInRoleAsync(string roleName);

    /// <summary>
    /// Replaces all existing roles for a user with a new set of roles.
    /// </summary>
    /// <remarks>
    /// This is a full replacement operation. Any roles the user currently has that are not in the provided list will be removed.
    ///  If the provided list of roles is empty, the user will be removed from all roles.
    /// </remarks>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="roles">A complete list of role names to be assigned to the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing an <see cref="IdentityResult"/> indicating success or failure.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if a user with the specified <paramref name="userId"/> does not exist.</exception>
    /// <exception cref="AppException">Thrown if any of the provided role names in the <paramref name="roles"/> list are invalid or do not exist in the system.</exception>
    Task<IdentityResult> UpdateUserRolesAsync(string userId, List<string> roles);
    
    /// <summary>
    /// Asynchronously disables a user's account, preventing them from logging in.
    /// </summary>
    /// <remarks>
    /// This method works by setting a "lockout" on the user's account to a date far in the future.
    /// It effectively prevents the user from signing in until their account is re-enabled.
    /// </remarks>
    /// <param name="userId">The user's unique identifier (typically the GUID stored as a string).</param>
    /// <returns>
    /// A task that represents the asynchronous operation, containing an <see cref="IdentityResult"/> indicating the success or failure of the lockout operation.
    /// </returns>
    /// <exception cref="EntityNotFoundException">Thrown if a user with the specified <paramref name="userId"/> does not exist.</exception>
    Task<IdentityResult> DisableUserAsync(string userId);

    /// <summary>
    /// Asynchronously enables a previously disabled user's account.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>
    /// A task that represents the asynchronous operation, containing an <see cref="IdentityResult"/> indicating the success or failure of the operation.
    /// </returns>
    /// <exception cref="EntityNotFoundException">Thrown if a user with the specified <paramref name="userId"/> does not exist.</exception>
    Task<IdentityResult> EnableUserAsync(string userId);
}
