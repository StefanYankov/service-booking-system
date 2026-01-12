using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.Data.Seeders;

public class DemoDataSeeder
{
    public async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<DemoDataSeeder>>();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (dbContext.Services.Any())
        {
            logger.LogInformation("Demo data already seeded.");
            return;
        }

        logger.LogInformation("Seeding demo data...");

        // 1. Create Provider
        var provider = new ApplicationUser
        {
            UserName = "provider@demo.com",
            Email = "provider@demo.com",
            FirstName = "Bob",
            LastName = "The Builder",
            EmailConfirmed = true
        };
        
        if (await userManager.FindByEmailAsync(provider.Email) == null)
        {
            await userManager.CreateAsync(provider, "Password123!");
            await userManager.AddToRoleAsync(provider, RoleConstants.Provider);
        }
        else
        {
            provider = await userManager.FindByEmailAsync(provider.Email);
        }

        // 2. Create Categories
        var categories = new List<Category>
        {
            new() { Name = "Plumbing", Description = "Pipe repairs and installation" },
            new() { Name = "Electrical", Description = "Wiring and lighting" },
            new() { Name = "Cleaning", Description = "Home and office cleaning" }
        };

        foreach (var cat in categories)
        {
            if (!dbContext.Categories.Any(c => c.Name == cat.Name))
            {
                await dbContext.Categories.AddAsync(cat);
            }
        }
        await dbContext.SaveChangesAsync();

        // Reload categories to get IDs
        var plumbing = dbContext.Categories.First(c => c.Name == "Plumbing");
        var electrical = dbContext.Categories.First(c => c.Name == "Electrical");

        // 3. Create Services
        var services = new List<Service>
        {
            new()
            {
                Name = "Emergency Pipe Repair",
                Description = "24/7 fix for leaks.",
                Price = 80,
                DurationInMinutes = 60,
                CategoryId = plumbing.Id,
                ProviderId = provider!.Id,
                City = "Sofia",
                StreetAddress = "10 Vitosha Blvd",
                PostalCode = "1000",
                IsOnline = false,
                IsActive = true
            },
            new()
            {
                Name = "Bathroom Installation",
                Description = "Full bathroom renovation.",
                Price = 500,
                DurationInMinutes = 480,
                CategoryId = plumbing.Id,
                ProviderId = provider.Id,
                City = "Plovdiv",
                StreetAddress = "5 Main St",
                PostalCode = "4000",
                IsOnline = false,
                IsActive = true
            },
            new()
            {
                Name = "Online Consultation",
                Description = "Video call for DIY advice.",
                Price = 30,
                DurationInMinutes = 30,
                CategoryId = electrical.Id,
                ProviderId = provider.Id,
                City = "Varna", // Even online services might have a base city
                StreetAddress = "Virtual",
                PostalCode = "9000",
                IsOnline = true,
                IsActive = true
            }
        };

        await dbContext.Services.AddRangeAsync(services);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Demo data seeding completed.");
    }
}