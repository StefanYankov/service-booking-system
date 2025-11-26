using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Application.Services;

public class UsersService : IUsersService
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<ApplicationRole> roleManager;
    private readonly IEmailService emailService;
    private readonly ITemplateService templateService;
    private readonly IConfiguration configuration;


    public UsersService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IEmailService emailService,
        ITemplateService templateService,
        IConfiguration configuration)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.emailService = emailService;
        this.templateService = templateService;
        this.configuration = configuration;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> CreateUserAsync(UserCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        foreach (var roleName in dto.Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
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

        return result;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<UserViewDto>> GetAllUsersAsync(
        UserQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
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
            var setEmailResult = await userManager.SetEmailAsync(userToUpdate, dto.Email);
            if (!setEmailResult.Succeeded)
            {
                return setEmailResult;
            }

            var setUserNameResult = await userManager.SetUserNameAsync(userToUpdate, dto.Email);
            if (!setUserNameResult.Succeeded)
            {
                return setUserNameResult;
            }

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

        return await userManager.UpdateAsync(userToUpdate);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateUserRolesAsync(string userId, List<string> roles)
    {
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

        // Roles to add are the new roles that are not in the current roles list.
        var rolesToAdd = roles.Except(currentRoles).ToList();

        //  Roles to remove are the current roles that are not in the new roles list.
        var rolesToRemove = currentRoles.Except(roles).ToList();
        if (rolesToAdd.Any())
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return addResult;
            }
        }

        if (rolesToRemove.Any())
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return removeResult;
            }
        }

        return IdentityResult.Success;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> DisableUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            throw new EntityNotFoundException(nameof(ApplicationUser), userId);
        }

        var currentLockoutEnd = await userManager.GetLockoutEndDateAsync(user);
        if (currentLockoutEnd.HasValue && currentLockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            return IdentityResult.Success;
        }

        var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var emailBody = await templateService.RenderTemplateAsync("AccountDisabled.html", new Dictionary<string, string>
                {
                    { "UserName", user.FirstName }
                });
            
                _ = emailService.SendEmailAsync(user.Email, "Account Disabled Notification", emailBody);
            }
        }

        return result;
    }
}