using System.Net;
using System.Net.Http.Headers;
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

public class MvcBookingRescheduleTests : BaseIntegrationTest
{
    public MvcBookingRescheduleTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Get_Reschedule_WithValidBooking_ReturnsView()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Pending);

        var client = CreateAuthenticatedClient(customer.Id, RoleConstants.Customer);

        // Act
        var response = await client.GetAsync($"/Booking/Reschedule/{booking.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Reschedule Booking", content);
    }

    [Fact]
    public async Task Get_Reschedule_WithCancelledBooking_RedirectsToIndex()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Cancelled);

        var client = CreateAuthenticatedClient(customer.Id, RoleConstants.Customer);

        // Act
        var response = await client.GetAsync($"/Booking/Reschedule/{booking.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Booking", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_Reschedule_WithValidData_RedirectsToIndex()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        await SeedOperatingHoursAsync(service.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Pending);

        var client = CreateAuthenticatedClient(customer.Id, RoleConstants.Customer);
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        var newDate = DateTime.UtcNow.AddDays(7); 
        var newTime = new TimeSpan(10, 0, 0);

        var formData = new Dictionary<string, string>
        {
            { "BookingId", booking.Id },
            { "ServiceId", service.Id.ToString() },
            { "Date", newDate.ToString("yyyy-MM-dd") },
            { "Time", "10:00" }
        };

        // Act
        var response = await client.PostAsync("/Booking/Reschedule", new FormUrlEncodedContent(formData));

        // Assert
        if (response.StatusCode != HttpStatusCode.Redirect && response.StatusCode != HttpStatusCode.Found)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"Response Content: {errorContent}"); 
        }
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        DbContext.ChangeTracker.Clear();
        var dbBooking = await DbContext.Bookings.FindAsync(booking.Id);
        Assert.Equal(newDate.Date.Add(newTime), dbBooking!.BookingStart);
    }

    [Fact]
    public async Task Post_Reschedule_WithUnavailableSlot_ReturnsViewWithErrors()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        await SeedOperatingHoursAsync(service.Id);
        var booking = await SeedBookingAsync(customer.Id, service.Id, BookingStatus.Pending);
        
        var conflictDate = DateTime.UtcNow.AddDays(7).Date.Add(new TimeSpan(10, 0, 0));
        var otherCustomer = await SeedCustomerAsync();
        await SeedBookingAsync(otherCustomer.Id, service.Id, BookingStatus.Confirmed, conflictDate);

        var client = CreateAuthenticatedClient(customer.Id, RoleConstants.Customer);
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        var formData = new Dictionary<string, string>
        {
            { "BookingId", booking.Id },
            { "ServiceId", service.Id.ToString() },
            { "Date", conflictDate.Date.ToString("yyyy-MM-dd") },
            { "Time", "10:00" }
        };

        // Act
        var response = await client.PostAsync("/Booking/Reschedule", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("is not available", content);
    }

    [Fact]
    public async Task Post_Reschedule_OtherUsersBooking_ReturnsForbidden()
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
            { "BookingId", booking.Id },
            { "ServiceId", service.Id.ToString() },
            { "Date", DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd") },
            { "Time", "10:00" }
        };

        // Act
        var response = await client.PostAsync("/Booking/Reschedule", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("is not authorized to perform action", content);
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

    private async Task SeedOperatingHoursAsync(int serviceId)
    {
        // Open every day 9-17
        var days = Enum.GetValues<DayOfWeek>();
        foreach (var day in days)
        {
            var hour = new OperatingHour
            {
                ServiceId = serviceId,
                DayOfWeek = day,
                Segments = new List<OperatingSegment>
                {
                    new() { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) }
                }
            };
            await DbContext.OperatingHours.AddAsync(hour);
        }
        await DbContext.SaveChangesAsync();
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