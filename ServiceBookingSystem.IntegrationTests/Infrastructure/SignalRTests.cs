using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Infrastructure.Hubs;
using Xunit.Abstractions;

namespace ServiceBookingSystem.IntegrationTests.Infrastructure;

public class SignalRTests : BaseIntegrationTest
{
    public SignalRTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory, output)
    {
    }

    [Fact]
    public void SignalR_Services_ShouldBeRegistered()
    {
        // Act
        var hubContext = ServiceProvider.GetService<Microsoft.AspNetCore.SignalR.IHubContext<NotificationHub>>();

        // Assert
        Assert.NotNull(hubContext);
    }
}
