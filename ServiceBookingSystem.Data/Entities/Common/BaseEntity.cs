using System.ComponentModel.DataAnnotations;

namespace ServiceBookingSystem.Data.Entities.Common;

/// <summary>
/// A base class for all entities in the system.
/// It provides a primary key property.
/// </summary>
/// <typeparam name="TKey">The type of the primary key (e.g., int, string, Guid).</typeparam>
public abstract class BaseEntity<TKey>
{
    /// <summary>
    /// Gets or sets the primary key for this entity.
    /// </summary>
    [Key]
    public required TKey Id { get; set; }
}
