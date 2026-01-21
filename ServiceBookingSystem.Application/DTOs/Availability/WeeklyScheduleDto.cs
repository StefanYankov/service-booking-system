namespace ServiceBookingSystem.Application.DTOs.Availability;

public class WeeklyScheduleDto
{
    public List<DayScheduleDto> Days { get; set; } = new();
}
