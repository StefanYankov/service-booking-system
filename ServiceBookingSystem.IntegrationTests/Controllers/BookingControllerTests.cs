using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class BookingControllerTests : BaseIntegrationTest
{
    public BookingControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturn201Created()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        const string providerId = "provider-1";
        var bookingStart = DateTime.UtcNow.AddDays(1);

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        await userManager.CreateAsync(customer, password);

        var provider = new ApplicationUser
        {
            Id = providerId,
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, "Password123!");

        var category = new Category
        {
            Name = "Test Category",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync(); 

        var service = new Service 
        { 
            Name = "Test Service", 
            Description = "Desc", 
            ProviderId = providerId, 
            DurationInMinutes = 60, 
            Price = 100, 
            IsActive = true, 
            CategoryId = category.Id 
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var operatingHour = new OperatingHour
        {
            ServiceId = service.Id,
            DayOfWeek = bookingStart.DayOfWeek,
            Segments = new List<OperatingSegment>
            {
                new() { StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(23, 59) }
            }
        };
        await this.DbContext.OperatingHours.AddAsync(operatingHour);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookingDto = new BookingCreateDto
        {
            ServiceId = service.Id,
            BookingStart = bookingStart,
            Notes = "Integration Test Booking"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var createdBooking = await response.Content.ReadFromJsonAsync<BookingViewDto>();
        Assert.NotNull(createdBooking);
        Assert.Equal(service.Id, createdBooking.ServiceId);
        Assert.Equal("Pending", createdBooking.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_WithInvalidServiceId_ShouldReturn404NotFound(int invalidServiceId)
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        await SeedUserAsync(customerEmail, password);
        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookingDto = new BookingCreateDto
        {
            ServiceId = invalidServiceId,
            BookingStart = DateTime.UtcNow.AddDays(1)
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-1440)]
    public async Task Create_WithPastDate_ShouldReturn409Conflict(int minutesToAdd)
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, "Password123!");

        var category = new Category
        {
            Name = "Cat 1",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Service 1",
            Description = "Desc",
            ProviderId = "provider-1",
            DurationInMinutes = 60,
            Price = 100,
            IsActive = true,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        await SeedUserAsync(customerEmail, password);
        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookingDto = new BookingCreateDto
        {
            ServiceId = service.Id,
            BookingStart = DateTime.UtcNow.AddMinutes(minutesToAdd)
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnavailableSlot_ShouldReturn409Conflict()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        var bookingStart = DateTime.UtcNow.AddDays(2);

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        await userManager.CreateAsync(customer, password);

        var provider = new ApplicationUser
        {
            Id = "provider-1",
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, "Password123!");
        
        var category = new Category
        {
            Name = "Cat 1",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Closed Service",
            Description = "Desc",
            ProviderId = "provider-1",
            DurationInMinutes = 60,
            Price = 100, 
            IsActive = true,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookingDto = new BookingCreateDto
        {
            ServiceId = service.Id,
            BookingStart = bookingStart
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Equal("Slot Unavailable", problemDetails!.Title);
    }

    [Fact]
    public async Task Cancel_WithValidId_ShouldReturn200OK()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        var bookingStart = DateTime.UtcNow.AddDays(3);

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        await userManager.CreateAsync(customer, password);

        var provider = new ApplicationUser
        {
            Id = "provider-1",
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, "Password123!");
        
        var category = new Category
        {
            Name = "Cat 1",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Service 1",
            Description = "Desc",
            ProviderId = "provider-1",
            DurationInMinutes = 60,
            Price = 100,
            IsActive = true,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();
        
        var operatingHour = new OperatingHour
        {
            ServiceId = service.Id,
            DayOfWeek = bookingStart.DayOfWeek,
            Segments = new List<OperatingSegment>
            {
                new() { StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(23, 59) }
            }
        };
        await this.DbContext.OperatingHours.AddAsync(operatingHour);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await this.Client.PostAsJsonAsync("/api/booking", new BookingCreateDto { ServiceId = service.Id, BookingStart = bookingStart });
        createResponse.EnsureSuccessStatusCode();
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingViewDto>();

        // Act:
        var cancelResponse = await this.Client.PutAsync($"/api/booking/{createdBooking!.Id}/cancel", null);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelledBooking = await cancelResponse.Content.ReadFromJsonAsync<BookingViewDto>();
        Assert.Equal("Cancelled", cancelledBooking!.Status);
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    [InlineData("Declined")]
    public async Task Cancel_WithInvalidState_ShouldReturn409Conflict(string initialState)
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        var bookingStart = DateTime.UtcNow.AddDays(4);

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        await userManager.CreateAsync(customer, password);
        
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, "Password123!");

        var category = new Category
        {
            Name = "Cat 1",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Service 1",
            Description = "Desc",
            ProviderId = provider.Id,
            DurationInMinutes = 60,
            Price = 100,
            IsActive = true,
            CategoryId = category.Id
        };
        
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var statusEnum = Enum.Parse<BookingStatus>(initialState);
        var booking = new Booking 
        { 
            ServiceId = service.Id, 
            CustomerId = customer.Id, 
            BookingStart = bookingStart, 
            Status = statusEnum 
        };
        await this.DbContext.Bookings.AddAsync(booking);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.PutAsync($"/api/booking/{booking.Id}/cancel", null);

        // Assert:
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Equal("Invalid Action", problemDetails!.Title);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldReturn404()
    {
        // Arrange:
        const string customerEmail = "customer404@test.com";
        const string password = "Password123!";
        await SeedUserAsync(customerEmail, password);
        
        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.GetAsync("/api/booking/non-existent-id");

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Equal("Not Found", problemDetails!.Title);
    }

    [Fact]
    public async Task GetById_WithOtherUsersBooking_ShouldReturn404NotFound()
    {
        // Arrange:
        const string ownerEmail = "owner@test.com";
        const string hackerEmail = "hacker@test.com";
        const string password = "Password123!";

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var owner = new ApplicationUser
        {
            UserName = ownerEmail,
            Email = ownerEmail,
            FirstName = "Owner",
            LastName = "User"
        };
        
        await userManager.CreateAsync(owner, password);
        
        var hacker = new ApplicationUser
        {
            UserName = hackerEmail,
            Email = hackerEmail,
            FirstName = "Hacker",
            LastName = "User"
        };
        await userManager.CreateAsync(hacker, password);
        
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, "Password123!");

        var category = new Category
        {
            Name = "Cat 1",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Service 1",
            Description = "Desc",
            ProviderId = provider.Id,
            DurationInMinutes = 60,
            Price = 100,
            IsActive = true,
            CategoryId = category.Id
        };
        
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var booking = new Booking
        {
            ServiceId = service.Id,
            CustomerId = owner.Id,
            BookingStart = DateTime.UtcNow.AddDays(1)
        };
        await this.DbContext.Bookings.AddAsync(booking);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(hackerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.GetAsync($"/api/booking/{booking.Id}");

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_AsProvider_ShouldReturn200OK()
    {
        // Arrange:
        const string providerEmail = "provider@test.com";
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var provider = new ApplicationUser
        {
            UserName = providerEmail,
            Email = providerEmail,
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, password);
        
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        await userManager.CreateAsync(customer, password);

        var category = new Category
        {
            Name = "Cat 1",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Service 1",
            Description = "Desc", 
            ProviderId = provider.Id,
            DurationInMinutes = 60, 
            Price = 100, 
            IsActive = true, 
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var booking = new Booking
        {
            ServiceId = service.Id,
            CustomerId = customer.Id,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending
        };
        await this.DbContext.Bookings.AddAsync(booking);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(providerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await this.Client.PutAsync($"/api/booking/{booking.Id}/confirm", null);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var confirmedBooking = await response.Content.ReadFromJsonAsync<BookingViewDto>();
        Assert.Equal("Confirmed", confirmedBooking!.Status);
    }

    [Fact]
    public async Task Create_WithoutToken_ShouldReturn401Unauthorized()
    {
        // Arrange:
        this.Client.DefaultRequestHeaders.Authorization = null;
        var bookingDto = new BookingCreateDto
        {
            ServiceId = 1,
            BookingStart = DateTime.UtcNow.AddDays(1)
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidData_ShouldUpdateBooking()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        var bookingStart = DateTime.UtcNow.AddDays(1);
        var newStart = bookingStart.AddDays(1);

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        await userManager.CreateAsync(customer, password);

        var provider = new ApplicationUser
        {
            Id = "provider-1",
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, "Password123!");

        var category = new Category
        {
            Name = "Cat 1",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Service 1",
            Description = "Desc",
            ProviderId = "provider-1",
            DurationInMinutes = 60,
            Price = 100,
            IsActive = true,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var operatingHour = new OperatingHour { ServiceId = service.Id, DayOfWeek = newStart.DayOfWeek, Segments = new List<OperatingSegment> { new() { StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(23, 59) } } };
        await this.DbContext.OperatingHours.AddAsync(operatingHour);
        await this.DbContext.SaveChangesAsync();

        var booking = new Booking { ServiceId = service.Id, CustomerId = customer.Id, BookingStart = bookingStart, Status = BookingStatus.Pending };
        await this.DbContext.Bookings.AddAsync(booking);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new BookingUpdateDto
        {
            Id = booking.Id,
            BookingStart = newStart,
            Notes = "Rescheduled"
        };

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/booking/{booking.Id}", updateDto);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedBooking = await response.Content.ReadFromJsonAsync<BookingViewDto>();
        Assert.Equal(newStart, updatedBooking!.BookingStart);
        Assert.Equal("Rescheduled", updatedBooking.Notes);
    }

    [Fact]
    public async Task Update_WithConflict_ShouldReturnConflict()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        var bookingStart = DateTime.UtcNow.AddDays(1);
        var newStart = bookingStart.AddDays(1);

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        await userManager.CreateAsync(customer, password);

        var provider = new ApplicationUser
        {
            Id = "provider-1",
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, "Password123!");

        var category = new Category { Name = "Cat 1", Description = "Desc" };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Service 1",
            Description = "Desc",
            ProviderId = "provider-1",
            DurationInMinutes = 60,
            Price = 100,
            IsActive = true,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var operatingHour = new OperatingHour { ServiceId = service.Id, DayOfWeek = newStart.DayOfWeek, Segments = new List<OperatingSegment> { new() { StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(23, 59) } } };
        await this.DbContext.OperatingHours.AddAsync(operatingHour);
        await this.DbContext.SaveChangesAsync();

        var otherUser = new ApplicationUser
        {
            Id = "other",
            UserName = "other@test.com",
            Email = "other@test.com",
            FirstName = "Other",
            LastName = "User"
        };
        
        await userManager.CreateAsync(otherUser, "Password123!");

        var existingBooking = new Booking
        {
            ServiceId = service.Id,
            CustomerId = "other",
            BookingStart = newStart,
            Status = BookingStatus.Confirmed
        };
        await this.DbContext.Bookings.AddAsync(existingBooking);

        var booking = new Booking
        {
            ServiceId = service.Id,
            CustomerId = customer.Id,
            BookingStart = bookingStart,
            Status = BookingStatus.Pending
        };
        
        await this.DbContext.Bookings.AddAsync(booking);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new BookingUpdateDto
        {
            Id = booking.Id,
            BookingStart = newStart,
            Notes = "Rescheduled"
        };

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/booking/{booking.Id}", updateDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_OtherUsersBooking_ShouldReturn403Forbidden()
    {
        // Arrange:
        const string ownerEmail = "owner@test.com";
        const string hackerEmail = "hacker@test.com";
        const string password = "Password123!";

        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var owner = new ApplicationUser
        {
            UserName = ownerEmail,
            Email = ownerEmail,
            FirstName = "Owner",
            LastName = "User"
        };
        await userManager.CreateAsync(owner, password);
        
        var hacker = new ApplicationUser
        {
            UserName = hackerEmail,
            Email = hackerEmail,
            FirstName = "Hacker",
            LastName = "User"
        };
        await userManager.CreateAsync(hacker, password);
        
        var provider = new ApplicationUser
        {
            Id = "provider-1",
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, "Password123!");

        var category = new Category
        {
            Name = "Cat 1",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Service 1",
            Description = "Desc",
            ProviderId = "provider-1",
            DurationInMinutes = 60,
            Price = 100,
            IsActive = true,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var booking = new Booking
        {
            ServiceId = service.Id,
            CustomerId = owner.Id,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending
        };
        await this.DbContext.Bookings.AddAsync(booking);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(hackerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new BookingUpdateDto
        {
            Id = booking.Id,
            BookingStart = DateTime.UtcNow.AddDays(2),
            Notes = "Hacked"
        };

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/booking/{booking.Id}", updateDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Helpers ---

    private async Task SeedUserAsync(string email, string password)
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User"
        };
        await userManager.CreateAsync(user, password);
    }

    private async Task<string> GetAuthTokenAsync(string email, string password)
    {
        var loginResponse = await this.Client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return loginResult!.Token;
    }

    private class LoginResult
    {
        public string Token { get; init; } = null!;
    }
}