using System.ComponentModel.DataAnnotations.Schema;
using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Domain;

/// <summary>
/// Represents a single block of time on a specific day of the week when a service is available.
/// This entity is auditable but not soft-deletable, as its existence is purely relational
/// to its parent Service. If an operating hour is removed, it is hard-deleted.
/// </summary>
public class OperatingHour : AuditableEntity<int>
{
    /// <summary>
    /// Gets or sets the day of the week for this time block.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the start time of the operating block.
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the operating block.
    /// </summary>
    public TimeOnly EndTime { get; set; }

    // --- Foreign Keys and Navigation Properties ---

    public int ServiceId { get; set; }

    [ForeignKey(nameof(ServiceId))]
    public virtual Service Service { get; set; } = null!;
}
