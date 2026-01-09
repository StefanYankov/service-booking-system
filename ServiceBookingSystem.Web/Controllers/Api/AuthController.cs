using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Web.Controllers.Api;

/// <summary>
/// Controller responsible for handling authentication requests (Login).
/// It issues JWT tokens for valid credentials.
/// </summary>
public class AuthController : BaseApiController
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ITokenService tokenService;
    private readonly ILogger<AuthController> logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        this.userManager = userManager;
        this.tokenService = tokenService;
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
}