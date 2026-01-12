using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Web.Models;

namespace ServiceBookingSystem.Web.Controllers;

public class ServiceController : Controller
{
    private readonly IServiceService serviceService;
    private readonly ICategoryService categoryService;
    private readonly ILogger<ServiceController> logger;

    public ServiceController(
        IServiceService serviceService,
        ICategoryService categoryService,
        ILogger<ServiceController> logger)
    {
        this.serviceService = serviceService;
        this.categoryService = categoryService;
        this.logger = logger;
    }

    public async Task<IActionResult> Index(ServiceSearchParameters parameters)
    {
        logger.LogInformation("MVC: Viewing Service List with params: {@Params}", parameters);

        // 1. Fetch Services
        var services = await serviceService.SearchServicesAsync(parameters);

        // 2. Fetch Metadata for Filters
        var categories = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 });
        var cities = await serviceService.GetDistinctCitiesAsync();

        // 3. Build ViewModel
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

    public async Task<IActionResult> Details(int id)
    {
        logger.LogInformation("MVC: Viewing Service Details for ID: {ServiceId}", id);
        var service = await serviceService.GetServiceByIdAsync(id);

        if (service == null)
        {
            return NotFound();
        }

        return View(service);
    }
}