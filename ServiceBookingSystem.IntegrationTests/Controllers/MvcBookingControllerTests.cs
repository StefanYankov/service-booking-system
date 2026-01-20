using System.Net;
using Microsoft.AspNetCore.Antiforgery;
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

public class MvcBookingControllerTests : BaseIntegrationTest
{
    public MvcBookingControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Get_Create_ReturnsViewWithDefaultDate()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        
        var client = CreateAuthenticatedClient(customer.Id, RoleConstants.Customer);

        // Act
        var response = await client.GetAsync($"/Booking/Create?serviceId={service.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        // Check if the date input has today's date (yyyy-MM-dd)
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        Assert.Contains($"value=\"{today}\"", content);
    }

    [Fact]
    public async Task Get_Index_ContainsCorrectCancelUrl()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Pending);

        var client = CreateAuthenticatedClient(customer.Id, RoleConstants.Customer);

        // Act
        var response = await client.GetAsync("/Booking/Index");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        // Debug output
        if (!content.Contains($"/Booking/Cancel/{booking.Id}"))
        {
            Output.WriteLine($"HTML Content: {content}");
        }

        // Check for the Cancel form action
        Assert.Contains($"/Booking/Cancel/{booking.Id}", content);
        Assert.DoesNotContain("/api/Booking", content);
    }

    [Fact]
    public async Task Post_Cancel_WithValidData_RedirectsToIndex()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Pending);

        var client = CreateAuthenticatedClient(customer.Id, RoleConstants.Customer);

        var formData = new Dictionary<string, string>
        {
            { "id", booking.Id }
        };

        // Act
        var response = await client.PostAsync("/Booking/Cancel", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.True(location.EndsWith("/Booking") || location.EndsWith("/Booking/Index"));
        DbContext.ChangeTracker.Clear();
        var dbBooking = await DbContext.Bookings.FindAsync(booking.Id);
        Assert.Equal(BookingStatus.Cancelled, dbBooking!.Status);
    }

    [Fact]
    public async Task Post_Cancel_Unauthorized_ReturnsForbidden()
    {
        // Arrange
        var attacker = await SeedCustomerAsync();
        var victim = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(victim.Id, service.Id, BookingStatus.Pending);

        var client = CreateAuthenticatedClient(attacker.Id, RoleConstants.Customer);

        var formData = new Dictionary<string, string>
        {
            { "id", booking.Id }
        };

        // Act
        var response = await client.PostAsync("/Booking/Cancel", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.True(location.EndsWith("/Booking") || location.EndsWith("/Booking/Index"));
        DbContext.ChangeTracker.Clear();
        var dbBooking = await DbContext.Bookings.FindAsync(booking.Id);
        Assert.Equal(BookingStatus.Pending, dbBooking!.Status);
    }

    [Fact]
    public async Task Post_Confirm_AsProvider_RedirectsToReceived()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Pending);

        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        var formData = new Dictionary<string, string>
        {
            { "id", booking.Id }
        };

        // Act
        var response = await client.PostAsync("/Booking/Confirm", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Booking/Received", response.Headers.Location!.ToString());
        DbContext.ChangeTracker.Clear();
        var dbBooking = await DbContext.Bookings.FindAsync(booking.Id);
        Assert.Equal(BookingStatus.Confirmed, dbBooking!.Status);
    }

    [Fact]
    public async Task Post_Decline_AsProvider_RedirectsToReceived()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Pending);

        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        var formData = new Dictionary<string, string>
        {
            { "id", booking.Id }
        };

        // Act
        var response = await client.PostAsync("/Booking/Decline", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Booking/Received", response.Headers.Location!.ToString());
        DbContext.ChangeTracker.Clear();
        var dbBooking = await DbContext.Bookings.FindAsync(booking.Id);
        Assert.Equal(BookingStatus.Declined, dbBooking!.Status);
    }

    [Fact]
    public async Task Post_Complete_AsProvider_RedirectsToReceived()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Confirmed, DateTime.UtcNow.AddDays(-1));

        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        var formData = new Dictionary<string, string>
        {
            { "id", booking.Id }
        };

        // Act
        var response = await client.PostAsync("/Booking/Complete", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Booking/Received", response.Headers.Location!.ToString());
        DbContext.ChangeTracker.Clear();
        var dbBooking = await DbContext.Bookings.FindAsync(booking.Id);
        Assert.Equal(BookingStatus.Completed, dbBooking!.Status);
    }

    // --- Helpers ---

    private HttpClient CreateAuthenticatedClient(string userId, string role)
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

                services.AddSingleton<IAntiforgery, MockAntiforgery>();
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    private async Task<ApplicationUser> SeedCustomerAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = $"c_{Guid.NewGuid()}@test.com", Email = $"c_{Guid.NewGuid()}@test.com", FirstName = "C", LastName = "T" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Customer);
        return user;
    }

    private async Task<ApplicationUser> SeedProviderAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = $"p_{Guid.NewGuid()}@test.com", Email = $"p_{Guid.NewGuid()}@test.com", FirstName = "P", LastName = "T" };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, RoleConstants.Provider);
        return user;
    }

    private async Task<Service> SeedServiceAsync(string providerId)
    {
        var category = new Category { Name = $"Cat_{Guid.NewGuid()}", Description = "D" };
        await DbContext.Categories.AddAsync(category);
        await DbContext.SaveChangesAsync();

        var service = new Service { Name = "S", Description = "D", ProviderId = providerId, CategoryId = category.Id, Price = 10, DurationInMinutes = 60 };
        await DbContext.Services.AddAsync(service);
        await DbContext.SaveChangesAsync();
        return service;
    }

    private async Task<Booking> SeedBookingAsync(string customerId, int serviceId, BookingStatus status, DateTime? start = null)
    {
        var booking = new Booking
        {
            CustomerId = customerId,
            ServiceId = serviceId,
            BookingStart = start ?? DateTime.UtcNow.AddDays(1),
            Status = status
        };
        await DbContext.Bookings.AddAsync(booking);
        await DbContext.SaveChangesAsync();
        return booking;
    }
}