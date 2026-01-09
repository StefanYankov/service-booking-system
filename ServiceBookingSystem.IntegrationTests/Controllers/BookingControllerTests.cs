using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Booking;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class BookingControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public BookingControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        this.client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturn201Created()
    {
        // Arrange:
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var customerEmail = $"customer_{uniqueSuffix}@test.com";
        const string password = "Password123!";
        var bookingStart = DateTime.UtcNow.AddDays(1);
        int serviceId;

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var customer = new ApplicationUser { UserName = customerEmail, Email = customerEmail, FirstName = "Test", LastName = "Customer" };
            await userManager.CreateAsync(customer, password);

            var provider = new ApplicationUser { Id = $"prov-{uniqueSuffix}", UserName = $"prov_{uniqueSuffix}@test.com", Email = $"prov_{uniqueSuffix}@test.com", FirstName = "Test", LastName = "Provider" };
            await userManager.CreateAsync(provider, "Password123!");

            var category = new Category { Name = $"Cat {uniqueSuffix}", Description = "Desc" };
            await dbContext.Categories.AddAsync(category);
            await dbContext.SaveChangesAsync();

            var service = new Service 
            { 
                Name = $"Service {uniqueSuffix}", 
                Description = "Desc", 
                ProviderId = provider.Id, 
                DurationInMinutes = 60, 
                Price = 100, 
                IsActive = true, 
                CategoryId = category.Id 
            };
            await dbContext.Services.AddAsync(service);
            await dbContext.SaveChangesAsync();
            serviceId = service.Id;

            var operatingHour = new OperatingHour
            {
                ServiceId = serviceId,
                DayOfWeek = bookingStart.DayOfWeek,
                StartTime = new TimeOnly(0, 0),
                EndTime = new TimeOnly(23, 59)
            };
            await dbContext.OperatingHours.AddAsync(operatingHour);
            await dbContext.SaveChangesAsync();
        }

        var token = await GetAuthTokenAsync(customerEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookingDto = new BookingCreateDto
        {
            ServiceId = serviceId,
            BookingStart = bookingStart,
            Notes = "Integration Test Booking"
        };

        // Act:
        var response = await client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var createdBooking = await response.Content.ReadFromJsonAsync<BookingViewDto>();
        Assert.NotNull(createdBooking);
        Assert.Equal(serviceId, createdBooking.ServiceId);
        Assert.Equal("Pending", createdBooking.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_WithInvalidServiceId_ShouldReturn404NotFound(int invalidServiceId)
    {
        // Arrange:
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var customerEmail = $"customer_inv_{uniqueSuffix}@test.com";
        const string password = "Password123!";
        await SeedUserAsync(customerEmail, password);
        var token = await GetAuthTokenAsync(customerEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookingDto = new BookingCreateDto
        {
            ServiceId = invalidServiceId,
            BookingStart = DateTime.UtcNow.AddDays(1)
        };

        // Act:
        var response = await client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-1440)]
    public async Task Create_WithPastDate_ShouldReturn409Conflict(int minutesToAdd)
    {
        // Arrange:
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var customerEmail = $"customer_past_{uniqueSuffix}@test.com";
        const string password = "Password123!";
        int serviceId;
        
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            var category = new Category { Name = $"Cat {uniqueSuffix}", Description = "Desc" };
            await dbContext.Categories.AddAsync(category);
            await dbContext.SaveChangesAsync();

            var provider = new ApplicationUser { Id = $"prov-{uniqueSuffix}", UserName = $"prov_{uniqueSuffix}@test.com", Email = $"prov_{uniqueSuffix}@test.com", FirstName = "Test", LastName = "Provider" };
            await userManager.CreateAsync(provider, "Password123!");

            var service = new Service 
            { 
                Name = $"Service {uniqueSuffix}", 
                Description = "Desc", 
                ProviderId = provider.Id, 
                DurationInMinutes = 60, 
                Price = 100, 
                IsActive = true, 
                CategoryId = category.Id 
            };
            await dbContext.Services.AddAsync(service);
            await dbContext.SaveChangesAsync();
            serviceId = service.Id;
        }

        await SeedUserAsync(customerEmail, password);
        var token = await GetAuthTokenAsync(customerEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookingDto = new BookingCreateDto
        {
            ServiceId = serviceId,
            BookingStart = DateTime.UtcNow.AddMinutes(minutesToAdd)
        };

        // Act:
        var response = await client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnavailableSlot_ShouldReturn409Conflict()
    {
        // Arrange:
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var customerEmail = $"customer_conflict_{uniqueSuffix}@test.com";
        const string password = "Password123!";
        int serviceId;
        var bookingStart = DateTime.UtcNow.AddDays(2);

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var customer = new ApplicationUser { UserName = customerEmail, Email = customerEmail, FirstName = "Test", LastName = "Customer" };
            await userManager.CreateAsync(customer, password);

            var provider = new ApplicationUser { Id = $"prov-{uniqueSuffix}", UserName = $"prov_{uniqueSuffix}@test.com", Email = $"prov_{uniqueSuffix}@test.com", FirstName = "Test", LastName = "Provider" };
            await userManager.CreateAsync(provider, "Password123!");
            
            var category = new Category { Name = $"Cat {uniqueSuffix}", Description = "Desc" };
            await dbContext.Categories.AddAsync(category);
            await dbContext.SaveChangesAsync();

            var service = new Service { Name = $"Closed {uniqueSuffix}", Description = "Desc", ProviderId = provider.Id, DurationInMinutes = 60, Price = 100, IsActive = true, CategoryId = category.Id };
            await dbContext.Services.AddAsync(service);
            await dbContext.SaveChangesAsync();
            serviceId = service.Id;
            // NO Operating Hours added
        }

        var token = await GetAuthTokenAsync(customerEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookingDto = new BookingCreateDto
        {
            ServiceId = serviceId,
            BookingStart = bookingStart
        };

        // Act:
        var response = await client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Equal("Slot Unavailable", problemDetails!.Title);
    }

    [Fact]
    public async Task Cancel_WithValidId_ShouldReturn200OK()
    {
        // Arrange:
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var customerEmail = $"customer_cancel_{uniqueSuffix}@test.com";
        const string password = "Password123!";
        int serviceId;
        var bookingStart = DateTime.UtcNow.AddDays(3);

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var customer = new ApplicationUser { UserName = customerEmail, Email = customerEmail, FirstName = "Test", LastName = "Customer" };
            await userManager.CreateAsync(customer, password);

            var provider = new ApplicationUser { Id = $"prov-{uniqueSuffix}", UserName = $"prov_{uniqueSuffix}@test.com", Email = $"prov_{uniqueSuffix}@test.com", FirstName = "Test", LastName = "Provider" };
            await userManager.CreateAsync(provider, "Password123!");
            
            var category = new Category { Name = $"Cat {uniqueSuffix}", Description = "Desc" };
            await dbContext.Categories.AddAsync(category);
            await dbContext.SaveChangesAsync();

            var service = new Service { Name = $"Service {uniqueSuffix}", Description = "Desc", ProviderId = provider.Id, DurationInMinutes = 60, Price = 100, IsActive = true, CategoryId = category.Id };
            await dbContext.Services.AddAsync(service);
            await dbContext.SaveChangesAsync();
            serviceId = service.Id;
            
            var operatingHour = new OperatingHour { ServiceId = serviceId, DayOfWeek = bookingStart.DayOfWeek, StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(23, 59) };
            await dbContext.OperatingHours.AddAsync(operatingHour);
            await dbContext.SaveChangesAsync();
        }

        var token = await GetAuthTokenAsync(customerEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/booking", new BookingCreateDto { ServiceId = serviceId, BookingStart = bookingStart });
        createResponse.EnsureSuccessStatusCode();
        var createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingViewDto>();

        // Act:
        var cancelResponse = await client.PutAsync($"/api/booking/{createdBooking!.Id}/cancel", null);

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
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var customerEmail = $"customer_state_{initialState}_{uniqueSuffix}@test.com";
        const string password = "Password123!";
        var bookingStart = DateTime.UtcNow.AddDays(4);
        string bookingId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            var customer = new ApplicationUser { UserName = customerEmail, Email = customerEmail, FirstName = "Test", LastName = "Customer" };
            await userManager.CreateAsync(customer, password);
            
            var provider = new ApplicationUser { Id = $"prov-{uniqueSuffix}", UserName = $"prov_{uniqueSuffix}@test.com", Email = $"prov_{uniqueSuffix}@test.com", FirstName = "Test", LastName = "Provider" };
            await userManager.CreateAsync(provider, "Password123!");

            var category = new Category { Name = $"Cat {uniqueSuffix}", Description = "Desc" };
            await dbContext.Categories.AddAsync(category);
            await dbContext.SaveChangesAsync();

            var service = new Service { Name = $"Service {uniqueSuffix}", Description = "Desc", ProviderId = provider.Id, DurationInMinutes = 60, Price = 100, IsActive = true, CategoryId = category.Id };
            await dbContext.Services.AddAsync(service);
            await dbContext.SaveChangesAsync();

            var statusEnum = Enum.Parse<BookingStatus>(initialState);
            var booking = new Booking 
            { 
                ServiceId = service.Id, 
                CustomerId = customer.Id, 
                BookingStart = bookingStart, 
                Status = statusEnum 
            };
            await dbContext.Bookings.AddAsync(booking);
            await dbContext.SaveChangesAsync();
            bookingId = booking.Id;
        }

        var token = await GetAuthTokenAsync(customerEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await client.PutAsync($"/api/booking/{bookingId}/cancel", null);

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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await client.GetAsync("/api/booking/non-existent-id");

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Equal("Not Found", problemDetails!.Title);
    }

    [Fact]
    public async Task GetById_WithOtherUsersBooking_ShouldReturn404NotFound()
    {
        // Arrange:
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var ownerEmail = $"owner_{uniqueSuffix}@test.com";
        var hackerEmail = $"hacker_{uniqueSuffix}@test.com";
        const string password = "Password123!";
        string bookingId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            var owner = new ApplicationUser { UserName = ownerEmail, Email = ownerEmail, FirstName = "Owner", LastName = "User" };
            await userManager.CreateAsync(owner, password);
            
            var hacker = new ApplicationUser { UserName = hackerEmail, Email = hackerEmail, FirstName = "Hacker", LastName = "User" };
            await userManager.CreateAsync(hacker, password);
            
            var provider = new ApplicationUser { Id = $"prov-{uniqueSuffix}", UserName = $"prov_{uniqueSuffix}@test.com", Email = $"prov_{uniqueSuffix}@test.com", FirstName = "Test", LastName = "Provider" };
            await userManager.CreateAsync(provider, "Password123!");

            var category = new Category { Name = $"Cat {uniqueSuffix}", Description = "Desc" };
            await dbContext.Categories.AddAsync(category);
            await dbContext.SaveChangesAsync();

            var service = new Service { Name = $"Service {uniqueSuffix}", Description = "Desc", ProviderId = provider.Id, DurationInMinutes = 60, Price = 100, IsActive = true, CategoryId = category.Id };
            await dbContext.Services.AddAsync(service);
            await dbContext.SaveChangesAsync();

            var booking = new Booking { ServiceId = service.Id, CustomerId = owner.Id, BookingStart = DateTime.UtcNow.AddDays(1) };
            await dbContext.Bookings.AddAsync(booking);
            await dbContext.SaveChangesAsync();
            bookingId = booking.Id;
        }

        // Login as Hacker
        var token = await GetAuthTokenAsync(hackerEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await client.GetAsync($"/api/booking/{bookingId}");

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_AsProvider_ShouldReturn200OK()
    {
        // Arrange:
        var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
        var providerEmail = $"prov_{uniqueSuffix}@test.com";
        var customerEmail = $"cust_{uniqueSuffix}@test.com";
        const string password = "Password123!";
        string bookingId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            
            var provider = new ApplicationUser { UserName = providerEmail, Email = providerEmail, FirstName = "Test", LastName = "Provider" };
            await userManager.CreateAsync(provider, password);
            
            var customer = new ApplicationUser { UserName = customerEmail, Email = customerEmail, FirstName = "Test", LastName = "Customer" };
            await userManager.CreateAsync(customer, password);

            var category = new Category { Name = $"Cat {uniqueSuffix}", Description = "Desc" };
            await dbContext.Categories.AddAsync(category);
            await dbContext.SaveChangesAsync();

            var service = new Service { Name = $"Service {uniqueSuffix}", Description = "Desc", ProviderId = provider.Id, DurationInMinutes = 60, Price = 100, IsActive = true, CategoryId = category.Id };
            await dbContext.Services.AddAsync(service);
            await dbContext.SaveChangesAsync();

            var booking = new Booking { ServiceId = service.Id, CustomerId = customer.Id, BookingStart = DateTime.UtcNow.AddDays(1), Status = BookingStatus.Pending };
            await dbContext.Bookings.AddAsync(booking);
            await dbContext.SaveChangesAsync();
            bookingId = booking.Id;
        }

        // Login as Provider
        var token = await GetAuthTokenAsync(providerEmail, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await client.PutAsync($"/api/booking/{bookingId}/confirm", null);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var confirmedBooking = await response.Content.ReadFromJsonAsync<BookingViewDto>();
        Assert.Equal("Confirmed", confirmedBooking!.Status);
    }

    [Fact]
    public async Task Create_WithoutToken_ShouldReturn401Unauthorized()
    {
        // Arrange:
        client.DefaultRequestHeaders.Authorization = null;
        var bookingDto = new BookingCreateDto { ServiceId = 1, BookingStart = DateTime.UtcNow.AddDays(1) };

        // Act:
        var response = await client.PostAsJsonAsync("/api/booking", bookingDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Helpers ---

    private async Task SeedUserAsync(string email, string password)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Test", LastName = "User" };
            await userManager.CreateAsync(user, password);
        }
    }

    private async Task<string> GetAuthTokenAsync(string email, string password)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return loginResult!.Token;
    }

    private class LoginResult
    {
        public string Token { get; set; } = null!;
    }
}