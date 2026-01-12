using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Identity;

public class IdentityPagesTests : BaseIntegrationTest
{
    public IdentityPagesTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Theory]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    public async Task Get_IdentityPage_ReturnsSuccess(string url)
    {
        // Act
        var response = await this.Client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("text/html", response.Content.Headers.ContentType!.ToString());
    }
}