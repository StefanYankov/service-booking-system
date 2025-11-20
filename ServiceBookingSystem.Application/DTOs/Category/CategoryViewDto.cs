namespace ServiceBookingSystem.Application.DTOs.Category;

public class CategoryViewDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}