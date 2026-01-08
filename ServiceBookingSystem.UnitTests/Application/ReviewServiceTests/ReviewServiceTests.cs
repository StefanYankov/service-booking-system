using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Application.Services;
using ServiceBookingSystem.Data.Contexts;

namespace ServiceBookingSystem.UnitTests.Application.ReviewServiceTests;

public partial class ReviewServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> dbContextOptions;
    private readonly ApplicationDbContext dbContext;
    private readonly Mock<ILogger<ReviewService>> loggerMock;
    private readonly Mock<IBookingService> bookingServiceMock;
    private readonly ReviewService reviewService;

    public ReviewServiceTests()
    {
        dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ReviewServiceTests_{Guid.NewGuid()}")
            .Options;
        dbContext = new ApplicationDbContext(dbContextOptions);
        
        loggerMock = new Mock<ILogger<ReviewService>>();
        bookingServiceMock = new Mock<IBookingService>();

        reviewService = new ReviewService(
            dbContext,
            loggerMock.Object,
            bookingServiceMock.Object);
    }

    public void Dispose()
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Dispose();
    }
}