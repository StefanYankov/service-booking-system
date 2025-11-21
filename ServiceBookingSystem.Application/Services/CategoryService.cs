using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Application.DTOs.Category;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext dbContext;

    public CategoryService(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async Task<int> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        await this.ValidateNameIsUniqueAsync(dto.Name, null, cancellationToken);

        var categoryEntity = new Category()
        {
            Name = dto.Name,
            Description = dto.Description
        };

        dbContext.Categories.Add(categoryEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return categoryEntity.Id;
    }

    /// <inheritdoc/>
    public async Task<CategoryViewDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await this.dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);


        if (category is null)
        {
            return null;
        }

        var dtoToReturn = new CategoryViewDto()
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };

        return dtoToReturn;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<CategoryViewDto>> GetAllAsync(PagingAndSortingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var baseQuery = dbContext.Categories.AsNoTracking();

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var query = baseQuery;

        var sortBy = parameters.SortBy?.ToLower();
        var sortDirection = parameters.SortDirection?.ToLower();
        var isDescending = sortDirection == "desc";

        if (sortBy == "name")
        {
            query = isDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name);
        }
        else
        {
            // Default sort order is by Id, ascending.
            query = query.OrderBy(c => c.Id);
        }

        var pagedQuery = query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize);

        var items = await pagedQuery
            .Select(c => new CategoryViewDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryViewDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(CategoryUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }
        
        await this.ValidateNameIsUniqueAsync(dto.Name, dto.Id, cancellationToken);
        var category = await this.dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.Id, cancellationToken);
        
        if (category is null)
        {
            throw new EntityNotFoundException(nameof(Category), dto.Id);
        }
        
        category.Name = dto.Name;
        category.Description = dto.Description;

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var categoryToDelete = await this.dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (categoryToDelete is null)
        {
            throw new EntityNotFoundException(nameof(Category), id);
        }

        this.dbContext.Categories.Remove(categoryToDelete);
        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Checks if a category name is unique, optionally excluding a specific category ID from the check.
    /// </summary>
    /// <param name="name">The name to check for uniqueness.</param>
    /// <param name="excludeId">The ID of the category to exclude from the check (used during updates).</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <exception cref="DuplicateEntityException">Thrown if the name is already in use by another category.</exception>
    private async Task ValidateNameIsUniqueAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        var query = this.dbContext.Categories.AsQueryable();

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        var nameAlreadyExists = await query.AnyAsync(c => c.Name == name, cancellationToken);

        if (nameAlreadyExists)
        {
            throw new DuplicateEntityException(nameof(Category), name);
        }
    }
}