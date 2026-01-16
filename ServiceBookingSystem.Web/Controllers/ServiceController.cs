using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;
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

    /// <summary>
    /// Displays the list of services owned by the currently logged-in provider.
    /// </summary>
    /// <param name="pageNumber">The page number (default 1).</param>
    /// <returns>The provider's service list view.</returns>
    [Authorize(Roles = RoleConstants.Provider)]
    [HttpGet]
    public async Task<IActionResult> MyServices(int pageNumber = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var parameters = new PagingAndSortingParameters
        {
            PageNumber = pageNumber,
            PageSize = 10,
            SortBy = "CreatedOn",
            SortDirection = "Desc"
        };

        var servicesResult = await serviceService.GetServicesByProviderAsync(userId, parameters);

        var items = servicesResult.Items.Select(s => new ProviderServiceListViewModel
        {
            Id = s.Id,
            Name = s.Name,
            CategoryName = s.CategoryName,
            Price = s.Price,
            IsActive = s.IsActive
        }).ToList();

        var model = new PagedResult<ProviderServiceListViewModel>(items, servicesResult.TotalCount, servicesResult.PageNumber, servicesResult.PageSize);

        return View(model);
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categories = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 });
        
        var model = new ServiceCreateViewModel
        {
            Categories = categories.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList()
        };
        
        return View(model);
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var categories = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 });
            model.Categories = categories.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            var dto = new ServiceCreateDto
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                DurationInMinutes = model.DurationInMinutes,
                CategoryId = model.CategoryId,
                IsOnline = model.IsOnline,
                StreetAddress = model.StreetAddress,
                City = model.City,
                PostalCode = model.PostalCode
            };

            await serviceService.CreateServiceAsync(dto, userId);
            TempData["SuccessMessage"] = "Service created successfully!";
            return RedirectToAction(nameof(MyServices));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create service.");
            ModelState.AddModelError(string.Empty, ex.Message);
            
            var categories = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 });
            model.Categories = categories.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            return View(model);
        }
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var service = await serviceService.GetServiceByIdAsync(id);
        if (service == null) return NotFound();

        if (service.ProviderId != userId) return Forbid();

        var categories = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 });

        var model = new ServiceUpdateViewModel
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            Price = service.Price,
            DurationInMinutes = service.DurationInMinutes,
            CategoryId = service.CategoryId,
            IsOnline = service.IsOnline,
            IsActive = service.IsActive,
            StreetAddress = service.StreetAddress,
            City = service.City,
            PostalCode = service.PostalCode,
            Categories = categories.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList(),
            ExistingImages = service.Images // Populate existing images
        };

        return View(model);
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ServiceUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var categories = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 });
            model.Categories = categories.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            // Reload images if validation fails
            var service = await serviceService.GetServiceByIdAsync(model.Id);
            if (service != null) model.ExistingImages = service.Images;
            
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            var dto = new ServiceUpdateDto
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                DurationInMinutes = model.DurationInMinutes,
                CategoryId = model.CategoryId,
                IsOnline = model.IsOnline,
                IsActive = model.IsActive,
                StreetAddress = model.StreetAddress,
                City = model.City,
                PostalCode = model.PostalCode
            };

            await serviceService.UpdateServiceAsync(dto, userId);

            // Handle Image Upload
            if (model.NewImage != null)
            {
                await serviceService.AddImageAsync(model.Id, userId, model.NewImage);
            }

            TempData["SuccessMessage"] = "Service updated successfully!";
            return RedirectToAction(nameof(MyServices));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update service.");
            ModelState.AddModelError(string.Empty, ex.Message);
            
            var categories = await categoryService.GetAllAsync(new PagingAndSortingParameters { PageSize = 100 });
            model.Categories = categories.Items.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
            // Reload images
            var service = await serviceService.GetServiceByIdAsync(model.Id);
            if (service != null) model.ExistingImages = service.Images;
            
            return View(model);
        }
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int serviceId, int imageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            await serviceService.DeleteImageAsync(serviceId, userId, imageId);
            TempData["SuccessMessage"] = "Image deleted successfully.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete image {ImageId}", imageId);
            TempData["ErrorMessage"] = "Failed to delete image.";
        }

        return RedirectToAction(nameof(Edit), new { id = serviceId });
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetThumbnail(int serviceId, int imageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            await serviceService.SetThumbnailAsync(serviceId, userId, imageId);
            TempData["SuccessMessage"] = "Thumbnail updated.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set thumbnail for service {ServiceId}", serviceId);
            TempData["ErrorMessage"] = "Failed to set thumbnail.";
        }

        return RedirectToAction(nameof(Edit), new { id = serviceId });
    }

    [Authorize(Roles = RoleConstants.Provider)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            await serviceService.DeleteServiceAsync(id, userId);
            TempData["SuccessMessage"] = "Service deleted successfully.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete service {ServiceId}", id);
            TempData["ErrorMessage"] = "Failed to delete service.";
        }

        return RedirectToAction(nameof(MyServices));
    }
}