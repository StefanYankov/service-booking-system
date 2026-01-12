using System.Net;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Controllers;

public class HomeControllerTests : BaseIntegrationTest
{
    public HomeControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public async Task Index_ReturnsSuccessAndHtml()
    {
        // Act:
        var response = await this.Client.GetAsync("/");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("text/html", response.Content.Headers.ContentType!.ToString());
        Assert.Contains("Find the Perfect Service", content);
    }

    [Fact]
    public async Task Index_PopulatesDropdowns()
    {
        // Arrange:
        var category = new Category
        {
            Name = "TestCat",
            Description = "Desc"
        };
        await this.DbContext.Categories.AddAsync(category);
        
        var provider = new ApplicationUser
        {
            UserName = "p@t.com",
            Email = "p@t.com", 
            FirstName = "P",
            LastName = "T"
        };
        await this.DbContext.Users.AddAsync(provider);
        await this.DbContext.SaveChangesAsync();
        
        var service = new Service
        {
            Name = "S1",
            Description = "D",
            ProviderId = provider.Id,
            CategoryId = category.Id,
            City = "TestCity"
        };
        
        await this.DbContext.Services.AddAsync(service);
        
        await this.DbContext.SaveChangesAsync();

        // Act:
        var response = await this.Client.GetAsync("/");

        // Assert:
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("TestCat", content);
        Assert.Contains("TestCity", content);
    }
}