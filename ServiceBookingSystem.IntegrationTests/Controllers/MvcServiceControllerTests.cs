using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServiceBookingSystem.Application.DTOs.Image;
using ServiceBookingSystem.Application.Interfaces.Infrastructure;
using ServiceBookingSystem.Data.Common;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class MvcServiceControllerTests : BaseIntegrationTest
{
    private readonly Mock<IImageService> imageServiceMock;

    public MvcServiceControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
        imageServiceMock = new Mock<IImageService>();
    }

    [Fact]
    public async Task Post_Create_Valid_Redirects()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var category = await SeedCategoryAsync();
        var client = CreateAuthenticatedClient(provider.Id, imageServiceMock.Object);
        
        // Force English culture
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        var getResponse = await client.GetAsync("/Service/Create");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "Name", "New Service" },
            { "Description", "A brand new service description." },
            { "Price", "50" }, // Use integer to avoid culture issues
            { "DurationInMinutes", "60" },
            { "CategoryId", category.Id.ToString() },
            { "IsOnline", "true" },
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Service/Create", new FormUrlEncodedContent(formData));

        // Assert
        if (response.StatusCode != HttpStatusCode.Redirect && response.StatusCode != HttpStatusCode.Found)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"Response Content: {errorContent}"); 
        }
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Service/MyServices", response.Headers.Location!.ToString());
        
        // Verify DB
        var service = DbContext.Services.FirstOrDefault(s => s.Name == "New Service");
        Assert.NotNull(service);
        Assert.Equal(provider.Id, service.ProviderId);
    }

    [Fact]
    public async Task Post_Edit_WithNewImage_ShouldUploadAndRedirect()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        
        var client = CreateAuthenticatedClient(provider.Id, imageServiceMock.Object);
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        // Mock Image Service
        imageServiceMock.Setup(x => x.AddImageAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>()))
            .ReturnsAsync(new ImageStorageResult { Url = "https://test.com/img.jpg", PublicId = "pid" });

        // Get Edit Page to get Token
        var getResponse = await client.GetAsync($"/Service/Edit/{service.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(service.Id.ToString()), "Id");
        content.Add(new StringContent(service.Name), "Name");
        content.Add(new StringContent(service.Description), "Description");
        content.Add(new StringContent(service.Price.ToString()), "Price");
        content.Add(new StringContent(service.DurationInMinutes.ToString()), "DurationInMinutes");
        content.Add(new StringContent(service.CategoryId.ToString()), "CategoryId");
        content.Add(new StringContent(token), "__RequestVerificationToken");
        
        // Add File
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "NewImage", "test.jpg");

        // Act
        var response = await client.PostAsync("/Service/Edit", content);

        // Assert
        if (response.StatusCode != HttpStatusCode.Redirect && response.StatusCode != HttpStatusCode.Found)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"Response Content: {errorContent}"); 
        }
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Service/Edit/{service.Id}", response.Headers.Location!.ToString());
        
        // Verify DB
        var image = DbContext.ServiceImages.FirstOrDefault(i => i.ServiceId == service.Id);
        Assert.NotNull(image);
        Assert.Equal("https://test.com/img.jpg", image.ImageUrl);
    }

    [Fact]
    public async Task Post_SetThumbnail_ShouldUpdateAndRedirect()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var image1 = await SeedImageAsync(service.Id, false);
        var image2 = await SeedImageAsync(service.Id, true); // Currently main

        var client = CreateAuthenticatedClient(provider.Id, imageServiceMock.Object);
        
        // Get Edit Page for Token
        var getResponse = await client.GetAsync($"/Service/Edit/{service.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "serviceId", service.Id.ToString() },
            { "imageId", image1.Id.ToString() },
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Service/SetThumbnail", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Verify DB
        DbContext.ChangeTracker.Clear(); // Clear cache to get fresh data
        var dbImage1 = await DbContext.ServiceImages.FindAsync(image1.Id);
        var dbImage2 = await DbContext.ServiceImages.FindAsync(image2.Id);
        
        Assert.True(dbImage1!.IsThumbnail);
        Assert.False(dbImage2!.IsThumbnail);
    }

    [Fact]
    public async Task Post_DeleteImage_ShouldDeleteAndRedirect()
    {
        // Arrange
        var provider = await SeedProviderAsync();
        var service = await SeedServiceAsync(provider.Id);
        var image = await SeedImageAsync(service.Id, false);

        var client = CreateAuthenticatedClient(provider.Id, imageServiceMock.Object);
        
        imageServiceMock.Setup(x => x.DeleteImageAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var getResponse = await client.GetAsync($"/Service/Edit/{service.Id}");
        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            { "serviceId", service.Id.ToString() },
            { "imageId", image.Id.ToString() },
            { "__RequestVerificationToken", token }
        };

        // Act
        var response = await client.PostAsync("/Service/DeleteImage", new FormUrlEncodedContent(formData));

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Verify DB
        DbContext.ChangeTracker.Clear();
        var dbImage = await DbContext.ServiceImages.FindAsync(image.Id);
        Assert.Null(dbImage);
    }

    // --- Helpers ---

    private HttpClient CreateAuthenticatedClient(string userId, IImageService mockImageService)
    {
        var client = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Mock Auth
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });

                // Mock Image Service
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IImageService));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(mockImageService);
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

    private async Task<ApplicationUser> SeedProviderAsync()
    {
        var userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = $"p_{Guid.NewGuid()}@test.com", Email = $"p_{Guid.NewGuid()}@test.com", FirstName = "P", LastName = "T" };
        var result = await userManager.CreateAsync(user, "Password123!");
        if (!result.Succeeded) throw new Exception("Failed to create provider");
        
        var roleResult = await userManager.AddToRoleAsync(user, RoleConstants.Provider);
        if (!roleResult.Succeeded) throw new Exception($"Failed to add provider role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");

        return user;
    }

    private async Task<Category> SeedCategoryAsync()
    {
        var category = new Category { Name = $"Cat_{Guid.NewGuid()}", Description = "D" };
        await DbContext.Categories.AddAsync(category);
        await DbContext.SaveChangesAsync();
        return category;
    }

    private async Task<Service> SeedServiceAsync(string providerId)
    {
        var category = await SeedCategoryAsync();
        var service = new Service 
        { 
            Name = "Valid Service Name", 
            Description = "This is a valid description with enough characters.", 
            ProviderId = providerId, 
            CategoryId = category.Id, 
            Price = 10, 
            DurationInMinutes = 60 
        };
        await DbContext.Services.AddAsync(service);
        await DbContext.SaveChangesAsync();
        return service;
    }

    private async Task<ServiceImage> SeedImageAsync(int serviceId, bool isThumbnail)
    {
        var image = new ServiceImage { ServiceId = serviceId, ImageUrl = "url", PublicId = "pid", IsThumbnail = isThumbnail };
        await DbContext.ServiceImages.AddAsync(image);
        await DbContext.SaveChangesAsync();
        return image;
    }
}