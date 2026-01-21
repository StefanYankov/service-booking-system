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

public class MvcScheduleControllerTests : BaseIntegrationTest
{
    public MvcScheduleControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Get_Index_AsOwner_ReturnsView()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        // Act
        var response = await client.GetAsync($"/Schedule/Index?serviceId={service.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Weekly Schedule", content);
    }

    [Fact]
    public async Task Get_Index_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var owner = await SeedProviderAsync();
        var attacker = await SeedProviderAsync();
        var service = await SeedServiceAsync(owner.Id);
        var client = CreateAuthenticatedClient(attacker.Id, RoleConstants.Provider);

        // Act
        var response = await client.GetAsync($"/Schedule/Index?serviceId={service.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_UpdateWeekly_RedirectsToIndex()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        var getResponse = await client.GetAsync($"/Schedule/Index?serviceId={service.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "ServiceId", service.Id.ToString() },
            { "ServiceName", service.Name },
            { "__RequestVerificationToken", token },
            // Monday Open 9-17 (Index 0)
            { "Days[0].DayOfWeek", "Monday" },
            { "Days[0].IsClosed", "false" },
            { "Days[0].Segments[0].Start", "09:00" },
            { "Days[0].Segments[0].End", "17:00" },
            // Tuesday Closed (Index 1)
            { "Days[1].DayOfWeek", "Tuesday" },
            { "Days[1].IsClosed", "true" }
        };

        // Act
        var response = await client.PostAsync("/Schedule/UpdateWeekly", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.Contains("/Schedule", location);
        Assert.Contains($"serviceId={service.Id}", location);
        
        // Verify DB
        DbContext.ChangeTracker.Clear();
        var hours = DbContext.OperatingHours.Where(h => h.ServiceId == service.Id).ToList();
        Assert.Contains(hours, h => h.DayOfWeek == DayOfWeek.Monday);
    }

    [Fact]
    public async Task Get_Overrides_AsOwner_ReturnsView()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        // Act
        var response = await client.GetAsync($"/Schedule/Overrides?serviceId={service.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Holidays & Overrides", content);
    }

    [Fact]
    public async Task Post_AddOverride_RedirectsToOverrides()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        var getResponse = await client.GetAsync($"/Schedule/Overrides?serviceId={service.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "ServiceId", service.Id.ToString() },
            { "Date", DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd") },
            { "IsDayOff", "true" },
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Schedule/AddOverride", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Schedule/Overrides?serviceId={service.Id}", response.Headers.Location!.ToString());
        
        // Verify DB
        DbContext.ChangeTracker.Clear();
        var overrideEntity = DbContext.ScheduleOverrides.FirstOrDefault(o => o.ServiceId == service.Id);
        Assert.NotNull(overrideEntity);
        Assert.True(overrideEntity.IsDayOff);
    }

    [Fact]
    public async Task Post_DeleteOverride_RedirectsToOverrides()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var overrideEntity = new ScheduleOverride
        {
            ServiceId = service.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            IsDayOff = true
        };
        await DbContext.ScheduleOverrides.AddAsync(overrideEntity);
        await DbContext.SaveChangesAsync();

        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        var getResponse = await client.GetAsync($"/Schedule/Overrides?serviceId={service.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "id", overrideEntity.Id.ToString() },
            { "serviceId", service.Id.ToString() },
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Schedule/DeleteOverride", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Schedule/Overrides?serviceId={service.Id}", response.Headers.Location!.ToString());
        
        // Verify DB
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.ScheduleOverrides.FindAsync(overrideEntity.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Post_DeleteOverride_WithInvalidId_RedirectsWithErrorMessage()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var client = CreateAuthenticatedClient(provider.Id, RoleConstants.Provider);

        var getResponse = await client.GetAsync($"/Schedule/Overrides?serviceId={service.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "id", "999" }, // Invalid ID
            { "serviceId", service.Id.ToString() },
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Schedule/DeleteOverride", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Schedule/Overrides?serviceId={service.Id}", response.Headers.Location!.ToString());
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
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    private string ExtractAntiForgeryToken(string htmlBody)
    {
        var match = System.Text.RegularExpressions.Regex.Match(htmlBody, @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />");
        return match.Success ? match.Groups[1].Value : throw new InvalidOperationException("Anti-forgery token not found");
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
}
