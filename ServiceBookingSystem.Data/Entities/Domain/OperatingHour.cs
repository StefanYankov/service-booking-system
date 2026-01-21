using System.ComponentModel.DataAnnotations.Schema;
using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Domain;

/// <summary>
/// Represents the configuration for a specific day of the week (e.g., "Mondays").
/// It contains a collection of time segments (shifts) for that day.
/// </summary>
public class OperatingHour : AuditableEntity<int>
{
    /// <summary>
    /// Gets or sets the day of the week for this configuration.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the collection of time segments (shifts) for this day.
    /// </summary>
    public ICollection<OperatingSegment> Segments { get; set; } = new List<OperatingSegment>();

    // --- Foreign Keys and Navigation Properties ---

    public int ServiceId { get; set; }

    [ForeignKey(nameof(ServiceId))]
    public virtual Service Service { get; set; } = null!;
}
