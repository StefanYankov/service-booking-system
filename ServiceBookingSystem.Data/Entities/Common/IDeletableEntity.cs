namespace ServiceBookingSystem.Data.Entities.Common;

/// <summary>
/// Defines the contract for entities that support "soft deletion".
/// Instead of being permanently removed from the database, entities implementing this interface
/// are marked as deleted. This preserves data history and allows for "undelete" functionality.
/// </summary>
public interface IDeletableEntity
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been deleted.
    /// When true, the entity is considered "soft-deleted" and should not be included
    /// in normal query results.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when the entity was deleted.
    /// This property is nullable and should only have a value when IsDeleted is true.
    /// </summary>
    DateTime? DeletedOn { get; set; }
}
