namespace ServiceBookingSystem.Data.Entities.Common;

/// <summary>
/// A base class for entities that support both auditing and soft deletion.
/// This is the recommended base class for most domain entities.
/// It provides a primary key, audit timestamps, and soft-delete flags.
/// </summary>
/// <typeparam name="TKey">The type of the primary key.</typeparam>
public abstract class DeletableEntity<TKey> : AuditableEntity<TKey>, IDeletableEntity
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been soft-deleted.
    /// This flag is used by a global query filter to automatically exclude deleted items.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when the entity was soft-deleted.
    /// </summary>
    public DateTime? DeletedOn { get; set; }
}
