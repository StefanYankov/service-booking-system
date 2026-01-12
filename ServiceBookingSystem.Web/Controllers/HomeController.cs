using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Web.Models;

namespace ServiceBookingSystem.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> logger;
    private readonly ICategoryService categoryService;

    public HomeController(ILogger<HomeController> logger, ICategoryService categoryService)
    {
        this.logger = logger;
        this.categoryService = categoryService;
    }

    public async Task<IActionResult> Index()
    {
        logger.LogInformation("User is viewing the Home page at {ViewTime}", DateTime.UtcNow);

        // Fetch all categories (using a large page size to get all)
        var categoryResult = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 }, CancellationToken.None);
        
        var model = new HomeViewModel
        {
            Categories = categoryResult.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList(),
            // TODO: Fetch distinct cities from ServiceService once implemented
            Cities = new List<SelectListItem>
            {
                new SelectListItem { Value = "Sofia", Text = "Sofia" },
                new SelectListItem { Value = "Plovdiv", Text = "Plovdiv" },
                new SelectListItem { Value = "Varna", Text = "Varna" }
            }
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
    public IActionResult Terms()
    {
        return View();
    }
    
    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}