using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Web.Models;

namespace ServiceBookingSystem.Web.Controllers;

/// <summary>
/// Manages the public catalog of services, including listing, searching, and viewing details.
/// </summary>
public class ServiceController : Controller
{
    private readonly IServiceService serviceService;
    private readonly ICategoryService categoryService;
    private readonly IReviewService reviewService;
    private readonly ILogger<ServiceController> logger;
    
    private const int ReviewsPageSize = 5;

    public ServiceController(
        IServiceService serviceService,
        ICategoryService categoryService,
        IReviewService reviewService,
        ILogger<ServiceController> logger)
    {
        this.serviceService = serviceService;
        this.categoryService = categoryService;
        this.reviewService = reviewService;
        this.logger = logger;
    }

    /// <summary>
    /// Displays the list of services with filtering options.
    /// </summary>
    /// <param name="parameters">Search and filter parameters.</param>
    /// <returns>The service list view.</returns>
    public async Task<IActionResult> Index(ServiceSearchParameters parameters)
    {
        logger.LogInformation("MVC: Viewing Service List with params: {@Params}", parameters);

        var services = await serviceService.SearchServicesAsync(parameters);

        var categories = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 });

        var cities = await serviceService.GetDistinctCitiesAsync();

        var model = new ServiceListViewModel
        {
            Services = services,
            SearchParams = parameters,
            Categories = categories.Items.Select(c => new SelectListItem 
            { 
                Value = c.Id.ToString(), 
                Text = c.Name,
                Selected = parameters.CategoryId == c.Id
            }).ToList(),
            Cities = cities.Select(c => new SelectListItem 
            { 
                Value = c, 
                Text = c,
                Selected = parameters.City == c
            }).ToList()
        };

        return View(model);
    }

    /// <summary>
    /// Displays the details of a specific service, including reviews.
    /// </summary>
    /// <param name="id">The ID of the service.</param>
    /// <param name="pageNumber">The page number for reviews (default 1).</param>
    /// <returns>The service details view.</returns>
    public async Task<IActionResult> Details(int id, int pageNumber = 1)
    {
        logger.LogInformation("MVC: Viewing Service Details for ID: {ServiceId}", id);
        
        var service = await serviceService.GetServiceByIdAsync(id);
        if (service == null)
        {
            return NotFound();
        }

        var reviewParams = new PagingAndSortingParameters
        {
            PageNumber = pageNumber,
            PageSize = ReviewsPageSize,
            SortBy = "Date",
            SortDirection = "Desc"
        };

        var reviews = await reviewService.GetReviewsByServiceAsync(id, reviewParams);

        var model = new ServiceDetailsViewModel
        {
            Service = service,
            Reviews = reviews
        };

        return View(model);
    }
}