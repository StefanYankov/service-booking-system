using Microsoft.EntityFrameworkCore;
using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Extensions;

public static class SoftDeleteExtensions
{
    /// <summary>
    /// Marks an entity as soft-deleted without triggering EF Core's hard-delete validation logic.
    /// This is required for entities with required relationships configured to Restrict delete.
    /// </summary>
    public static void SoftDelete<T>(this DbContext context, T entity) 
        where T : class, IDeletableEntity
    {
        entity.IsDeleted = true;
        entity.DeletedOn = DateTime.UtcNow;
        context.Entry(entity).State = EntityState.Modified;
    }
}