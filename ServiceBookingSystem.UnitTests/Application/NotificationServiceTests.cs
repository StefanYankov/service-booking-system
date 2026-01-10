using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Entities.Identity;

namespace ServiceBookingSystem.UnitTests.Application;

public class NotificationServiceTests
{
    private readonly Mock<IEmailService> emailServiceMock;
    private readonly Mock<ITemplateService> templateServiceMock;
    private readonly Mock<ILogger<NotificationService>> loggerMock;
    private readonly NotificationService notificationService;

    public NotificationServiceTests()
    {
        emailServiceMock = new Mock<IEmailService>();
        templateServiceMock = new Mock<ITemplateService>();
        loggerMock = new Mock<ILogger<NotificationService>>();
        notificationService = new NotificationService(emailServiceMock.Object, templateServiceMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task NotifyBookingCreatedAsync_ShouldSendTwoEmails()
    {
        // Arrange:
        var booking = CreateTestBooking();
        templateServiceMock.Setup(x => x.RenderTemplateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync("Body");

        // Act:
        await notificationService.NotifyBookingCreatedAsync(booking);

        // Assert::
        emailServiceMock
            .Verify(x => x.SendEmailAsync(booking.Customer.Email!, "Booking Received", "Body"), Times.Once);
        emailServiceMock
            .Verify(x => x.SendEmailAsync(booking.Service.Provider.Email!, "New Booking Request", "Body"), Times.Once);
    }

    [Fact]
    public async Task NotifyBookingConfirmedAsync_ShouldSendEmailToCustomer()
    {
        // Arrange:
        var booking = CreateTestBooking();
        templateServiceMock.
            Setup(x => x.RenderTemplateAsync("BookingConfirmed.html", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync("Confirmed Body");

        // Act:
        await notificationService.NotifyBookingConfirmedAsync(booking);

        // Assert:
        emailServiceMock.Verify(x => x.SendEmailAsync(booking.Customer.Email!, "Booking Confirmed", "Confirmed Body"), Times.Once);
    }

    [Fact]
    public async Task NotifyBookingDeclinedAsync_ShouldSendEmailToCustomer()
    {
        // Arrange:
        var booking = CreateTestBooking();
        templateServiceMock
            .Setup(x => x.RenderTemplateAsync("BookingDeclined.html", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync("Declined Body");

        // Act:
        await notificationService.NotifyBookingDeclinedAsync(booking);

        // Assert:
        emailServiceMock
            .Verify(x => x.SendEmailAsync(booking.Customer.Email!, "Booking Declined", "Declined Body"), Times.Once);
    }

    [Fact]
    public async Task NotifyBookingCancelledAsync_ByProvider_ShouldNotifyCustomer()
    {
        // Arrange:
        var booking = CreateTestBooking();
        templateServiceMock.Setup(x => x.RenderTemplateAsync("BookingCancelled.html", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync("Cancelled Body");

        // Act:
        await notificationService.NotifyBookingCancelledAsync(booking, cancelledByProvider: true);

        // Assert:
        emailServiceMock
            .Verify(x => x.SendEmailAsync(booking.Customer.Email!, "Booking Cancelled", "Cancelled Body"), Times.Once);
        emailServiceMock
            .Verify(x => x.SendEmailAsync(booking.Service.Provider.Email!, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyBookingCancelledAsync_ByCustomer_ShouldNotifyProvider()
    {
        // Arrange:
        var booking = CreateTestBooking();
        templateServiceMock
            .Setup(x => x.RenderTemplateAsync("BookingCancelled.html", It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync("Cancelled Body");

        // Act:
        await notificationService.NotifyBookingCancelledAsync(booking, cancelledByProvider: false);

        // Assert:
        emailServiceMock
            .Verify(x => x.SendEmailAsync(booking.Service.Provider.Email!, "Booking Cancelled", "Cancelled Body"), Times.Once);
        emailServiceMock
            .Verify(x => x.SendEmailAsync(booking.Customer.Email!, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Notify_WhenEmailFails_ShouldLogAndNotThrow()
    {
        // Arrange:
        var booking = CreateTestBooking();
        emailServiceMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP Error"));

        // Act:
        var exception = await Record.ExceptionAsync(() => notificationService.NotifyBookingConfirmedAsync(booking));

        // Assert:
        Assert.Null(exception);
    }

    private Booking CreateTestBooking()
    {
        return new Booking
        {
            Id = "b1",
            BookingStart = DateTime.UtcNow,
            CustomerId = "customer-1",
            Customer = new ApplicationUser { Id = "customer-1", FirstName = "Customer", LastName = "Omer", Email = "cust@test.com" },
            ServiceId = 1,
            Service = new Service
            {
                Id = 1,
                Name = "Test Service",
                Description = "Test Desc",
                ProviderId = "prov-1",
                Provider = new ApplicationUser { Id = "prov-1", FirstName = "Prov", LastName = "Ider", Email = "prov@test.com" }
            }
        };
    }
}