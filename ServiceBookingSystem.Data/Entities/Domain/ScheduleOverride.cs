using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Domain;

public class ScheduleOverride : AuditableEntity<int>
{
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public DateOnly Date { get; set; }
    
    public bool IsDayOff { get; set; }

    public ICollection<OverrideSegment> Segments { get; set; } = new List<OverrideSegment>();
}
