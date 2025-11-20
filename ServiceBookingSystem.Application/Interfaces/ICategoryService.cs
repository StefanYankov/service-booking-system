using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.Application.Interfaces;

public interface ICategoryService
{
    /// <summary>
    /// Asynchronously creates a new category in the system based on the provided data.
    /// </summary>
    /// <param name="dto">A Data Transfer Object containing the information for the new category.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the unique identifier of the newly created category.
    /// </returns>
    /// <exception cref="DuplicateEntityException">Thrown when a category with the same name already exists.</exception>
    Task<int> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a category by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the category view DTO if found; otherwise, null.
    /// </returns>
    Task<CategoryViewDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves a paginated list of categories based on specified query parameters.
    /// </summary>
    /// <param name="parameters">The parameters for paging (PageNumber, PageSize) and sorting (SortBy, SortDirection).</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a paged result with the list of categories for the current page
    /// and pagination metadata.
    /// </returns>
    Task<PagedResult<CategoryViewDto>> GetAllAsync(PagingAndSortingParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates an existing category.
    /// </summary>
    /// <param name="dto">A Data Transfer Object containing the updated information for the category.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the category with the specified ID does not exist.</exception>
    /// <exception cref="DuplicateEntityException">Thrown when the category is being renamed to a name that is already in use by another category.</exception>
    Task UpdateAsync(CategoryUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously soft-deletes a category.
    /// </summary>
    /// <param name="id">The unique identifier of the category to delete.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the category with the specified ID does not exist.</exception>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
