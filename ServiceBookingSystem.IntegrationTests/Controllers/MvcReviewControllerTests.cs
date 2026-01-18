using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class MvcReviewControllerTests : BaseIntegrationTest
{
    public MvcReviewControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Get_Create_WithCompletedBooking_ReturnsView()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Completed);

        var client = CreateAuthenticatedClient(customer.Id);

        // Act
        var response = await client.GetAsync($"/Review/Create/{booking.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Write a Review", content);
    }

    [Fact]
    public async Task Get_Create_WithPendingBooking_RedirectsToIndex()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Pending);

        var client = CreateAuthenticatedClient(customer.Id);

        // Act
        var response = await client.GetAsync($"/Review/Create/{booking.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Booking", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Get_Create_WithInvalidBookingId_ReturnsNotFound()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var client = CreateAuthenticatedClient(customer.Id);

        // Act
        var response = await client.GetAsync("/Review/Create/invalid-id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Create_AsProvider_ReturnsForbidden()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);

        var customer = await SeedCustomerAsync();
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Completed);
        
        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        // Act
        var response = await client.GetAsync($"/Review/Create/{booking.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Create_WithValidData_RedirectsToService()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Completed);

        var client = CreateAuthenticatedClient(customer.Id);

        var getResponse = await client.GetAsync($"/Review/Create/{booking.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "BookingId", booking.Id },
            { "ServiceId", service.Id.ToString() },
            { "ServiceName", service.Name },
            { "Rating", "5" },
            { "Comment", "Excellent service! Highly recommended." },
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Review/Create", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Service/Details/{service.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_Create_WithInvalidData_ReturnsViewWithErrors()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Completed);

        var client = CreateAuthenticatedClient(customer.Id);

        var getResponse = await client.GetAsync($"/Review/Create/{booking.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "BookingId", booking.Id },
            { "ServiceId", service.Id.ToString() },
            { "Rating", "0" }, 
            { "Comment", "Short" },
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Review/Create", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Rating must be between 1 and 5 stars", content);
    }

    [Fact]
    public async Task Post_Create_DuplicateReview_ReturnsViewWithErrors()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Completed);

        var client = CreateAuthenticatedClient(customer.Id);

        var review = new Review { BookingId = booking.Id, ServiceId = service.Id, CustomerId = customer.Id, Rating = 5, Comment = "First review" };
        await DbContext.Reviews.AddAsync(review);
        await DbContext.SaveChangesAsync();

        var getResponse = await client.GetAsync($"/Review/Create/{booking.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "BookingId", booking.Id },
            { "ServiceId", service.Id.ToString() },
            { "Rating", "4" },
            { "Comment", "Second review attempt" },
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Review/Create", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("already exists", content);
    }

    // --- Helpers ---

    private HttpClient CreateAuthenticatedClient(string userId, string? role = null)
    {
        var client = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        if (role != null)
        {
            client.DefaultRequestHeaders.Add("X-Test-Role", role);
        }
        return client;
    }

    private string ExtractAntiForgeryToken(string htmlBody)
    {
        var match = System.Text.RegularExpressions.Regex.Match(htmlBody, @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />");
        return match.Success ? match.Groups[1].Value : throw new InvalidOperationException("Anti-forgery token not found");
    }

    private async Task<ApplicationUser> SeedCustomerAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser 
        { 
            UserName = $"c_{Guid.NewGuid()}@test.com", 
            Email = $"c_{Guid.NewGuid()}@test.com", 
            FirstName = "C", 
            LastName = "T",
            EmailConfirmed = true 
        };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Customer);
        return user;
    }

    private async Task<ApplicationUser> SeedProviderAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser 
        { 
            UserName = $"p_{Guid.NewGuid()}@test.com", 
            Email = $"p_{Guid.NewGuid()}@test.com", 
            FirstName = "P", 
            LastName = "T",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Provider);
        return user;
    }

    private async Task<Service> SeedServiceAsync(string providerId)
    {
        var category = new Category { Name = $"Cat_{Guid.NewGuid()}", Description = "D" };
        await DbContext.Categories.AddAsync(category);
        await DbContext.SaveChangesAsync();

        var service = new Service { Name = "S", Description = "D", ProviderId = providerId, CategoryId = category.Id };
        await DbContext.Services.AddAsync(service);
        await DbContext.SaveChangesAsync();
        return service;
    }

    private async Task<Booking> SeedBookingAsync(string customerId, int serviceId, BookingStatus status)
    {
        var booking = new Booking
        {
            CustomerId = customerId,
            ServiceId = serviceId,
            BookingStart = DateTime.UtcNow.AddDays(-1),
            Status = status
        };
        await DbContext.Bookings.AddAsync(booking);
        await DbContext.SaveChangesAsync();
        return booking;
    }
}