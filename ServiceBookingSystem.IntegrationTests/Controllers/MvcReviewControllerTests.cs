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
    public async Task Post_Create_WithValidData_RedirectsToService()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Completed);

        var client = CreateAuthenticatedClient(customer.Id);

        // Get the form to extract token (optional if we mock it, but good practice)
        // Since we use TestAuthHandler, we bypass the Login form, but the Review form still has AntiForgery.
        // However, TestAuthHandler doesn't disable AntiForgery validation.
        // We need to extract the token from the GET request to the Review page.
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
            { "Rating", "0" }, // Invalid: Min 1
            { "Comment", "Short" }, // Invalid: Min 10 chars
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Review/Create", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // Redisplays form
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Rating must be between 1 and 5 stars", content); // Validation message
    }

    // --- Helpers ---

    private HttpClient CreateAuthenticatedClient(string userId)
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