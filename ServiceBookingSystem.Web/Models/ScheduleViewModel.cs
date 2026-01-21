namespace ServiceBookingSystem.Web.Models;

public class ScheduleViewModel
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;

    public List<DayScheduleViewModel> Days { get; set; } = new();
}

public class DayScheduleViewModel
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public List<TimeSegmentViewModel> Segments { get; set; } = new();
}

public class TimeSegmentViewModel
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
}
