using Microsoft.AspNetCore.Http;
using ServiceBookingSystem.Application.DTOs.Service;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.Application.Interfaces;

public interface IServiceService
{
    /// <summary>
    /// Creates a new service for a specified provider.
    /// </summary>
    /// <param name="dto">The data transfer object containing the details of the service to create.</param>
    /// <param name="providerId">The unique identifier of the user providing the service.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created service's view model.</returns>
    Task<ServiceViewDto> CreateServiceAsync(ServiceCreateDto dto, string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing service for a specified provider.
    /// </summary>
    /// <param name="dto">The data transfer object containing the updated details of the service.</param>
    /// <param name="providerId">The unique identifier of the user attempting to update the service, for ownership verification.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated service's view model.</returns>
    Task<ServiceViewDto> UpdateServiceAsync(ServiceUpdateDto dto, string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a service, marking it as inactive and preserving its data.
    /// </summary>
    /// <param name="serviceId">The unique identifier of the service to delete.</param>
    /// <param name="providerId">The unique identifier of the user attempting to delete the service, for ownership verification.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteServiceAsync(int serviceId, string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single service by its unique identifier.
    /// </summary>
    /// <param name="serviceId">The unique identifier of the service.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the service's view model, or null if not found.</returns>
    Task<ServiceViewDto?> GetServiceByIdAsync(int serviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated and sorted list of services belonging to a specific category.
    /// </summary>
    /// <param name="categoryId">The unique identifier of the category.</param>
    /// <param name="parameters">Parameters for paging and sorting.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a paged list of service view models.</returns>
    Task<PagedResult<ServiceViewDto>> GetServicesByCategoryAsync(int categoryId, PagingAndSortingParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated and sorted list of services offered by a specific provider.
    /// </summary>
    /// <param name="providerId">The unique identifier of the provider.</param>
    /// <param name="parameters">Parameters for paging and sorting.</param>
    /// <param name="cancellationToken">A token to allow the operation to be cancelled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a paged list of service view models.</returns>
    Task<PagedResult<ServiceViewDto>> GetServicesByProviderAsync(string providerId, PagingAndSortingParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for services based on various criteria.
    /// </summary>
    /// <param name="parameters">The search parameters (term, price range, category, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result of matching services.</returns>
    Task<PagedResult<ServiceViewDto>> SearchServicesAsync(ServiceSearchParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an image for a service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="userId">The ID of the user (provider) performing the upload.</param>
    /// <param name="file">The image file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL of the uploaded image.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the service is not found.</exception>
    /// <exception cref="AuthorizationException">Thrown if the user is not the owner of the service.</exception>
    Task<string> AddImageAsync(int serviceId, string userId, IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image from a service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="userId">The ID of the user (provider) performing the deletion.</param>
    /// <param name="imageId">The ID of the image to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="EntityNotFoundException">Thrown if the image or service is not found.</exception>
    /// <exception cref="AuthorizationException">Thrown if the user is not the owner of the service.</exception>
    Task DeleteImageAsync(int serviceId, string userId, int imageId, CancellationToken cancellationToken = default);
}