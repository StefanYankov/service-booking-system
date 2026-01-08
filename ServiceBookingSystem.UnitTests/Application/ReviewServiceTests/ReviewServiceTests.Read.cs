using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit;

namespace ServiceBookingSystem.UnitTests.Application.ReviewServiceTests;

public partial class ReviewServiceTests
{
    [Fact]
    public async Task GetReviewsByServiceAsync_ShouldReturnPagedResult()
    {
        // Arrange
        const int serviceId = 1;
        var provider = new ApplicationUser { Id = "provider-1", FirstName = "Test", LastName = "Provider" };
        var customer = new ApplicationUser { Id = "customer-1", FirstName = "Test", LastName = "Customer" };
        var service = new Service { Id = serviceId, Name = "Test Service", Description = "Test Description", ProviderId = "provider-1", Provider = provider };
        
        var reviews = new List<Review>
        {
            new Review { Id = 1, ServiceId = serviceId, Service = service, CustomerId = "customer-1", Customer = customer, BookingId = "b1", Rating = 5, CreatedOn = DateTime.UtcNow },
            new Review { Id = 2, ServiceId = serviceId, Service = service, CustomerId = "customer-1", Customer = customer, BookingId = "b2", Rating = 4, CreatedOn = DateTime.UtcNow.AddDays(-1) }
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Reviews.AddRangeAsync(reviews);
        await dbContext.SaveChangesAsync();

        var parameters = new PagingParameters { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await reviewService.GetReviewsByServiceAsync(serviceId, parameters);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(5, result.Items.First().Rating); // Should be ordered by CreatedOn desc (Review 1 is newer)
    }

    [Fact]
    public async Task GetReviewsByServiceAsync_WithNoReviews_ShouldReturnEmptyList()
    {
        // Arrange
        const int serviceId = 1;
        var parameters = new PagingParameters { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await reviewService.GetReviewsByServiceAsync(serviceId, parameters);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetReviewsByServiceAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        const int serviceId = 1;
        var provider = new ApplicationUser { Id = "provider-1", FirstName = "Test", LastName = "Provider" };
        var customer = new ApplicationUser { Id = "customer-1", FirstName = "Test", LastName = "Customer" };
        var service = new Service { Id = serviceId, Name = "Test Service", Description = "Test Description", ProviderId = "provider-1", Provider = provider };
        
        var reviews = new List<Review>();
        for (int i = 1; i <= 20; i++)
        {
            reviews.Add(new Review 
            { 
                Id = i, 
                ServiceId = serviceId, 
                Service = service, 
                CustomerId = "customer-1", 
                Customer = customer, 
                BookingId = $"b{i}", 
                Rating = 5, 
                CreatedOn = DateTime.UtcNow.AddDays(i) // Newer reviews have higher IDs
            });
        }

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Reviews.AddRangeAsync(reviews);
        await dbContext.SaveChangesAsync();

        // Request Page 2, Size 5. 
        // Total 20. Sorted Descending by Date.
        // Page 1: IDs 20, 19, 18, 17, 16
        // Page 2: IDs 15, 14, 13, 12, 11
        var parameters = new PagingParameters { PageNumber = 2, PageSize = 5 };

        // Act
        var result = await reviewService.GetReviewsByServiceAsync(serviceId, parameters);

        // Assert
        Assert.Equal(20, result.TotalCount);
        Assert.Equal(5, result.Items.Count());
        Assert.Equal(15, result.Items.First().Id);
        Assert.Equal(11, result.Items.Last().Id);
    }

    [Fact]
    public async Task GetReviewSummaryAsync_WithReviews_ShouldReturnCorrectAverage()
    {
        // Arrange
        const int serviceId = 1;
        
        // Seed required entities
        var provider = new ApplicationUser { Id = "provider-1", FirstName = "Test", LastName = "Provider" };
        var customer1 = new ApplicationUser { Id = "c1", FirstName = "C", LastName = "1" };
        var customer2 = new ApplicationUser { Id = "c2", FirstName = "C", LastName = "2" };
        var service = new Service { Id = serviceId, Name = "Test Service", Description = "Test Description", ProviderId = "provider-1", Provider = provider };
        
        // We need dummy bookings to satisfy the FK if enforced, or just for completeness
        var booking1 = new Booking { Id = "b1", ServiceId = serviceId, CustomerId = "c1", Status = BookingStatus.Completed };
        var booking2 = new Booking { Id = "b2", ServiceId = serviceId, CustomerId = "c2", Status = BookingStatus.Completed };

        var reviews = new List<Review>
        {
            new Review { Id = 1, ServiceId = serviceId, Service = service, CustomerId = "c1", Customer = customer1, BookingId = "b1", Booking = booking1, Rating = 5 },
            new Review { Id = 2, ServiceId = serviceId, Service = service, CustomerId = "c2", Customer = customer2, BookingId = "b2", Booking = booking2, Rating = 3 }
        };

        await dbContext.Users.AddRangeAsync(provider, customer1, customer2);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddRangeAsync(booking1, booking2);
        await dbContext.Reviews.AddRangeAsync(reviews);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await reviewService.GetReviewSummaryAsync(serviceId);

        // Assert
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(2, result.TotalReviews);
        Assert.Equal(4.0, result.AverageRating); // (5+3)/2 = 4
    }

    [Fact]
    public async Task GetReviewSummaryAsync_WithNoReviews_ShouldReturnZero()
    {
        // Arrange
        const int serviceId = 1;

        // Act
        var result = await reviewService.GetReviewSummaryAsync(serviceId);

        // Assert
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(0, result.TotalReviews);
        Assert.Equal(0, result.AverageRating);
    }

    [Fact]
    public async Task GetReviewSummaryAsync_ShouldExcludeDeletedReviews()
    {
        // Arrange
        const int serviceId = 1;
        
        var provider = new ApplicationUser { Id = "provider-1", FirstName = "Test", LastName = "Provider" };
        var customer = new ApplicationUser { Id = "customer-1", FirstName = "Test", LastName = "Customer" };
        var service = new Service { Id = serviceId, Name = "Test Service", Description = "Test Description", ProviderId = "provider-1", Provider = provider };
        var booking1 = new Booking { Id = "b1", ServiceId = serviceId, CustomerId = "customer-1", Status = BookingStatus.Completed };
        var booking2 = new Booking { Id = "b2", ServiceId = serviceId, CustomerId = "customer-1", Status = BookingStatus.Completed };

        var reviews = new List<Review>
        {
            new Review { Id = 1, ServiceId = serviceId, Service = service, CustomerId = "customer-1", Customer = customer, BookingId = "b1", Booking = booking1, Rating = 5 },
            new Review { Id = 2, ServiceId = serviceId, Service = service, CustomerId = "customer-1", Customer = customer, BookingId = "b2", Booking = booking2, Rating = 1, IsDeleted = true, DeletedOn = DateTime.UtcNow }
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddRangeAsync(booking1, booking2);
        await dbContext.Reviews.AddRangeAsync(reviews);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await reviewService.GetReviewSummaryAsync(serviceId);

        // Assert
        Assert.Equal(1, result.TotalReviews); // Only the active one
        Assert.Equal(5.0, result.AverageRating); // Average of just the 5-star review
    }
}