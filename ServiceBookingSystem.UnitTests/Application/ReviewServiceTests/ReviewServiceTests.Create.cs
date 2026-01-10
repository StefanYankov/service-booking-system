using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Review;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.ReviewServiceTests;

public partial class ReviewServiceTests
{
    [Fact]
    public async Task CreateReviewAsync_WithValidData_ShouldCreateReviewAndReturnDto()
    {
        // Arrange:
        const string customerId = "customer-1";
        const string bookingId = "booking-1";
        const int serviceId = 1;
        var dto = new ReviewCreateDto
        {
            BookingId = bookingId,
            Rating = 5,
            Comment = "Great service!"
        };

        var provider = new ApplicationUser 
        {
            Id = "provider-1",
                FirstName = "Test",
                LastName = "Provider"
        };
        
        var customer = new ApplicationUser 
        {
            Id = customerId,
                FirstName = "Test",
                LastName = "Customer"
        };
        
        var service = new Service 
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = "provider-1",
            Provider = provider
        };
        
        var booking = new Booking 
        {
            Id = bookingId,
            ServiceId = serviceId,
            CustomerId = customerId,
            BookingStart = DateTime.UtcNow.AddDays(-1),
            Status = BookingStatus.Completed
        };
        
        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        var bookingDto = new BookingViewDto 
        { 
            Id = bookingId, 
            ServiceId = serviceId, 
            ServiceName = "Test Service",
            CustomerId = customerId,
            CustomerName = "Test Customer",
            Status = "Completed"
        };

        bookingServiceMock
            .Setup(x => x.GetBookingByIdAsync(bookingId, customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookingDto);

        // Act:
        var result = await reviewService.CreateReviewAsync(dto, customerId);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(5, result.Rating);
        Assert.Equal("Great service!", result.Comment);
        
        // Verify DB persistence
        var savedReview = await dbContext.Reviews.FirstOrDefaultAsync();
        Assert.NotNull(savedReview);
        Assert.Equal(bookingId, savedReview.BookingId);
        Assert.Equal(serviceId, savedReview.ServiceId);
    }

    [Fact]
    public async Task CreateReviewAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Act & Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            reviewService.CreateReviewAsync(null!, "customer-1"));
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData("   ")] // Whitespace
    [InlineData(null)] // Null
    public async Task CreateReviewAsync_WithInvalidCustomerId_ShouldThrowArgumentException(string? customerId)
    {
        // Arrange:
        var dto = new ReviewCreateDto
        {
            BookingId = "b1",
            Rating = 5
        };

        // Act & Assert:
        await Assert.ThrowsAsync<ArgumentException>(() => 
            reviewService.CreateReviewAsync(dto, customerId!));
    }

    [Fact]
    public async Task CreateReviewAsync_WhenBookingNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        var dto = new ReviewCreateDto
        {
            BookingId = "non-existent",
            Rating = 5
        };
        bookingServiceMock
            .Setup(x => x.GetBookingByIdAsync("non-existent", "customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingViewDto?)null);

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            reviewService.CreateReviewAsync(dto, "customer-1"));
    }

    [Fact]
    public async Task CreateReviewAsync_WhenBookingNotCompleted_ShouldThrowInvalidBookingStateException()
    {
        // Arrange:
        const string bookingId = "b1";
        var dto = new ReviewCreateDto
        {
            BookingId = bookingId,
            Rating = 5
        };
        var bookingDto = new BookingViewDto
        {
            Id = bookingId,
            Status = "Pending",
            CustomerId = "customer-1"
        };

        bookingServiceMock
            .Setup(x => x.GetBookingByIdAsync(bookingId, "customer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookingDto);

        // Act & Assert:
        await Assert.ThrowsAsync<InvalidBookingStateException>(() => 
            reviewService.CreateReviewAsync(dto, "customer-1"));
    }

    [Fact]
    public async Task CreateReviewAsync_WhenAlreadyReviewed_ShouldThrowDuplicateEntityException()
    {
        // Arrange:
        const string customerId = "customer-1";
        const string bookingId = "booking-1";
        const int serviceId = 1;
        var dto = new ReviewCreateDto
        {
            BookingId = bookingId,
            Rating = 5
        };
        
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            FirstName = "Test",
            LastName = "Provider"
        };
        
        var customer = new ApplicationUser
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "Customer"
        };
        
        var service = new Service 
        {
            Id = serviceId,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = "provider-1",
            Provider = provider
        };
        
        var booking = new Booking 
        {
            Id = bookingId,
            ServiceId = serviceId, 
            CustomerId = customerId,
            BookingStart = DateTime.UtcNow.AddDays(-1),
            Status = BookingStatus.Completed
        };
        
        var existingReview = new Review
        {
            BookingId = bookingId,
            ServiceId = serviceId,
            CustomerId = customerId,
            Rating = 4,
            Comment = "Old Review"
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.Reviews.AddAsync(existingReview);
        await dbContext.SaveChangesAsync();

        var bookingDto = new BookingViewDto 
        { 
            Id = bookingId, 
            ServiceId = serviceId, 
            CustomerId = customerId,
            Status = "Completed"
        };

        bookingServiceMock
            .Setup(x => x.GetBookingByIdAsync(bookingId, customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookingDto);

        // Act & Assert:
        await Assert.ThrowsAsync<DuplicateEntityException>(() => 
            reviewService.CreateReviewAsync(dto, customerId));
    }
}