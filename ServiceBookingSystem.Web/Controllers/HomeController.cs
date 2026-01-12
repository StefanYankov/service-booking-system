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
    private readonly IServiceService serviceService;

    public HomeController(
        ILogger<HomeController> logger, 
        ICategoryService categoryService,
        IServiceService serviceService)
    {
        this.logger = logger;
        this.categoryService = categoryService;
        this.serviceService = serviceService;
    }

    public async Task<IActionResult> Index()
    {
        logger.LogInformation("User is viewing the Home page at {ViewTime}", DateTime.UtcNow);

        // Fetch all categories (using a large page size to get all)
        var categoryResult = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 }, CancellationToken.None);
        
        // Fetch distinct cities
        var cities = await serviceService.GetDistinctCitiesAsync(CancellationToken.None);
        
        var model = new HomeViewModel
        {
            Categories = categoryResult.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList(),
            Cities = cities.Select(c => new SelectListItem { Value = c, Text = c }).ToList()
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