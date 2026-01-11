using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServiceBookingSystem.Application.DTOs.Identity;
using ServiceBookingSystem.Application.DTOs.Image;
using ServiceBookingSystem.Application.Interfaces.Infrastructure;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class ServiceImageControllerTests : BaseIntegrationTest
{
    private readonly Mock<IImageService> imageServiceMock;

    public ServiceImageControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) 
        : base(factory, output)
    {
        imageServiceMock = new Mock<IImageService>();
    }

    // Helper to create a client with the mocked service
    private HttpClient CreateClientWithMock()
    {
        return Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IImageService>(_ => imageServiceMock.Object);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task UploadImage_WithValidFile_ShouldReturn201Created()
    {
        // Arrange:
        const string providerEmail = "provider@test.com";
        const string password = "Password123!";
        await SeedProviderAndServiceAsync(providerEmail, password);
        
        var service = this.DbContext.Services.First();
        var token = await GetAuthTokenAsync(providerEmail, password);
        
        var client = CreateClientWithMock(); // Use custom client
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Fake JPG header
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        imageServiceMock
            .Setup(x => x.AddImageAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(new ImageStorageResult { Url = "https://cloud.com/img.jpg", PublicId = "123" });

        // Act:
        var response = await client.PostAsync($"/api/services/{service.Id}/images", content);

        // Assert:
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://cloud.com/img.jpg", result.GetProperty("url").GetString());
    }

    [Fact]
    public async Task UploadImage_WithInvalidExtension_ShouldReturn400BadRequest()
    {
        // Arrange:
        const string providerEmail = "provider@test.com";
        const string password = "Password123!";
        await SeedProviderAndServiceAsync(providerEmail, password);
        
        var service = this.DbContext.Services.First();
        var token = await GetAuthTokenAsync(providerEmail, password);
        
        var client = CreateClientWithMock();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0x00 });
        content.Add(fileContent, "file", "test.exe"); // Invalid extension

        // Act:
        var response = await client.PostAsync($"/api/services/{service.Id}/images", content);

        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_WithValidId_ShouldReturn204NoContent()
    {
        // Arrange:
        const string providerEmail = "provider@test.com";
        const string password = "Password123!";
        await SeedProviderAndServiceAsync(providerEmail, password);
        
        var service = this.DbContext.Services.First();
        var image = new ServiceImage
        {
            ServiceId = service.Id,
            ImageUrl = "url",
            PublicId = "123"
        };
        await this.DbContext.ServiceImages.AddAsync(image);
        await this.DbContext.SaveChangesAsync();

        var token = await GetAuthTokenAsync(providerEmail, password);
        var client = CreateClientWithMock();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act:
        var response = await client.DeleteAsync($"/api/services/{service.Id}/images/{image.Id}");

        // Assert:
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        imageServiceMock.Verify(x => x.DeleteImageAsync("123"), Times.Once);
    }

    private async Task SeedProviderAndServiceAsync(string email, string password)
    {
        var userManager = this.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var provider = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "Provider"
        };
        await userManager.CreateAsync(provider, password);
        await userManager.AddToRoleAsync(provider, "Provider");

        var category = new Category
        {
            Name = "Cat",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        await this.DbContext.SaveChangesAsync();

        var service = new Service
        {
            Name = "Service",
            Description = "Desc",
            ProviderId = provider.Id,
            CategoryId = category.Id
        };
        await this.DbContext.Services.AddAsync(service);
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