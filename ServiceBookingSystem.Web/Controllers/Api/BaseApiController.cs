using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceBookingSystem.Web.Controllers.Api;

/// <summary>
/// A base controller for all API endpoints.
/// Provides common functionality like User ID retrieval and standard attributes.
/// Inherits from ControllerBase to avoid View overhead.
/// Enforces JWT Authentication for all API endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Retrieves the ID of the currently authenticated user from the claims.
    /// Returns null if the user is not authenticated.
    /// </summary>
    protected string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Retrieves the email of the currently authenticated user.
    /// </summary>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email);
    }
}