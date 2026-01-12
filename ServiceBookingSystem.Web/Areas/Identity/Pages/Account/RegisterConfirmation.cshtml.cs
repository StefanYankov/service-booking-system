using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterConfirmationModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public string Email { get; set; } = string.Empty;

    public bool DisplayConfirmAccountLink { get; set; }

    public string? EmailConfirmationUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string email, string? returnUrl = null)
    {
        if (email == null)
        {
            return RedirectToPage("/Index");
        }
        returnUrl = returnUrl ?? Url.Content("~/");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound($"Unable to load user with email '{email}'.");
        }

        Email = email;
        
        // In a real app, we don't display the link. 
        // But for development/demo without a real email sender, we might want to.
        // However, we have NullEmailService which logs it, or SendGrid.
        // Let's stick to "Check your email".
        DisplayConfirmAccountLink = false;

        return Page();
    }
}