namespace ServiceBookingSystem.Application.DTOs.Identity.User;

public class UserViewDto
{
    public required string Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}