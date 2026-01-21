namespace ServiceBookingSystem.Web.Models;

public class ScheduleOverrideListViewModel
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public List<ScheduleOverrideViewModel> Overrides { get; set; } = new();
}

public class ScheduleOverrideViewModel
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public bool IsDayOff { get; set; }
    public List<TimeSegmentViewModel> Segments { get; set; } = new();
}

public class CreateOverrideViewModel
{
    public int ServiceId { get; set; }
    public DateOnly Date { get; set; }
    public bool IsDayOff { get; set; }
    
    // For binding simple start/end if not day off
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}
