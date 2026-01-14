using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application.BookingServiceTests;

public partial class BookingServiceTests
{
    [Fact]
    public async Task GetBookingByIdAsync_WithValidIdAndCustomer_ShouldReturnDto()
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
            BookingStart = DateTime.UtcNow
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.GetBookingByIdAsync(bookingId, customerId);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
        Assert.Equal(customerId, result.CustomerId);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithValidIdAndProvider_ShouldReturnDto()
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
            BookingStart = DateTime.UtcNow
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.GetBookingByIdAsync(bookingId, providerId);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act:
        var result = await bookingService.GetBookingByIdAsync("non-existent", "user-1");

        // Assert:
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithUnauthorizedUser_ShouldReturnNull()
    {
        // Arrange:
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
            CustomerId = "customer-1",
            Customer = customer,
            BookingStart = DateTime.UtcNow
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.GetBookingByIdAsync(bookingId, "random-user");

        // Assert:
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBookingsByCustomerAsync_ShouldReturnPagedResult()
    {
        // Arrange:
        const string customerId = "customer-1";
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

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = "b1",
                ServiceId = 1,
                Service = service,
                CustomerId = customerId,
                Customer = customer,
                BookingStart = DateTime.UtcNow
            },
            new Booking
            {
                Id = "b2",
                ServiceId = 1,
                Service = service,
                CustomerId = customerId,
                Customer = customer,
                BookingStart = DateTime.UtcNow.AddDays(1)
            },
            new Booking
            {
                Id = "b3",
                ServiceId = 1,
                Service = service,
                CustomerId = "other-customer",
                Customer = new ApplicationUser
                {
                    Id = "other",
                    FirstName = "O",
                    LastName = "C"
                },
                BookingStart = DateTime.UtcNow
            }
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddRangeAsync(bookings);
        await dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act:
        var result = await bookingService.GetBookingsByCustomerAsync(customerId, parameters);

        // Assert:
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, b => Assert.Equal(customerId, b.CustomerId));
    }

    [Fact]
    public async Task GetBookingsByCustomerAsync_WithSorting_ShouldReturnSortedResult()
    {
        // Arrange:
        const string customerId = "customer-1";
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

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = "b1",
                ServiceId = 1,
                Service = service,
                CustomerId = customerId,
                Customer = customer,
                BookingStart = DateTime.UtcNow.AddDays(2)
            }, // Latest
            new Booking
            {
                Id = "b2",
                ServiceId = 1,
                Service = service,
                CustomerId = customerId,
                Customer = customer,
                BookingStart = DateTime.UtcNow
            } // Earliest
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddRangeAsync(bookings);
        await dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters
        {
            SortBy = "date",
            SortDirection = "asc"
        };

        // Act:
        var result = await bookingService.GetBookingsByCustomerAsync(customerId, parameters);

        // Assert:
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("b2", result.Items.First().Id); // Earliest first
        Assert.Equal("b1", result.Items.Last().Id);
    }

    [Fact]
    public async Task GetBookingsByProviderAsync_ShouldReturnPagedResult()
    {
        // Arrange:
        const string providerId = "provider-1";
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
        var otherService = new Service
        {
            Id = 2,
            Name = "Other Service",
            Description = "Test Description",
            ProviderId = "other-provider",
            Provider = new ApplicationUser
            {
                Id = "other",
                FirstName = "O",
                LastName = "P"
            }
        };

        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = "b1",
                ServiceId = 1,
                Service = service,
                CustomerId = "customer-1",
                Customer = customer,
                BookingStart = DateTime.UtcNow
            },
            new Booking
            {
                Id = "b2",
                ServiceId = 2,
                Service = otherService,
                CustomerId = "customer-1",
                Customer = customer,
                BookingStart = DateTime.UtcNow
            }
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddRangeAsync(service, otherService);
        await dbContext.Bookings.AddRangeAsync(bookings);
        await dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act:
        var result = await bookingService.GetBookingsByProviderAsync(providerId, parameters);

        // Assert:
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("b1", result.Items.First().Id);
    }

    [Fact]
    public async Task HasCompletedBookingAsync_WhenBookingExistsAndCompleted_ShouldReturnTrue()
    {
        // Arrange:
        const string customerId = "customer-1";
        const int serviceId = 1;
        var booking = new Booking
        {
            Id = "b1",
            ServiceId = serviceId,
            CustomerId = customerId,
            BookingStart = DateTime.UtcNow,
            Status = BookingStatus.Completed
        };

        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.HasCompletedBookingAsync(customerId, serviceId);

        // Assert:
        Assert.True(result);
    }

    [Fact]
    public async Task HasCompletedBookingAsync_WhenBookingExistsButNotCompleted_ShouldReturnFalse()
    {
        // Arrange:
        const string customerId = "customer-1";
        const int serviceId = 1;
        var booking = new Booking
        {
            Id = "b1",
            ServiceId = serviceId,
            CustomerId = customerId,
            BookingStart = DateTime.UtcNow,
            Status = BookingStatus.Confirmed
        };

        await dbContext.Bookings.AddAsync(booking);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.HasCompletedBookingAsync(customerId, serviceId);

        // Assert:
        Assert.False(result);
    }

    [Fact]
    public async Task HasCompletedBookingAsync_WhenNoBookingExists_ShouldReturnFalse()
    {
        // Act:
        var result = await bookingService.HasCompletedBookingAsync("customer-1", 1);

        // Assert:
        Assert.False(result);
    }

    [Fact]
    public async Task GetBookingsByCustomerAsync_WithNoBookings_ShouldReturnEmptyList()
    {
        // Arrange
        const string customerId = "customer-1";
        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act:
        var result = await bookingService.GetBookingsByCustomerAsync(customerId, parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetBookingsByProviderAsync_WithNoBookings_ShouldReturnEmptyList()
    {
        // Arrange:
        const string providerId = "provider-1";
        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act:
        var result = await bookingService.GetBookingsByProviderAsync(providerId, parameters);

        // Assert:
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetBookingsByCustomerAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        const string customerId = "customer-1";
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

        var bookings = new List<Booking>();
        for (int i = 1; i <= 20; i++)
        {
            bookings.Add(new Booking
            {
                Id = $"b{i}",
                ServiceId = 1,
                Service = service,
                CustomerId = customerId,
                Customer = customer,
                BookingStart = DateTime.UtcNow.AddDays(i)
            });
        }

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddRangeAsync(bookings);
        await dbContext.SaveChangesAsync();

        var parameters = new PagingAndSortingParameters
        {
            PageNumber = 2,
            PageSize = 5,
            SortBy = "date",
            SortDirection = "asc"
        };

        // Act:
        var result = await bookingService.GetBookingsByCustomerAsync(customerId, parameters);

        // Assert:
        Assert.Equal(20, result.TotalCount);
        Assert.Equal(5, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);

        // Sort by date asc, page 2 (items 6-10) should start with b6
        Assert.Equal("b6", result.Items.First().Id);
        Assert.Equal("b10", result.Items.Last().Id);
    }

    [Fact]
    public async Task GetBookingsByProviderAndCustomerAsync_ShouldReturnOnlySharedBookings()
    {
        // Arrange:
        const string providerId = "provider-1";
        const string customerId = "customer-1";

        var provider = new ApplicationUser
        {
            Id = providerId,
            FirstName = "P",
            LastName = "1"
        };

        var customer = new ApplicationUser
        {
            Id = customerId,
            FirstName = "C",
            LastName = "1"
        };

        var otherProvider = new ApplicationUser
        {
            Id = "provider-2",
            FirstName = "P",
            LastName = "2"
        };

        var service1 = new Service
        {
            Id = 1,
            Name = "S1",
            ProviderId = providerId,
            Provider = provider,
            Description = "D"
        };
        var service2 = new Service
        {
            Id = 2,
            Name = "S2",
            ProviderId = "provider-2",
            Provider = otherProvider,
            Description = "D"
        };

        var b1 = new Booking
        {
            Id = "b1",
            ServiceId = 1,
            Service = service1,
            CustomerId = customerId,
            Customer = customer, BookingStart = DateTime.UtcNow
        };

        var b2 = new Booking
        {
            Id = "b2",
            ServiceId = 2,
            Service = service2,
            CustomerId = customerId,
            Customer = customer, BookingStart = DateTime.UtcNow
        }; // Same customer, different provider

        var b3 = new Booking
        {
            Id = "b3",
            ServiceId = 1,
            Service = service1,
            CustomerId = "other-cust" +
                         "omer",
            Customer = new ApplicationUser
            {
                Id = "other",
                FirstName = "O",
                LastName = "C"
            },
            BookingStart = DateTime.UtcNow
        }; // Same provider, different customer

        await dbContext.Users.AddRangeAsync(provider, customer, otherProvider);
        await dbContext.Services.AddRangeAsync(service1, service2);
        await dbContext.Bookings.AddRangeAsync(b1, b2, b3);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.GetBookingsByProviderAndCustomerAsync(providerId, customerId);

        // Assert:
        Assert.Single(result);
        Assert.Equal("b1", result.First().Id);
    }

    [Fact]
    public async Task GetBookingsByProviderAndCustomerAsync_WithNoBookings_ShouldReturnEmptyList()
    {
        // Arrange:
        const string providerId = "provider-1";
        const string customerId = "customer-1";

        var provider = new ApplicationUser
        {
            Id = providerId,
            FirstName = "P",
            LastName = "1"
        };

        var customer = new ApplicationUser
        {
            Id = customerId,
            FirstName = "C",
            LastName = "1"
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.GetBookingsByProviderAndCustomerAsync(providerId, customerId);

        // Assert:
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBookingsByProviderAndCustomerAsync_ShouldReturnSortedByDateDesc()
    {
        // Arrange:
        const string providerId = "provider-1";
        const string customerId = "customer-1";

        var provider = new ApplicationUser
        {
            Id = providerId,
            FirstName = "P",
            LastName = "1"
        };

        var customer = new ApplicationUser
        {
            Id = customerId,
            FirstName = "C",
            LastName = "1"
        };

        var service = new Service
        {
            Id = 1,
            Name = "S1",
            ProviderId = providerId,
            Provider = provider,
            Description = "D"
        };

        var b1 = new Booking
        {
            Id = "b1",
            ServiceId = 1,
            Service = service,
            CustomerId = customerId,
            Customer = customer,
            BookingStart = DateTime.UtcNow.AddDays(-1)
        };

        var b2 = new Booking
        {
            Id = "b2",
            ServiceId = 1,
            Service = service,
            CustomerId = customerId,
            Customer = customer,
            BookingStart = DateTime.UtcNow
        };

        await dbContext.Users.AddRangeAsync(provider, customer);
        await dbContext.Services.AddAsync(service);
        await dbContext.Bookings.AddRangeAsync(b1, b2);
        await dbContext.SaveChangesAsync();

        // Act:
        var result = await bookingService.GetBookingsByProviderAndCustomerAsync(providerId, customerId);

        // Assert:
        Assert.Equal(2, result.Count);
        Assert.Equal("b2", result[0].Id);
        Assert.Equal("b1", result[1].Id);
    }
}