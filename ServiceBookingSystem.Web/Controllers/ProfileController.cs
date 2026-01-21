using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Identity.User;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Web.Models;

namespace ServiceBookingSystem.Web.Controllers;

/// <summary>
/// Manages the current user's profile, including personal details and password changes.
/// </summary>
[Authorize]
[Route("[controller]")]
public class ProfileController : Controller
{
    private readonly IUsersService usersService;
    private readonly ILogger<ProfileController> logger;

    public ProfileController(IUsersService usersService, ILogger<ProfileController> logger)
    {
        this.usersService = usersService;
        this.logger = logger;
    }

    /// <summary>
    /// Displays the user's profile page.
    /// </summary>
    /// <returns>The profile view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        logger.LogInformation("User {UserId} viewing profile", userId);

        var user = await usersService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var model = new ProfileViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = user.Roles
        };

        return View(model);
    }

    /// <summary>
    /// Updates the user's personal information.
    /// </summary>
    /// <param name="model">The profile data.</param>
    /// <returns>The profile view (redirects on success).</returns>
    [HttpPost("Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        if (model.Id != userId)
        {
            logger.LogWarning("User {UserId} attempted to update profile for {TargetId}", userId, model.Id);
            return Forbid();
        }

        logger.LogInformation("User {UserId} updating profile", userId);

        try
        {
            var dto = new UserUpdateDto
            {
                Id = userId,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await usersService.UpdateUserAsync(dto);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update profile for user {UserId}", userId);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
        }

        return View("Index", model);
    }

    /// <summary>
    /// Changes the user's password.
    /// </summary>
    /// <param name="model">The password change data.</param>
    /// <returns>The profile view (redirects on success).</returns>
    [HttpPost("ChangePassword")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            var user = await usersService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var profileModel = new ProfileViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Roles = user.Roles
            };
            
            ViewData["ChangePasswordModel"] = model;
            return View("Index", profileModel);
        }

        logger.LogInformation("User {UserId} changing password", userId);

        try
        {
            var dto = new ChangePasswordDto
            {
                OldPassword = model.OldPassword,
                NewPassword = model.NewPassword,
                ConfirmNewPassword = model.ConfirmPassword
            };

            var result = await usersService.ChangePasswordAsync(userId, dto);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to change password for user {UserId}", userId);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
        }

        // Re-fetch profile for view
        var userReload = await usersService.GetUserByIdAsync(userId);
        var profileReload = new ProfileViewModel
        {
            Id = userReload!.Id,
            Email = userReload.Email ?? string.Empty,
            FirstName = userReload.FirstName,
            LastName = userReload.LastName,
            PhoneNumber = userReload.PhoneNumber,
            Roles = userReload.Roles
        };
        ViewData["ChangePasswordModel"] = model;
        return View("Index", profileReload);
    }
}