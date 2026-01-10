using ServiceBookingSystem.Application.DTOs.Review;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Core.Exceptions;

namespace ServiceBookingSystem.Application.Interfaces;

/// <summary>
/// Defines the contract for managing customer reviews and ratings.
/// Provides methods for creating, updating, and retrieving reviews for services.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Creates a new review for a service.
    /// Validates that the customer has a completed booking for the service.
    /// </summary>
    /// <param name="dto">The review data (Rating, Comment, ServiceId).</param>
    /// <param name="customerId">The ID of the customer creating the review.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created review details.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the booking is not found.</exception>
    /// <exception cref="InvalidBookingStateException">Thrown if the booking is not in 'Completed' status.</exception>
    /// <exception cref="AuthorizationException">Thrown if the user is not the owner of the booking.</exception>
    /// <exception cref="DuplicateEntityException">Thrown if the user has already reviewed this booking.</exception>
    Task<ReviewViewDto> CreateReviewAsync(ReviewCreateDto dto, string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing review.
    /// Validates that the user is the author of the review.
    /// </summary>
    /// <param name="dto">The updated review data.</param>
    /// <param name="userId">The ID of the user attempting the update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated review details.</returns>
    /// <exception cref="EntityNotFoundException">Thrown if the review is not found.</exception>
    /// <exception cref="AuthorizationException">Thrown if the user is not the author of the review.</exception>
    Task<ReviewViewDto> UpdateReviewAsync(ReviewUpdateDto dto, string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paged list of reviews for a specific service.
    /// </summary>
    /// <param name="serviceId">The ID of the service to retrieve reviews for.</param>
    /// <param name="parameters">Paging and sorting parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result of reviews.</returns>
    Task<PagedResult<ReviewViewDto>> GetReviewsByServiceAsync(int serviceId, PagingParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a summary of reviews for a specific service (Average Rating, Total Count).
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summary object containing the average rating and total count.</returns>
    Task<ReviewSummaryDto> GetReviewSummaryAsync(int serviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a review by its ID.
    /// </summary>
    /// <param name="reviewId">The ID of the review.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The review details if found; otherwise, null.</returns>
    Task<ReviewViewDto?> GetByIdAsync(int reviewId, CancellationToken cancellationToken = default);
}