using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Domain;

public class OverrideSegment : AuditableEntity<int>
{
    public int ScheduleOverrideId { get; set; }
    public ScheduleOverride ScheduleOverride { get; set; } = null!;

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
