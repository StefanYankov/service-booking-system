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

        // 1. Get Booking
        var booking = await this.bookingService.GetBookingByIdAsync(dto.BookingId, customerId, cancellationToken);
        if (booking == null)
        {
            this.logger.LogWarning("Create review failed: Booking {BookingId} not found or access denied.", dto.BookingId);
            throw new EntityNotFoundException(nameof(Booking), dto.BookingId);
        }

        // 2. Validate Booking State (Must be Completed)
        if (booking.Status != nameof(BookingStatus.Completed))
        {
            this.logger.LogWarning("Create review failed: Booking {BookingId} is not completed.", dto.BookingId);
            throw new InvalidOperationException("You can only review completed bookings.");
        }

        // 3. Validate Customer (Redundant if GetBookingById checks auth, but good for sanity)
        if (booking.CustomerId != customerId)
        {
             this.logger.LogWarning("Create review failed: User {UserId} is not the owner of booking {BookingId}.", customerId, dto.BookingId);
             throw new AuthorizationException(customerId, "Create Review");
        }

        // 4. Check for Duplicate Review (One per Booking)
        var alreadyReviewed = await this.dbContext.Reviews
            .AnyAsync(r => r.BookingId == dto.BookingId, cancellationToken);
        
        if (alreadyReviewed)
        {
            this.logger.LogWarning("Create Review failed: Booking {BookingId} has already been reviewed.", dto.BookingId);
            throw new InvalidOperationException("You have already reviewed this booking.");
        }

        // 5. Create Review
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

    public async Task<ReviewViewDto> UpdateReviewAsync(ReviewUpdateDto dto, string userId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<PagedResult<ReviewViewDto>> GetReviewsByServiceAsync(int serviceId, PagingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<ReviewSummaryDto> GetReviewSummaryAsync(int serviceId,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}