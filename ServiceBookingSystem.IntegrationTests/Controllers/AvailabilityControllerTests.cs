using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.DTOs.Availability;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class AvailabilityControllerTests : BaseIntegrationTest
{
    public AvailabilityControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task GetAvailableSlots_WithNoBookings_ShouldReturnAllSlots()
    {
        // Arrange:
        var date = DateTime.UtcNow.AddDays(1).Date; // Tomorrow
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 12); // 9-12 (3 slots)

        // Act:
        var response = await this.Client.GetAsync($"/api/availability/slots?serviceId={serviceId}&date={date:yyyy-MM-dd}");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var slots = (await response.Content.ReadFromJsonAsync<IEnumerable<TimeOnly>>())?.ToList();
        Assert.NotNull(slots);
        Assert.Equal(3, slots.Count);
        Assert.Contains(new TimeOnly(9, 0), slots);
        Assert.Contains(new TimeOnly(10, 0), slots);
        Assert.Contains(new TimeOnly(11, 0), slots);
    }

    [Fact]
    public async Task GetAvailableSlots_WithConflict_ShouldReturnFilteredSlots()
    {
        // Arrange:
        var date = DateTime.UtcNow.AddDays(1).Date;
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 12);
        
        // Create a booking at 10:00
        await CreateBookingAsync(serviceId, date.AddHours(10));

        // Act:
        var response = await this.Client.GetAsync($"/api/availability/slots?serviceId={serviceId}&date={date:yyyy-MM-dd}");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var slots = (await response.Content.ReadFromJsonAsync<IEnumerable<TimeOnly>>())?.ToList();
        Assert.NotNull(slots);
        Assert.Equal(2, slots.Count);
        Assert.Contains(new TimeOnly(9, 0), slots);
        Assert.DoesNotContain(new TimeOnly(10, 0), slots); // Taken
        Assert.Contains(new TimeOnly(11, 0), slots);
    }

    [Fact]
    public async Task GetAvailableSlots_InvalidService_ShouldReturn404()
    {
        // Act:
        var response = await this.Client.GetAsync($"/api/availability/slots?serviceId=999&date={DateTime.UtcNow:yyyy-MM-dd}");

        // Assert:
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableSlots_PastDate_ShouldReturnEmpty()
    {
        // Arrange:
        var date = DateTime.UtcNow.AddDays(-1).Date; // Yesterday
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 12);

        // Act:
        var response = await this.Client.GetAsync($"/api/availability/slots?serviceId={serviceId}&date={date:yyyy-MM-dd}");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var slots = await response.Content.ReadFromJsonAsync<IEnumerable<TimeOnly>>();
        Assert.Empty(slots!);
    }

    [Fact]
    public async Task CheckSlot_WhenAvailable_ShouldReturnTrue()
    {
        // Arrange:
        var date = DateTime.UtcNow.AddDays(1).Date;
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 17);

        // Act:
        var response = await this.Client.GetAsync($"/api/availability/check?serviceId={serviceId}&bookingStart={date.AddHours(10):O}&durationMinutes=60");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var isAvailable = await response.Content.ReadFromJsonAsync<bool>();
        Assert.True(isAvailable);
    }

    [Fact]
    public async Task CheckSlot_WhenConflict_ShouldReturnFalse()
    {
        // Arrange:
        var date = DateTime.UtcNow.AddDays(1).Date;
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 17);
        await CreateBookingAsync(serviceId, date.AddHours(10));

        // Act:
        var response = await this.Client.GetAsync($"/api/availability/check?serviceId={serviceId}&bookingStart={date.AddHours(10):O}&durationMinutes=60");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var isAvailable = await response.Content.ReadFromJsonAsync<bool>();
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task CheckSlot_PastDate_ShouldReturnFalse()
    {
        // Arrange:
        var date = DateTime.UtcNow.AddDays(-1).Date;
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 17);

        // Act:
        var response = await this.Client.GetAsync($"/api/availability/check?serviceId={serviceId}&bookingStart={date.AddHours(10):O}&durationMinutes=60");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var isAvailable = await response.Content.ReadFromJsonAsync<bool>();
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task CheckSlot_NoOperatingHours_ShouldReturnFalse()
    {
        // Arrange:
        var date = DateTime.UtcNow.AddDays(1).Date;
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var provider = new ApplicationUser
        {
            UserName = "provider_nohours@test.com",
            Email = "provider_nohours@test.com",
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
            ProviderId = provider.Id,
            DurationInMinutes = 60,
            Price = 100,
            IsActive = true,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        // Act:
        var response = await this.Client.GetAsync($"/api/availability/check?serviceId={service.Id}&bookingStart={date.AddHours(10):O}&durationMinutes=60");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var isAvailable = await response.Content.ReadFromJsonAsync<bool>();
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task GetWeeklySchedule_AsOwner_ReturnsSchedule()
    {
        // Arrange
        var date = DateTime.UtcNow.AddDays(1).Date;
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 17);
        var providerEmail = "provider@test.com"; // From SeedServiceWithHoursAsync
        var token = await GetAuthTokenAsync(providerEmail, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await this.Client.GetAsync($"/api/availability/services/{serviceId}/weekly");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var schedule = await response.Content.ReadFromJsonAsync<WeeklyScheduleDto>();
        Assert.NotNull(schedule);
        Assert.Equal(7, schedule.Days.Count);
    }

    [Fact]
    public async Task UpdateWeeklySchedule_AsOwner_UpdatesSchedule()
    {
        // Arrange
        var date = DateTime.UtcNow.AddDays(1).Date;
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 17);
        var providerEmail = "provider@test.com";
        var token = await GetAuthTokenAsync(providerEmail, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new WeeklyScheduleDto();
        dto.Days.Add(new DayScheduleDto 
        { 
            DayOfWeek = DayOfWeek.Monday, 
            IsClosed = false,
            Segments = [new() { Start = new TimeOnly(10, 0), End = new TimeOnly(14, 0) }]
        });

        // Act
        var response = await this.Client.PutAsJsonAsync($"/api/availability/services/{serviceId}/weekly", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        DbContext.ChangeTracker.Clear();
        var hours = DbContext.OperatingHours.Where(h => h.ServiceId == serviceId).ToList();
        Assert.Contains(hours, h => h.DayOfWeek == DayOfWeek.Monday);
    }

    [Fact]
    public async Task AddOverride_AsOwner_AddsOverride()
    {
        // Arrange
        var date = DateTime.UtcNow.AddDays(1).Date;
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 17);
        var providerEmail = "provider@test.com";
        var token = await GetAuthTokenAsync(providerEmail, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new ScheduleOverrideDto
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            IsDayOff = true
        };

        // Act
        var response = await this.Client.PostAsJsonAsync($"/api/availability/services/{serviceId}/overrides", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        DbContext.ChangeTracker.Clear();
        var overrideEntity = DbContext.ScheduleOverrides.FirstOrDefault(o => o.ServiceId == serviceId);
        Assert.NotNull(overrideEntity);
        Assert.True(overrideEntity.IsDayOff);
    }

    [Fact]
    public async Task DeleteOverride_AsOwner_DeletesOverride()
    {
        // Arrange
        var date = DateTime.UtcNow.AddDays(1).Date;
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 17);
        var providerEmail = "provider@test.com";
        var token = await GetAuthTokenAsync(providerEmail, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var overrideEntity = new ScheduleOverride
        {
            ServiceId = serviceId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            IsDayOff = true
        };
        await DbContext.ScheduleOverrides.AddAsync(overrideEntity);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await this.Client.DeleteAsync($"/api/availability/services/{serviceId}/overrides/{overrideEntity.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.ScheduleOverrides.FindAsync(overrideEntity.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task UpdateWeeklySchedule_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var date = DateTime.UtcNow.AddDays(1).Date;
        var serviceId = await SeedServiceWithHoursAsync(date, 9, 17);
        
        // Create another provider
        var attackerEmail = "attacker@test.com";
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var attacker = new ApplicationUser { UserName = attackerEmail, Email = attackerEmail, FirstName = "A", LastName = "T" };
        await userManager.CreateAsync(attacker, "Password123!");
        await userManager.AddToRoleAsync(attacker, RoleConstants.Provider);

        var token = await GetAuthTokenAsync(attackerEmail, "Password123!");
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new WeeklyScheduleDto();

        // Act
        var response = await this.Client.PutAsJsonAsync($"/api/availability/services/{serviceId}/weekly", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Helpers ---

    private async Task<int> SeedServiceWithHoursAsync(DateTime date, int startHour, int endHour)
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var provider = new ApplicationUser
        {
            UserName = "provider@test.com",
            Email = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider"
        };
        
        // Check if user exists (might be reused across tests if not cleaned properly, though Respawn handles it)
        if (await userManager.FindByEmailAsync(provider.Email) == null)
        {
            await userManager.CreateAsync(provider, "Password123!");
            await userManager.AddToRoleAsync(provider, RoleConstants.Provider);
        }
        else
        {
            provider = await userManager.FindByEmailAsync(provider.Email);
        }

        var category = new Category
        {
            Name = $"Cat_{Guid.NewGuid()}",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Test Service",
            Description = "Desc",
            ProviderId = provider!.Id,
            DurationInMinutes = 60,
            Price = 100,
            IsActive = true,
            CategoryId = category.Id
        };
        
        await this.DbContext.Services.AddAsync(service);
        await this.DbContext.SaveChangesAsync();

        var hours = new OperatingHour
        {
            ServiceId = service.Id,
            DayOfWeek = date.DayOfWeek,
            Segments = new List<OperatingSegment>
            {
                new() { StartTime = new TimeOnly(startHour, 0), EndTime = new TimeOnly(endHour, 0) }
            }
        };
        await this.DbContext.OperatingHours.AddAsync(hours);
        await this.DbContext.SaveChangesAsync();

        return service.Id;
    }

    private async Task CreateBookingAsync(int serviceId, DateTime start)
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var customer = new ApplicationUser
        {
            UserName = $"customer_{Guid.NewGuid()}@test.com",
            Email = $"customer_{Guid.NewGuid()}@test.com",
            FirstName = "Test",
            LastName = "Customer"
        };
        
        await userManager.CreateAsync(customer, "Password123!");

        var booking = new Booking
        {
            ServiceId = serviceId,
            CustomerId = customer.Id,
            BookingStart = start,
            Status = BookingStatus.Confirmed
        };
        await this.DbContext.Bookings.AddAsync(booking);
        await this.DbContext.SaveChangesAsync();
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