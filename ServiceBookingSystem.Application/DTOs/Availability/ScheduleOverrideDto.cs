namespace ServiceBookingSystem.Application.DTOs.Availability;

public class ScheduleOverrideDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public bool IsDayOff { get; set; }
    public List<TimeSegmentDto> Segments { get; set; } = new();
}
