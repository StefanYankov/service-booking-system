using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Data.Contexts;
using ServiceBookingSystem.Data.Entities.Domain;
using ServiceBookingSystem.Data.Extensions;

namespace ServiceBookingSystem.UnitTests.Data.Extensions;

public class SoftDeleteExtensionsTests
{
    [Fact]
    public void SoftDelete_ShouldSetIsDeletedTrue_AndStateToModified()
    {
        // Arrange:
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"SoftDeleteTest_{Guid.NewGuid()}")
            .Options;

        using var context = new ApplicationDbContext(options);

        var entity = new Service
        {
            Id = 1,
            Name = "Test",
            Description = "Test",
            ProviderId = "123",
            CategoryId = 1
        };

        context.Services.Attach(entity);
        
        // Verify initial state
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedOn);
        Assert.Equal(EntityState.Unchanged, context.Entry(entity).State);

        // Act:
        context.SoftDelete(entity);

        // Assert:
        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedOn);
        
        Assert.Equal(EntityState.Modified, context.Entry(entity).State);
    }
}