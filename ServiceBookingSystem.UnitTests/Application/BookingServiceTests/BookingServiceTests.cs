using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Application.Interfaces.Infrastructure;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Data.Contexts;

namespace ServiceBookingSystem.UnitTests.Application.BookingServiceTests;

public partial class BookingServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> dbContextOptions;
    private readonly ApplicationDbContext dbContext;
    private readonly Mock<ILogger<BookingService>> loggerMock;
    private readonly Mock<IServiceService> serviceServiceMock;
    private readonly Mock<IUsersService> usersServiceMock;
    private readonly Mock<IAvailabilityService> availabilityServiceMock;
    private readonly Mock<INotificationService> notificationServiceMock;
    private readonly Mock<IRealTimeNotificationService> realTimeNotificationServiceMock;
    private readonly BookingService bookingService;

    public BookingServiceTests()
    {
        dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"BookingServiceTests_{Guid.NewGuid()}")
            .Options;
        dbContext = new ApplicationDbContext(dbContextOptions);
        
        loggerMock = new Mock<ILogger<BookingService>>();
        serviceServiceMock = new Mock<IServiceService>();
        usersServiceMock = new Mock<IUsersService>();
        availabilityServiceMock = new Mock<IAvailabilityService>();
        notificationServiceMock = new Mock<INotificationService>();
        realTimeNotificationServiceMock = new Mock<IRealTimeNotificationService>();

        bookingService = new BookingService(
            dbContext,
            loggerMock.Object,
            serviceServiceMock.Object,
            availabilityServiceMock.Object,
            usersServiceMock.Object,
            notificationServiceMock.Object,
            realTimeNotificationServiceMock.Object);
    }

    public void Dispose()
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Dispose();
    }
}