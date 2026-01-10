using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Review;
using ServiceBookingSystem.Application.DTOs.Shared;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class ReviewControllerTests : BaseIntegrationTest
{
    public ReviewControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Create_WithValidCompletedBooking_ShouldReturn201Created()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        const string bookingId = "booking-1";

        var serviceId = await SeedBaseDataAsync();
        await CreateCompletedBookingAsync(bookingId, serviceId, customerEmail, password);

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var reviewDto = new ReviewCreateDto
        {
            BookingId = bookingId,
            Rating = 5,
            Comment = "Excellent service!"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/review", reviewDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var createdReview = await response.Content.ReadFromJsonAsync<ReviewViewDto>();
        Assert.NotNull(createdReview);
        Assert.Equal(5, createdReview.Rating);
        Assert.Equal("Excellent service!", createdReview.Comment);
    }

    [Fact]
    public async Task Create_WithoutBooking_ShouldReturn404NotFound()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";

        await SeedBaseDataAsync();
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser { UserName = customerEmail, Email = customerEmail, FirstName = "Test", LastName = "Customer" };
        await userManager.CreateAsync(customer, password);

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var reviewDto = new ReviewCreateDto
        {
            BookingId = "fake-booking-id",
            Rating = 5,
            Comment = "I never booked this!"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/review", reviewDto);

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReviewForPendingBooking_ShouldReturn409Conflict()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        const string bookingId = "booking-pending";

        var serviceId = await SeedBaseDataAsync();
        
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        await userManager.CreateAsync(customer, password);
        var booking = new Booking
        {
            Id = bookingId,
            ServiceId = serviceId,
            CustomerId = customer.Id,
            BookingStart = DateTime.UtcNow.AddDays(1),
            Status = BookingStatus.Pending
        };
        
        await this.DbContext.Bookings.AddAsync(booking);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var reviewDto = new ReviewCreateDto
        {
            BookingId = bookingId,
            Rating = 5,
            Comment = "Premature review"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/review", reviewDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateReview_ShouldReturn409Conflict()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        const string bookingId = "booking-1";

        var serviceId = await SeedBaseDataAsync();
        await CreateCompletedBookingAsync(bookingId, serviceId, customerEmail, password);

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var reviewDto = new ReviewCreateDto
        {
            BookingId = bookingId,
            Rating = 5,
            Comment = "First review"
        };
        
        // Create first review
        var firstResponse = await this.Client.PostAsJsonAsync("/api/review", reviewDto);
        firstResponse.EnsureSuccessStatusCode();

        // Act: Try to create second review for same booking
        var response = await this.Client.PostAsJsonAsync("/api/review", reviewDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRating_ShouldReturn400BadRequest()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        await SeedBaseDataAsync();
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        
        await userManager.CreateAsync(customer, password);

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var reviewDto = new ReviewCreateDto
        {
            BookingId = "any",
            Rating = 6,
            Comment = "Invalid"
        };

        // Act:
        var response = await this.Client.PostAsJsonAsync("/api/review", reviewDto);

        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_OwnReview_ShouldReturn200OK()
    {
        // Arrange:
        const string customerEmail = "customer@test.com";
        const string password = "Password123!";
        var serviceId = await SeedBaseDataAsync();
        var reviewId = await CreateReviewInDbAsync(serviceId, customerEmail, "booking-1", 5, "Original");

        var token = await GetAuthTokenAsync(customerEmail, password);
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new ReviewUpdateDto
        {
            Id = reviewId,
            Rating = 1,
            Comment = "Updated"
        };

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/review/{reviewId}", updateDto);

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedReview = await response.Content.ReadFromJsonAsync<ReviewViewDto>();
        Assert.Equal(1, updatedReview!.Rating);
        Assert.Equal("Updated", updatedReview.Comment);
    }

    [Fact]
    public async Task Update_OtherUsersReview_ShouldReturn403Forbidden()
    {
        // Arrange:
        var serviceId = await SeedBaseDataAsync();
        var reviewId = await CreateReviewInDbAsync(serviceId, "owner@test.com", "booking-1", 5, "Original");

        // Login as Hacker
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var hacker = new ApplicationUser { UserName = "hacker@test.com", Email = "hacker@test.com", FirstName = "Hacker", LastName = "User" };
        await userManager.CreateAsync(hacker, "Password123!");
        
        var token = await GetAuthTokenAsync("hacker@test.com", "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateDto = new ReviewUpdateDto { Id = reviewId, Rating = 1, Comment = "Hacked" };

        // Act:
        var response = await this.Client.PutAsJsonAsync($"/api/review/{reviewId}", updateDto);

        // Assert:
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReviewsByService_ShouldReturnPublicList()
    {
        // Arrange:
        var serviceId = await SeedBaseDataAsync();
        
        await CreateReviewInDbAsync(serviceId, "user1@test.com", "booking-1", 5, "Great");
        await CreateReviewInDbAsync(serviceId, "user2@test.com", "booking-2", 4, "Good");

        // Act:
        this.Client.DefaultRequestHeaders.Authorization = null;
        var response = await this.Client.GetAsync($"/api/services/{serviceId}/reviews");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<ReviewViewDto>>();
        Assert.NotNull(pagedResult);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Contains(pagedResult.Items, r => r.Comment == "Great");
    }

    [Fact]
    public async Task GetById_ShouldReturnReview_Anonymously()
    {
        // Arrange:
        var serviceId = await SeedBaseDataAsync();
        var reviewId = await CreateReviewInDbAsync(serviceId, "user1@test.com", "booking-1", 5, "Great");

        // Act:
        this.Client.DefaultRequestHeaders.Authorization = null;
        var response = await this.Client.GetAsync($"/api/review/{reviewId}");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var review = await response.Content.ReadFromJsonAsync<ReviewViewDto>();
        Assert.Equal(reviewId, review!.Id);
    }

    // --- Helpers ---

    private async Task<int> SeedBaseDataAsync()
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var provider = new ApplicationUser { Id = "provider-1", UserName = "provider@test.com", Email = "provider@test.com", FirstName = "Test", LastName = "Provider" };
        await userManager.CreateAsync(provider, "Password123!");

        var category = new Category { Name = "Test Category", Description = "Desc" };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service { Name = "Test Service", Description = "Desc", ProviderId = "provider-1", DurationInMinutes = 60, Price = 100, IsActive = true, CategoryId = category.Id };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        return service.Id;
    }

    private async Task CreateCompletedBookingAsync(string bookingId, int serviceId, string customerEmail, string password)
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            FirstName = "Test",
            LastName = "Customer"
        };
        
        await userManager.CreateAsync(customer, password);

        var booking = new Booking
        {
            Id = bookingId,
            ServiceId = serviceId,
            CustomerId = customer.Id,
            BookingStart = DateTime.UtcNow.AddDays(-2),
            Status = BookingStatus.Completed
        };
        await this.DbContext.Bookings.AddAsync(booking);
        await this.DbContext.SaveChangesAsync();
    }

    private async Task<int> CreateReviewInDbAsync(int serviceId, string email, string bookingId, int rating, string comment)
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Test", LastName = "User" };
        await userManager.CreateAsync(user, "Password123!");

        var booking = new Booking
        {
            Id = bookingId,
            ServiceId = serviceId,
            CustomerId = user.Id,
            BookingStart = DateTime.UtcNow.AddDays(-1),
            Status = BookingStatus.Completed
        };
        
        await this.DbContext.Bookings.AddAsync(booking);

        var review = new Review
        {
            ServiceId = serviceId,
            CustomerId = user.Id,
            BookingId = bookingId,
            Rating = rating,
            Comment = comment,
            CreatedOn = DateTime.UtcNow
        };
        await this.DbContext.Reviews.AddAsync(review);
        await this.DbContext.SaveChangesAsync();
        
        return review.Id;
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
        public string Token { get; set; } = null!;
    }
}