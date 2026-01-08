using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Application.DTOs.Review;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Core.Constants;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.Application.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<ReviewService> logger;
    private readonly IBookingService bookingService;


    public ReviewService(
        ApplicationDbContext dbContext,
        ILogger<ReviewService> logger,
        IBookingService bookingService)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        this.bookingService = bookingService;
    }

    /// <inheritdoc/>
    public async Task<ReviewViewDto> CreateReviewAsync(ReviewCreateDto dto, string customerId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Attempting to create review for Booking {BookingId} by Customer {CustomerId}",
                dto?.BookingId, customerId);
        
        ArgumentNullException.ThrowIfNull(dto);
        
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new ArgumentException(ExceptionMessages.InvalidCustomerId, nameof(customerId));
        }

        var booking = await this.bookingService.GetBookingByIdAsync(dto.BookingId, customerId, cancellationToken);
        if (booking == null)
        {
            this.logger.LogWarning("Create review failed: Booking {BookingId} not found or access denied.", dto.BookingId);
            throw new EntityNotFoundException(nameof(Booking), dto.BookingId);
        }

        if (booking.Status != BookingStatus.Completed.ToString())
        {
            this.logger
                .LogWarning("Create review failed: Booking {BookingId} is not completed.", dto.BookingId);
            throw new InvalidOperationException("You can only review completed bookings.");
        }

        if (booking.CustomerId != customerId)
        {
             this.logger.LogWarning("Create review failed: User {UserId} is not the owner of booking {BookingId}.", customerId, dto.BookingId);
             throw new AuthorizationException(customerId, "Create Review");
        }

        var alreadyReviewed = await this.dbContext.Reviews
            .AnyAsync(r => r.BookingId == dto.BookingId, cancellationToken);
        
        if (alreadyReviewed)
        {
            this.logger.LogWarning("Create review failed: Booking {BookingId} has already been reviewed.", dto.BookingId);
            throw new InvalidOperationException("You have already reviewed this booking.");
        }

        var review = new Review
        {
            BookingId = dto.BookingId,
            ServiceId = booking.ServiceId,
            CustomerId = customerId,
            Rating = dto.Rating,
            Comment = dto.Comment
        };

        await this.dbContext.Reviews.AddAsync(review, cancellationToken);

        try
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
            this.logger
                .LogInformation(
                    "Review {ReviewId} created successfully for Booking {BookingId} (Service {ServiceId}) by Customer {CustomerId}.",
                    review.Id, dto.BookingId, booking.ServiceId, customerId);
        }
        catch (Exception ex)
        {
            this.logger
                .LogError(ex, "Failed to save review for Booking {BookingId}.",
                    dto.BookingId);
            throw;
        }

        var viewDto = new ReviewViewDto
        {
            Id = review.Id,
            ServiceId = booking.ServiceId,
            ServiceName = booking.ServiceName,
            CustomerId = customerId,
            CustomerName = booking.CustomerName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedOn = review.CreatedOn
        };
        return viewDto;
    }

    /// <inheritdoc/>
    public async Task<ReviewViewDto> UpdateReviewAsync(ReviewUpdateDto dto, string userId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Attempting to update review {ReviewId} by user {UserId}",
                dto?.Id, userId);
        
        ArgumentNullException.ThrowIfNull(dto);
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(ExceptionMessages.InvalidCustomerId, nameof(userId));
        }

        var review = await this.dbContext.Reviews
            .Include(r => r.Service)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == dto.Id, cancellationToken);

        if (review == null)
        {
            this.logger
                .LogWarning("Update review failed: Review {ReviewId} not found.",
                    dto.Id);
            throw new EntityNotFoundException(nameof(Review), dto.Id);
        }

        if (review.CustomerId != userId)
        {
            this.logger
                .LogWarning("Update review failed: User {UserId} is not the author of review {ReviewId}.",
                    userId, dto.Id);
            throw new AuthorizationException(userId, $"Update Review {dto.Id}");
        }

        review.Rating = dto.Rating;
        review.Comment = dto.Comment;

        try
        {
            await this.dbContext.SaveChangesAsync(cancellationToken);
            this.logger
                .LogInformation("Review {ReviewId} updated successfully.",
                dto.Id);
        }
        catch (Exception ex)
        {
            this.logger
                .LogError(ex, "Failed to update review {ReviewId}.",
                    dto.Id);
            throw;
        }

        return new ReviewViewDto
        {
            Id = review.Id,
            ServiceId = review.ServiceId,
            ServiceName = review.Service.Name,
            CustomerId = review.CustomerId,
            CustomerName = $"{review.Customer.FirstName} {review.Customer.LastName}",
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedOn = review.CreatedOn,
            LastModifiedOn = review.ModifiedOn
        };
    }

    /// <inheritdoc/>
    public async Task<PagedResult<ReviewViewDto>> GetReviewsByServiceAsync(int serviceId, PagingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Retrieving reviews for Service {ServiceId}. Page: {Page}, Size: {Size}",
                serviceId, parameters.PageNumber, parameters.PageSize);

        var query = this.dbContext.Reviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Service)
            .Where(r => r.ServiceId == serviceId)
            .OrderByDescending(r => r.CreatedOn);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(r => new ReviewViewDto
            {
                Id = r.Id,
                ServiceId = r.ServiceId,
                ServiceName = r.Service.Name,
                CustomerId = r.CustomerId,
                CustomerName = $"{r.Customer.FirstName} {r.Customer.LastName}",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedOn = r.CreatedOn,
                LastModifiedOn = r.ModifiedOn
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewViewDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    /// <inheritdoc/>
    public async Task<ReviewSummaryDto> GetReviewSummaryAsync(int serviceId,
        CancellationToken cancellationToken = default)
    {
        this.logger
            .LogDebug("Retrieving review summary for Service {ServiceId}",
                serviceId);

        var query = this.dbContext.Reviews
            .AsNoTracking()
            .Where(r => r.ServiceId == serviceId);

        var totalReviews = await query.CountAsync(cancellationToken);
        
        double averageRating = 0;
        if (totalReviews > 0)
        {
            averageRating = await query.AverageAsync(r => r.Rating, cancellationToken);
        }

        var dto = new ReviewSummaryDto
        {
            ServiceId = serviceId,
            TotalReviews = totalReviews,
            AverageRating = Math.Round(averageRating, 1)
        };
        return dto;
    }
}