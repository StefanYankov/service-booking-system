namespace ServiceBookingSystem.Application.DTOs.Availability;

public class DayScheduleDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public List<TimeSegmentDto> Segments { get; set; } = new();
}
