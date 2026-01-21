using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;

namespace ServiceBookingSystem.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleConstants.Administrator)]
public class CategoryController : Controller
{
    private readonly ICategoryService categoryService;
    private readonly ILogger<CategoryController> logger;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        this.categoryService = categoryService;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1)
    {
        logger.LogDebug("Admin: Viewing Category List, Page {Page}", pageNumber);
        
        var parameters = new PagingAndSortingParameters
        {
            PageNumber = pageNumber,
            PageSize = 10,
            SortBy = "Name",
            SortDirection = "Asc"
        };

        var result = await categoryService.GetAllAsync(parameters);
        return View(result);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryCreateDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            logger.LogDebug("Admin: Creating category {Name}", model.Name);
            await categoryService.CreateAsync(model);
            TempData["SuccessMessage"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create category");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await categoryService.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        var model = new CategoryUpdateDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryUpdateDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            logger.LogDebug("Admin: Updating category {Id}", model.Id);
            await categoryService.UpdateAsync(model);
            TempData["SuccessMessage"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update category {Id}", model.Id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            logger.LogDebug("Admin: Deleting category {Id}", id);
            await categoryService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Category deleted successfully.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete category {Id}", id);
            TempData["ErrorMessage"] = "Failed to delete category.";
        }

        return RedirectToAction(nameof(Index));
    }
}
