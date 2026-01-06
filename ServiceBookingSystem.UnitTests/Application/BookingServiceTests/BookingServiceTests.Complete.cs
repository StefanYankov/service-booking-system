using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.BookingServiceTests;

public partial class BookingServiceTests
{
    [Fact]
    public async Task CompleteBookingAsync_WithValidData_ShouldCompleteBooking()
    {
        // Arrange:
        const string providerId = "provider-1";
        const string bookingId = "booking-1";
        
        var provider = new ApplicationUser 
        {
            Id = providerId,
            FirstName = "Test",
            LastName = "Provider"
        };
        
        var customer = new ApplicationUser 
        {
            Id = "customer-1",
            FirstName = "Test",
            LastName = "Customer"
        };
        
        var service = new Service
        {
            Id = 1,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = providerId,
            Provider = provider
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = "customer-1", 
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(-1), // In the past
            Status = BookingStatus.Confirmed 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.CompleteBookingAsync(bookingId, providerId);

        // Assert:
        Assert.Equal("Completed", result.Status);
        
        var updatedBooking = await dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(BookingStatus.Completed, updatedBooking!.Status);
    }

    [Fact]
    public async Task CompleteBookingAsync_WhenBookingNotFound_ShouldThrowEntityNotFoundException()
    {
        // Act & Assert:
        await Assert.ThrowsAsync<EntityNotFoundException>(() => 
            bookingService.CompleteBookingAsync("non-existent", "provider-1"));
    }

    [Fact]
    public async Task CompleteBookingAsync_WhenUserNotProvider_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const string providerId = "provider-1";
        const string otherUserId = "other-1";
        const string bookingId = "booking-1";
        
        var provider = new ApplicationUser 
        {
            Id = providerId,
            FirstName = "Test",
            LastName = "Provider"
        };
        
        var customer = new ApplicationUser 
        {
            Id = "customer-1",
            FirstName = "Test",
            LastName = "Customer"
        };
        
        var service = new Service
        {
            Id = 1,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = providerId,
            Provider = provider
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = "customer-1", 
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(-1),
            Status = BookingStatus.Confirmed 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() => 
            bookingService.CompleteBookingAsync(bookingId, otherUserId));
    }

    [Fact]
    public async Task CompleteBookingAsync_WhenBookingNotConfirmed_ShouldThrowInvalidBookingStateException()
    {
        // Arrange
        const string providerId = "provider-1";
        const string bookingId = "booking-1";
        
        var provider = new ApplicationUser 
        {
            Id = providerId,
            FirstName = "Test",
            LastName = "Provider"
        };
        
        var customer = new ApplicationUser 
        {
            Id = "customer-1",
            FirstName = "Test",
            LastName = "Customer"
        };
        
        var service = new Service
        {
            Id = 1,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = providerId,
            Provider = provider
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = "customer-1", 
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(-1),
            Status = BookingStatus.Pending // Invalid state
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        var ex = await Assert.ThrowsAsync<InvalidBookingStateException>(() => 
            bookingService.CompleteBookingAsync(bookingId, providerId));
        Assert.Equal("Pending", ex.CurrentState);
        Assert.Equal("Complete", ex.Action);
    }

    [Fact]
    public async Task CompleteBookingAsync_WhenBookingInFuture_ShouldThrowBookingTimeException()
    {
        // Arrange:
        const string providerId = "provider-1";
        const string bookingId = "booking-1";
        
        var provider = new ApplicationUser 
        {
            Id = providerId,
            FirstName = "Test",
            LastName = "Provider"
        };
        
        var customer = new ApplicationUser 
        {
            Id = "customer-1",
            FirstName = "Test",
            LastName = "Customer"
        };
        
        var service = new Service
        {
            Id = 1,
            Name = "Test Service",
            Description = "Test Description",
            ProviderId = providerId,
            Provider = provider
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = "customer-1", 
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(1), // Future
            Status = BookingStatus.Confirmed 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        var ex = await Assert.ThrowsAsync<BookingTimeException>(() => 
            bookingService.CompleteBookingAsync(bookingId, providerId));
        Assert.Equal(bookingId, ex.BookingId);
    }
}