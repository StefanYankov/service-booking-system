using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    public UsersService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> CreateUserAsync(UserCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var user = new ApplicationUser
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            UserName = dto.Email,
        };

        var result = await userManager.CreateAsync(user, dto.Password);

        if (result.Succeeded)
        {
            if (await roleManager.RoleExistsAsync(dto.Role))
            {
                await userManager.AddToRoleAsync(user, dto.Role);
            }
            else
            {
                var error = new IdentityError
                {
                    Code = "RoleNotFound",
                    Description = $"The role '{dto.Role}' does not exist.",
                };
                return IdentityResult.Failed(error);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateUserAsync(UserUpdateDto dto)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<PagedResult<UserViewDto>> GetAllUsersAsync(PagingAndSortingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var baseQuery = userManager.Users.AsQueryable();
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var query = baseQuery;
        switch (parameters.SortBy?.ToLower())
        {
            case "username":
                query = parameters.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.UserName)
                    : query.OrderBy(u => u.UserName);
                break;
            case "email":
                query = parameters.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email);
                break;
            default:
                query = query.OrderBy(u => u.UserName);
                break;
        }

        var pagedQuery = query
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
    public async Task<IdentityResult> UpdateUserRolesAsync(string userId, List<string> roles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
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
        throw new NotImplementedException();
    }
}