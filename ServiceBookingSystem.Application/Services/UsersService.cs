using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Application.Services;

public class UsersService : IUsersService
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<ApplicationRole> roleManager;
    private readonly IEmailService emailService;
    private readonly ITemplateService templateService;
    private readonly IConfiguration configuration;
    private readonly ILogger<UsersService> logger;


    public UsersService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IEmailService emailService,
        ITemplateService templateService,
        IConfiguration configuration,
        ILogger<UsersService> logger)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.emailService = emailService;
        this.templateService = templateService;
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> CreateUserAsync(UserCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        
        logger.LogDebug("Attempting to create user with Email: {Email}", dto.Email);
        foreach (var roleName in dto.Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogWarning("Attempted to create user with non-existent role: {RoleName}", roleName);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleNotFound",
                    Description = $"The role '{roleName}' does not exist."
                });
            }
        }

        var user = new ApplicationUser
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            UserName = dto.Email,
            PhoneNumber = dto.PhoneNumber,
        };

        var result = await userManager.CreateAsync(user, dto.Password);

        if (result.Succeeded)
        {
            logger.LogInformation("User {UserId} with Email {Email} created successfully", user.Id, user.Email);

            await userManager.AddToRolesAsync(user, dto.Roles);

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var baseUrl = configuration["WebAppSettings:BaseUrl"];
            var confirmationLink =
                $"{baseUrl}/Identity/Account/ConfirmEmail?userId={user.Id}&code={System.Net.WebUtility.UrlEncode(token)}";

            var emailBody = await templateService.RenderTemplateAsync("ConfirmEmail.html",
                new Dictionary<string, string>
                {
                    { "UserName", user.FirstName },
                    { "ConfirmationLink", confirmationLink }
                });

            _ = emailService.SendEmailAsync(user.Email, "Confirm your email", emailBody);
        }
        else
        {
            logger.LogWarning("Failed to create user with Email {Email}. Errors: {Errors}", dto.Email,
                result.Errors.Select(e => e.Description));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> RegisterUserAsync(RegisterDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        logger.LogDebug("Attempting to register new user with Email: {Email} and Role: {Role}", dto.Email, dto.Role);

        // Validate Role: Only Customer or Provider allowed for public registration
        if (dto.Role != RoleConstants.Customer && dto.Role != RoleConstants.Provider)
        {
            logger.LogWarning("Invalid role registration attempt: {Role}", dto.Role);
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidRole",
                Description = "Invalid role selected. Allowed roles are 'Customer' or 'Provider'."
            });
        }

        var user = new ApplicationUser
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            UserName = dto.Email
        };

        var result = await userManager.CreateAsync(user, dto.Password);

        if (result.Succeeded)
        {
            logger.LogInformation("User {UserId} registered successfully", user.Id);
            await userManager.AddToRoleAsync(user, dto.Role);

            // Send Welcome Email (Reuse logic or create new template)
            // For now, reusing ConfirmEmail logic
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var baseUrl = configuration["WebAppSettings:BaseUrl"];
            var confirmationLink = $"{baseUrl}/Identity/Account/ConfirmEmail?userId={user.Id}&code={System.Net.WebUtility.UrlEncode(token)}";

            var emailBody = await templateService.RenderTemplateAsync("ConfirmEmail.html",
                new Dictionary<string, string>
                {
                    { "UserName", user.FirstName },
                    { "ConfirmationLink", confirmationLink }
                });

            _ = emailService.SendEmailAsync(user.Email, "Welcome to Service Booking System", emailBody);
        }
        else
        {
            logger.LogWarning("Failed to register user {Email}. Errors: {Errors}", dto.Email, result.Errors.Select(e => e.Description));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<UserViewDto>> GetAllUsersAsync(
        UserQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Fetching users with parameters: PageNumber={PageNumber}, PageSize={PageSize}, SortBy={SortBy}, SortDirection={SortDirection}, SearchTerm={SearchTerm}",
            parameters.PageNumber, parameters.PageSize, parameters.SortBy, parameters.SortDirection,
            parameters.SearchTerm);
        
        ArgumentNullException.ThrowIfNull(parameters);

        var baseQuery = userManager.Users;

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            baseQuery = baseQuery.Where(u =>
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm) ||
                (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(searchTerm))
            );
        }

        var orderedQuery = baseQuery.OrderBy(u => u.UserName);

        if (!string.IsNullOrWhiteSpace(parameters.SortBy))
        {
            switch (parameters.SortBy.ToLower())
            {
                case "email":
                    orderedQuery = parameters.SortDirection?.ToLower() == "desc"
                        ? baseQuery.OrderByDescending(u => u.Email)
                        : baseQuery.OrderBy(u => u.Email);
                    break;
            }
        }

        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        var pagedQuery = orderedQuery
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize);

        var users = await pagedQuery.ToListAsync(cancellationToken);

        var userDtos = new List<UserViewDto>();
        foreach (var user in users)
        {
            userDtos.Add(new UserViewDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = await userManager.GetRolesAsync(user)
            });
        }

        return new PagedResult<UserViewDto>(userDtos, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    /// <inheritdoc/>
    public async Task<UserViewDto?> GetUserByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(id);

        if (user is null)
        {
            return null;
        }

        var userRoles = await userManager.GetRolesAsync(user);

        var userDto = new UserViewDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Roles = userRoles,
        };

        return userDto;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserViewDto>> GetUsersInRoleAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Enumerable.Empty<UserViewDto>();
        }

        var users = await userManager.GetUsersInRoleAsync(roleName);
        if (!users.Any())
            return Enumerable.Empty<UserViewDto>();

        var dtos = new List<UserViewDto>();

        // n + 1 query problem but when you call GetRolesAsync() multiple times in the same DbContext lifetime, EF Core caches the query results per user.
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);

            dtos.Add(new UserViewDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            });
        }

        return dtos;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateUserAsync(UserUpdateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        logger.LogDebug("Attempting to update user with ID: {UserId}", dto.Id);

        var userToUpdate = await userManager.FindByIdAsync(dto.Id);

        if (userToUpdate is null)
        {
            throw new EntityNotFoundException(nameof(ApplicationUser), dto.Id);
        }

        userToUpdate.FirstName = dto.FirstName;
        userToUpdate.LastName = dto.LastName;
        userToUpdate.PhoneNumber = dto.PhoneNumber;

        if (!string.Equals(userToUpdate.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("User {UserId} email change detected. Old: {OldEmail}, New: {NewEmail}", dto.Id,
                userToUpdate.Email, dto.Email);

            var setEmailResult = await userManager.SetEmailAsync(userToUpdate, dto.Email);
            if (!setEmailResult.Succeeded)
            {
                logger.LogWarning("Failed to set new email for User {UserId}. Errors: {Errors}", dto.Id,
                    setEmailResult.Errors.Select(e => e.Description));
                return setEmailResult;
            }

            var setUserNameResult = await userManager.SetUserNameAsync(userToUpdate, dto.Email);
            if (!setUserNameResult.Succeeded)
            {
                logger.LogWarning("Failed to set new username for User {UserId}. Errors: {Errors}", dto.Id,
                    setUserNameResult.Errors.Select(e => e.Description));
                return setUserNameResult;
            }

            logger.LogInformation("User {UserId} email and username updated to {NewEmail}", dto.Id, dto.Email);

            if (!string.IsNullOrWhiteSpace(userToUpdate.Email))
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(userToUpdate);
                var baseUrl = configuration["WebAppSettings:BaseUrl"];
                var confirmationLink =
                    $"{baseUrl}/Identity/Account/ConfirmEmail?userId={userToUpdate.Id}&code={System.Net.WebUtility.UrlEncode(token)}";

                var emailBody = await templateService.RenderTemplateAsync("ConfirmEmail.html",
                    new Dictionary<string, string>
                    {
                        { "UserName", userToUpdate.FirstName },
                        { "ConfirmationLink", confirmationLink }
                    });

                _ = emailService.SendEmailAsync(userToUpdate.Email, "Confirm your new email", emailBody);
            }
        }

        var updateResult = await userManager.UpdateAsync(userToUpdate);
        if (updateResult.Succeeded)
        {
            logger.LogInformation("User {UserId} properties updated successfully", dto.Id);
        }
        else
        {
            logger.LogWarning("Failed to update user {UserId} properties. Errors: {Errors}", dto.Id,
                updateResult.Errors.Select(e => e.Description));
        }

        return updateResult;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        logger.LogDebug("Attempting to change password for User {UserId}", userId);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new EntityNotFoundException(nameof(ApplicationUser), userId);
        }

        var result = await userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);

        if (result.Succeeded)
        {
            logger.LogInformation("Password changed successfully for User {UserId}", userId);
        }
        else
        {
            logger.LogWarning("Failed to change password for User {UserId}. Errors: {Errors}", userId,
                result.Errors.Select(e => e.Description));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ConfirmEmailAsync(ConfirmEmailDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        logger.LogDebug("Attempting to confirm email for User {UserId}", dto.UserId);

        var user = await userManager.FindByIdAsync(dto.UserId);
        if (user is null)
        {
            throw new EntityNotFoundException(nameof(ApplicationUser), dto.UserId);
        }

        var result = await userManager.ConfirmEmailAsync(user, dto.Code);

        if (result.Succeeded)
        {
            logger.LogInformation("Email confirmed successfully for User {UserId}", dto.UserId);
        }
        else
        {
            logger.LogWarning("Failed to confirm email for User {UserId}. Errors: {Errors}", dto.UserId,
                result.Errors.Select(e => e.Description));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateUserRolesAsync(string userId, List<string> roles)
    {
        logger.LogDebug("Attempting to update roles for User {UserId} to: {Roles}", userId, roles);
        ArgumentNullException.ThrowIfNull(roles);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new EntityNotFoundException(nameof(ApplicationUser), userId);
        }

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                logger.LogWarning("Attempted to assign non-existent role {RoleName} to User {UserId}", roleName,
                    userId);
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleNotFound",
                    Description = $"The role '{roleName}' does not exist."
                });
            }
        }

        // Compute the delta roles

        // User's current roles.
        var currentRoles = await userManager.GetRolesAsync(user);
        logger.LogDebug("User {UserId} current roles: {CurrentRoles}", userId, currentRoles);

        // Roles to add are the new roles that are not in the current roles list.
        var rolesToAdd = roles.Except(currentRoles).ToList();

        //  Roles to remove are the current roles that are not in the new roles list.
        var rolesToRemove = currentRoles.Except(roles).ToList();
        logger.LogDebug("For User {UserId}: Roles to add: {RolesToAdd}, Roles to remove: {RolesToRemove}", userId,
            rolesToAdd, rolesToRemove);

        if (rolesToAdd.Any())
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                logger.LogWarning("Failed to add roles {RolesToAdd} to User {UserId}. Errors: {Errors}", rolesToAdd,
                    userId, addResult.Errors.Select(e => e.Description));
                return addResult;
            }

            logger.LogInformation("Successfully added roles {RolesToAdd} to User {UserId}", rolesToAdd, userId);
        }

        if (rolesToRemove.Any())
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                logger.LogWarning("Failed to remove roles {RolesToRemove} from User {UserId}. Errors: {Errors}",
                    rolesToRemove, userId, removeResult.Errors.Select(e => e.Description));
                return removeResult;
            }

            logger.LogInformation("Successfully removed roles {RolesToRemove} from User {UserId}", rolesToRemove,
                userId);
        }

        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> DisableUserAsync(string userId)
    {
        logger.LogDebug("Attempting to disable User {UserId}", userId);
        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            throw new EntityNotFoundException(nameof(ApplicationUser), userId);
        }

        var currentLockoutEnd = await userManager.GetLockoutEndDateAsync(user);
        if (currentLockoutEnd.HasValue && currentLockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            logger.LogDebug("User {UserId} is already disabled. No action taken.", userId);
            return IdentityResult.Success;
        }

        var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        if (result.Succeeded)
        {
            logger.LogInformation("User {UserId} has been disabled successfully", userId);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var emailBody = await templateService.RenderTemplateAsync("AccountDisabled.html",
                    new Dictionary<string, string>
                    {
                        { "UserName", user.FirstName }
                    });

                _ = emailService.SendEmailAsync(user.Email, "Account Disabled Notification", emailBody);
            }
        }
        else
        {
            logger.LogWarning("Failed to disable User {UserId}. Errors: {Errors}", userId,
                result.Errors.Select(e => e.Description));
        }

        return result;
    }
}