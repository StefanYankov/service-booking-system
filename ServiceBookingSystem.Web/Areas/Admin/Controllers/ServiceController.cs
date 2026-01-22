using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;

namespace ServiceBookingSystem.Web.Areas.Admin.Controllers;

/// <summary>
/// Manages service oversight for Administrators.
/// Allows viewing all services and banning (soft-deleting) them.
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleConstants.Administrator)]
[Route("[area]/[controller]/[action]")]
public class ServiceController : Controller
{
    private readonly IServiceService serviceService;
    private readonly ILogger<ServiceController> logger;

    public ServiceController(IServiceService serviceService, ILogger<ServiceController> logger)
    {
        this.serviceService = serviceService;
        this.logger = logger;
    }

    /// <summary>
    /// Displays a paginated list of all services (including inactive/deleted).
    /// </summary>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="sortBy">The field to sort by.</param>
    /// <param name="sortDirection">The sort direction (asc/desc).</param>
    /// <returns>The service list view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1, string? sortBy = null, string? sortDirection = null)
    {
        logger.LogDebug("Admin: Viewing Service List, Page {Page}", pageNumber);
        
        var parameters = new PagingAndSortingParameters
        {
            PageNumber = pageNumber,
            PageSize = 20,
            SortBy = sortBy ?? "Created",
            SortDirection = sortDirection ?? "Desc"
        };

        var result = await serviceService.GetServicesForAdminAsync(parameters);
        return View(result);
    }

    /// <summary>
    /// Bans (soft-deletes) a service.
    /// </summary>
    /// <param name="id">The ID of the service to ban.</param>
    /// <returns>Redirects to the index page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            logger.LogDebug("Admin: Deleting service {Id}", id);
            await serviceService.DeleteServiceByAdminAsync(id);
            TempData["SuccessMessage"] = "Service deleted/banned successfully.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete service {Id}", id);
            TempData["ErrorMessage"] = "Failed to delete service.";
        }

        return RedirectToAction(nameof(Index));
    }
}
