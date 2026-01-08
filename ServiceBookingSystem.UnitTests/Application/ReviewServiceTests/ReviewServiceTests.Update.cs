using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Application.DTOs.Review;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit;

namespace ServiceBookingSystem.UnitTests.Application.ReviewServiceTests;

public partial class ReviewServiceTests
{
    [Fact]
    public async Task UpdateReviewAsync_WithValidData_ShouldUpdateReviewAndReturnDto()
    {
        // Arrange:
        const string customerId = "customer-1";
        const int reviewId = 1;
        const int serviceId = 1;
        
        var provider = new ApplicationUser 
        {
            Id = "provider-1",
            FirstName = "Test",
            LastName = "Provider"
        };
        
        var customer = new ApplicationUser 
        {
            Id = customerId, FirstName = "Test", LastName = "Customer"
        };
        
        var service = new Service
        {
            Id = serviceId, Name = "Test Service",
            Description = "Test Description",
            ProviderId = "provider-1",
            Provider = provider
        };
        
        var review = new Review 
        { 
            Id = reviewId, 
            ServiceId = serviceId, 
            Service = service,
            CustomerId = customerId, 
            Customer = customer,
            BookingId = "b1",
            Rating = 4, 
            Comment = "Old Comment" 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Reviews.AddAsync(review);
        await dbContext.SaveChangesAsync();

        var dto = new ReviewUpdateDto
        {
            Id = reviewId,
            Rating = 5,
            Comment = "New Comment"
        };

        // Act:
        var result = await reviewService.UpdateReviewAsync(dto, customerId);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(5, result.Rating);
        Assert.Equal("New Comment", result.Comment);
        
        var updatedReview = await dbContext.Reviews.FindAsync(reviewId);
        Assert.Equal(5, updatedReview!.Rating);
        Assert.Equal("New Comment", updatedReview.Comment);
    }

    [Fact]
    public async Task UpdateReviewAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Act & Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            reviewService.UpdateReviewAsync(null!, "customer-1"));
    }

    [Fact]
    public async Task UpdateReviewAsync_WhenReviewNotFound_ShouldThrowEntityNotFoundException()
    {
        // Arrange:
        var dto = new ReviewUpdateDto
        {
            Id = 999,
            Rating = 5
        };

        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            reviewService.UpdateReviewAsync(dto, "customer-1"));
    }

    [Fact]
    public async Task UpdateReviewAsync_WhenUserNotAuthor_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const string customerId = "customer-1";
        const string otherUserId = "other-1";
        const int reviewId = 1;
        
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
            Id = 1,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = "provider-1",
            Provider = provider
        };
        
        var review = new Review 
        { 
            Id = reviewId, 
            ServiceId = 1, 
            Service = service,
            CustomerId = customerId, 
            Customer = customer,
            BookingId = "b1",
            Rating = 4, 
            Comment = "Old Comment" 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Reviews.AddAsync(review);
        await dbContext.SaveChangesAsync();

        var dto = new ReviewUpdateDto
        {
            Id = reviewId,
            Rating = 5
        };

        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() => 
            reviewService.UpdateReviewAsync(dto, otherUserId));
    }
}