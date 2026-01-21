using ServiceBookingSystem.Data.Entities.Common;

namespace ServiceBookingSystem.Data.Entities.Domain;

public class OperatingSegment : AuditableEntity<int>
{
    public int OperatingHourId { get; set; }
    public OperatingHour OperatingHour { get; set; } = null!;

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
