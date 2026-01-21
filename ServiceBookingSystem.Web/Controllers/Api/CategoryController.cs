using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Common;

namespace ServiceBookingSystem.Web.Controllers.Api;

/// <summary>
/// API Controller for managing service categories.
/// </summary>
[Area("Api")]
[Route("api/categories")]
public class CategoryController : BaseApiController
{
    private readonly ICategoryService categoryService;
    private readonly ILogger<CategoryController> logger;

    public CategoryController(
        ICategoryService categoryService,
        ILogger<CategoryController> logger)
    {
        this.categoryService = categoryService;
        this.logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of all categories.
    /// </summary>
    /// <param name="parameters">Pagination and sorting parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result of categories.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<CategoryViewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CategoryViewDto>>> GetAll(
        [FromQuery] PagingAndSortingParameters parameters,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("API: GetAll categories request. Page: {Page}, Size: {Size}", parameters.PageNumber, parameters.PageSize);
        var result = await categoryService.GetAllAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a specific category by ID.
    /// </summary>
    /// <param name="id">The ID of the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category details.</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryViewDto>> GetById(int id, CancellationToken cancellationToken)
    {
        logger.LogDebug("API: GetById category request for ID: {CategoryId}", id);
        var category = await categoryService.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return NotFound();
        }
        return Ok(category);
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="dto">The category creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created category.</returns>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Administrator)]
    [ProducesResponseType(typeof(CategoryViewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryViewDto>> Create([FromBody] CategoryCreateDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        logger.LogDebug("API: Create category request by User {UserId}. Name: {CategoryName}", userId, dto.Name);

        var result = await categoryService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">The ID of the category to update.</param>
    /// <param name="dto">The updated category data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = RoleConstants.Administrator)]
    [ProducesResponseType(typeof(CategoryViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryViewDto>> Update(int id, [FromBody] CategoryUpdateDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        logger.LogDebug("API: Update category request by User {UserId} for ID: {CategoryId}", userId, id);

        if (id != dto.Id)
        {
            return BadRequest("ID mismatch");
        }

        var result = await categoryService.UpdateAsync(dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a category.
    /// </summary>
    /// <param name="id">The ID of the category to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No Content.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = RoleConstants.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        logger.LogDebug("API: Delete category request by User {UserId} for ID: {CategoryId}", userId, id);

        await categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
