using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Core.Exceptions;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit;

namespace ServiceBookingSystem.UnitTests.Application.BookingServiceTests;

public partial class BookingServiceTests
{
    [Fact]
    public async Task ConfirmBookingAsync_WithValidData_ShouldConfirmBooking()
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
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.ConfirmBookingAsync(bookingId, providerId);

        // Assert:
        Assert.Equal("Confirmed", result.Status);
        
        var updatedBooking = await dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(BookingStatus.Confirmed, updatedBooking!.Status);
    }

    [Fact]
    public async Task ConfirmBookingAsync_WhenUserNotProvider_ShouldThrowAuthorizationException()
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
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() => 
            bookingService.ConfirmBookingAsync(bookingId, otherUserId));
    }

    [Fact]
    public async Task ConfirmBookingAsync_WhenBookingNotPending_ShouldThrowInvalidBookingStateException()
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
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Cancelled // Invalid state
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        var ex = await Assert.ThrowsAsync<InvalidBookingStateException>(() => 
            bookingService.ConfirmBookingAsync(bookingId, providerId));
        Assert.Equal("Cancelled", ex.CurrentState);
        Assert.Equal("Confirm", ex.Action);
    }

    [Fact]
    public async Task DeclineBookingAsync_WithValidData_ShouldDeclineBooking()
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
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await bookingService.DeclineBookingAsync(bookingId, providerId);

        // Assert:
        Assert.Equal("Declined", result.Status);
        
        var updatedBooking = await dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(BookingStatus.Declined, updatedBooking!.Status);
    }

    [Fact]
    public async Task DeclineBookingAsync_WhenUserNotProvider_ShouldThrowAuthorizationException()
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
        
        var service = new Service { 
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
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() => 
            bookingService.DeclineBookingAsync(bookingId, otherUserId));
    }

    [Fact]
    public async Task DeclineBookingAsync_WhenBookingNotPending_ShouldThrowInvalidBookingStateException()
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
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Confirmed // Invalid state
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        var ex = await Assert.ThrowsAsync<InvalidBookingStateException>(() => 
            bookingService.DeclineBookingAsync(bookingId, providerId));
        Assert.Equal("Confirmed", ex.CurrentState);
        Assert.Equal("Decline", ex.Action);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenCustomerCancels_ShouldCancelBooking()
    {
        // Arrange:
        const string customerId = "customer-1";
        const string bookingId = "booking-1";
        
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
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = customerId, 
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.CancelBookingAsync(bookingId, customerId);

        // Assert:
        Assert.Equal("Cancelled", result.Status);
        
        var updatedBooking = await dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(BookingStatus.Cancelled, updatedBooking!.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenProviderCancels_ShouldCancelBooking()
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
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Confirmed 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.CancelBookingAsync(bookingId, providerId);

        // Assert:
        Assert.Equal("Cancelled", result.Status);
        
        var updatedBooking = await dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(BookingStatus.Cancelled, updatedBooking!.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenUserNotAuthorized_ShouldThrowAuthorizationException()
    {
        // Arrange:
        const string otherUserId = "other-1";
        const string bookingId = "booking-1";
        
        var provider = new ApplicationUser 
        {
            Id = "provider-1",
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
            ProviderId = "provider-1",
            Provider = provider
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = "customer-1", 
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending 
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        await Assert.ThrowsAsync<AuthorizationException>(() => 
            bookingService.CancelBookingAsync(bookingId, otherUserId));
    }

    [Fact]
    public async Task CancelBookingAsync_WhenBookingCompleted_ShouldThrowInvalidBookingStateException()
    {
        // Arrange:
        const string customerId = "customer-1";
        const string bookingId = "booking-1";
        
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
            Id = 1, Name = "Test Service",
            Description = "Test Description",
            ProviderId = "provider-1",
            Provider = provider
        };
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            ServiceId = 1, 
            Service = service, 
            CustomerId = customerId, 
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(-1),
            Status = BookingStatus.Completed
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act & Assert:
        var ex = await Assert.ThrowsAsync<InvalidBookingStateException>(() => 
            bookingService.CancelBookingAsync(bookingId, customerId));
        Assert.Equal("Completed", ex.CurrentState);
        Assert.Equal("Cancel", ex.Action);
    }
}