using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.Interfaces;

namespace ServiceBookingSystem.Web.Controllers.Api;

/// <summary>
/// Manages user profile operations.
/// </summary>
[Area("Api")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUsersService usersService;
    private readonly ILogger<UsersController> logger;

    public UsersController(
        IUsersService usersService,
        ILogger<UsersController> logger)
    {
        this.usersService = usersService;
        this.logger = logger;
    }

    /// <summary>
    /// Retrieves the profile of the currently authenticated user.
    /// </summary>
    /// <returns>The user's profile details.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserViewDto>> GetMyProfile()
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: GetMyProfile request for User {UserId}", userId);

        if (userId == null) return Unauthorized();

        var user = await this.usersService.GetUserByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Updates the profile of the currently authenticated user.
    /// </summary>
    /// <param name="dto">The updated profile information.</param>
    /// <returns>The result of the update operation.</returns>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> UpdateMyProfile([FromBody] UserUpdateDto dto)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: UpdateMyProfile request for User {UserId}", userId);

        if (userId == null) return Unauthorized();

        if (dto.Id != userId)
        {
            logger
                .LogWarning("User {UserId} attempted to update profile for different ID {DtoId}", userId, dto.Id);
            return BadRequest("You can only update your own profile.");
        }

        var result = await this.usersService.UpdateUserAsync(dto);

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
}