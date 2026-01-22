using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;

namespace ServiceBookingSystem.Web.Controllers.Api;

[Area("Api")]
[Authorize(Roles = RoleConstants.Administrator)]
[Route("api/admin")]
public class AdminController : BaseApiController
{
    private readonly IUsersService usersService;
    private readonly IServiceService serviceService;
    private readonly ILogger<AdminController> logger;

    public AdminController(
        IUsersService usersService,
        IServiceService serviceService,
        ILogger<AdminController> logger)
    {
        this.usersService = usersService;
        this.serviceService = serviceService;
        this.logger = logger;
    }

    // --- User Management ---

    /// <summary>
    /// Retrieves a paginated list of all users.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<UserViewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserViewDto>>> GetAllUsers(
        [FromQuery] UserQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("API: Admin GetAllUsers request");
        var result = await this.usersService.GetAllUsersAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a specific user by ID.
    /// </summary>
    [HttpGet("users/{id}")]
    [ProducesResponseType(typeof(UserViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserViewDto>> GetUserById(string id)
    {
        logger.LogDebug("API: Admin GetUserById request for {UserId}", id);
        var user = await this.usersService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    /// <summary>
    /// Updates the roles for a specific user.
    /// </summary>
    [HttpPut("users/{id}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateUserRoles(string id, [FromBody] List<string> roles)
    {
        logger.LogDebug("API: Admin UpdateUserRoles request for {UserId}", id);
        
        if (roles == null || !roles.Any())
        {
            ModelState.AddModelError("Roles", "At least one role must be provided.");
            return BadRequest(ModelState);
        }

        var result = await this.usersService.UpdateUserRolesAsync(id, roles);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        return Ok();
    }

    /// <summary>
    /// Disables a user account (ban).
    /// </summary>
    [HttpPut("users/{id}/disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DisableUser(string id)
    {
        logger.LogDebug("API: Admin DisableUser request for {UserId}", id);
        var result = await this.usersService.DisableUserAsync(id);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        return Ok();
    }

    /// <summary>
    /// Enables a previously disabled user account (unban).
    /// </summary>
    [HttpPut("users/{id}/enable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EnableUser(string id)
    {
        logger.LogDebug("API: Admin EnableUser request for {UserId}", id);
        var result = await this.usersService.EnableUserAsync(id);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        return Ok();
    }

    // --- Service Management ---

    /// <summary>
    /// Retrieves a paginated list of all services (including inactive/deleted) for oversight.
    /// </summary>
    [HttpGet("services")]
    [ProducesResponseType(typeof(PagedResult<ServiceAdminViewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceAdminViewDto>>> GetAllServices(
        [FromQuery] PagingAndSortingParameters parameters,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("API: Admin GetAllServices request");
        var result = await this.serviceService.GetServicesForAdminAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Soft-deletes (bans) a service.
    /// </summary>
    [HttpDelete("services/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteService(int id, CancellationToken cancellationToken)
    {
        logger.LogDebug("API: Admin DeleteService request for {ServiceId}", id);
        await this.serviceService.DeleteServiceByAdminAsync(id, cancellationToken);
        return NoContent();
    }
}