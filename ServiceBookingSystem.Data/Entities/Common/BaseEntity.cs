using System.ComponentModel.DataAnnotations;

namespace ServiceBookingSystem.Data.Entities.Common;

/// <summary>
/// Base class for entities with audit information.
/// </summary>
/// <typeparam name="TKey">The type of the primary key.</typeparam>
public class BaseEntity<TKey> : IAuditInfo
{
    [Key]
    public TKey Id { get; set; }

    // <inheritDoc />
    public DateTime CreatedOn { get; set; }

    // <inheritDoc />
    public DateTime? ModifiedOn { get; set; }
}