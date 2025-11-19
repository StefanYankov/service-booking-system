namespace ServiceBookingSystem.Data.Entities.Common;

/// <summary>
/// A base class for entities that require audit information (creation and modification timestamps).
/// Inherits from BaseEntity to include a primary key.
/// </summary>
/// <typeparam name="TKey">The type of the primary key.</typeparam>
public abstract class AuditableEntity<TKey> : BaseEntity<TKey>, IAuditInfo
{
    /// <summary>
    /// Gets or sets the date and time, in UTC, when the entity was created.
    /// This is automatically set by the ApplicationDbContext upon creation.
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when the entity was last modified.
    /// This is automatically set by the ApplicationDbContext upon modification.
    /// </summary>
    public DateTime? ModifiedOn { get; set; }
}
