namespace ServiceBookingSystem.Data.Entities.Common;

/// <summary>
/// Defines audit information for entities.
/// </summary>
public interface IAuditInfo
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTime CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last modified.
    /// </summary>
    DateTime? ModifiedOn { get; set; }
}
