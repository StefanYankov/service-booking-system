using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;

namespace ServiceBookingSystem.Web.Areas.Admin.Controllers;

/// <summary>
/// Manages administrative tasks such as user management and system oversight.
/// Accessible only to users with the Administrator role.
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleConstants.Administrator)]
[Route("[area]/[controller]")]
public class AdminController : Controller
{
    private readonly IUsersService usersService;
    private readonly ILogger<AdminController> logger;

    public AdminController(IUsersService usersService, ILogger<AdminController> logger)
    {
        this.usersService = usersService;
        this.logger = logger;
    }

    /// <summary>
    /// Displays the main admin dashboard.
    /// Currently redirects to the User Management list.
    /// </summary>
    /// <returns>Redirects to Users action.</returns>
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// Displays a paginated list of all users in the system.
    /// </summary>
    /// <param name="pageNumber">The page number (default 1).</param>
    /// <param name="searchTerm">Optional search term for filtering users.</param>
    /// <returns>The user list view.</returns>
    [HttpGet("Users")]
    public async Task<IActionResult> Users(int pageNumber = 1, string? searchTerm = null)
    {
        logger.LogInformation("Admin viewing user list. Page: {Page}, Search: {Search}", pageNumber, searchTerm);

        var parameters = new UserQueryParameters
        {
            PageNumber = pageNumber,
            PageSize = 20,
            SearchTerm = searchTerm,
            SortBy = "Email",
            SortDirection = "Asc"
        };

        var users = await usersService.GetAllUsersAsync(parameters);

        // Pass search term to view for the input field
        ViewData["SearchTerm"] = searchTerm;

        return View(users);
    }

    /// <summary>
    /// Disables (bans) a user account.
    /// </summary>
    /// <param name="id">The ID of the user to disable.</param>
    /// <returns>Redirects back to the user list.</returns>
    [HttpPost("DisableUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableUser(string id)
    {
        logger.LogInformation("Admin disabling user {UserId}", id);

        try
        {
            await usersService.DisableUserAsync(id);
            TempData["SuccessMessage"] = "User disabled successfully.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to disable user {UserId}", id);
            TempData["ErrorMessage"] = "Failed to disable user.";
        }

        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// Enables (unbans) a user account.
    /// </summary>
    /// <param name="id">The ID of the user to enable.</param>
    /// <returns>Redirects back to the user list.</returns>
    [HttpPost("EnableUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableUser(string id)
    {
        logger.LogInformation("Admin enabling user {UserId}", id);

        try
        {
            await usersService.EnableUserAsync(id);
            TempData["SuccessMessage"] = "User enabled successfully.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enable user {UserId}", id);
            TempData["ErrorMessage"] = "Failed to enable user.";
        }

        return RedirectToAction(nameof(Users));
    }
}