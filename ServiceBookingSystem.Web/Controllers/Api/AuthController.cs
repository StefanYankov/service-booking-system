using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Web.Controllers.Api;

/// <summary>
/// Controller responsible for handling authentication requests (Login, Register, Password Management).
/// It issues JWT tokens for valid credentials.
/// </summary>
public class AuthController : BaseApiController
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ITokenService tokenService;
    private readonly IUsersService usersService;
    private readonly ILogger<AuthController> logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IUsersService usersService,
        ILogger<AuthController> logger)
    {
        this.userManager = userManager;
        this.tokenService = tokenService;
        this.usersService = usersService;
        this.logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="dto">The login credentials (email and password).</param>
    /// <returns>A JSON object containing the JWT token.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<object>> Login([FromBody] LoginDto dto)
    {
        logger
            .LogDebug("API: Login attempt for user {Email}",
                dto.Email);

        var user = await this.userManager.FindByEmailAsync(dto.Email);
        
        if (user == null || !await this.userManager.CheckPasswordAsync(user, dto.Password))
        {
            logger
                .LogWarning("API: Login failed for user {Email}. Invalid credentials.",
                    dto.Email);
            return Unauthorized("Invalid email or password.");
        }

        var roles = await this.userManager.GetRolesAsync(user);

        var token = this.tokenService.GenerateToken(user, roles);

        logger
            .LogInformation("API: Login successful for user {Email}.",
                dto.Email);

        return Ok(new { Token = token });
    }

    /// <summary>
    /// Registers a new user (Customer or Provider) and returns a JWT token.
    /// </summary>
    /// <param name="dto">The registration details.</param>
    /// <returns>A JSON object containing the JWT token.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> Register([FromBody] RegisterDto dto)
    {
        logger
            .LogDebug("API: Register attempt for user {Email} as {Role}",
                dto.Email, dto.Role);

        var result = await this.usersService.RegisterUserAsync(dto);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        // Auto-login after registration
        var user = await this.userManager.FindByEmailAsync(dto.Email);
        // User exists and role is assigned because RegisterUserAsync succeeded
        var roles = new List<string> { dto.Role }; 

        var token = this.tokenService.GenerateToken(user!, roles);

        logger.LogInformation("API: Registration successful for user {Email}.", dto.Email);

        return Ok(new { Token = token });
    }

    /// <summary>
    /// Changes the password for the currently authenticated user.
    /// </summary>
    /// <param name="dto">The password change details.</param>
    /// <returns>Status 200 OK if successful.</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = this.GetCurrentUserId();
        logger
            .LogDebug("API: ChangePassword request for User {UserId}",
                userId);

        if (userId == null) return Unauthorized();

        var result = await this.usersService.ChangePasswordAsync(userId, dto);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        logger.LogInformation("API: Password changed successfully for User {UserId}", userId);
        return Ok();
    }

    /// <summary>
    /// Confirms a user's email address using a token.
    /// </summary>
    /// <param name="dto">The confirmation details (UserId and Token).</param>
    /// <returns>Status 200 OK if successful.</returns>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
    {
        logger.LogDebug("API: ConfirmEmail request for User {UserId}", dto.UserId);

        var result = await this.usersService.ConfirmEmailAsync(dto);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        logger.LogInformation("API: Email confirmed successfully for User {UserId}", dto.UserId);
        return Ok();
    }
}