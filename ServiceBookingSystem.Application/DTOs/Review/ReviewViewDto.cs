namespace ServiceBookingSystem.Application.DTOs.Review;

public class ReviewViewDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public required string ServiceName { get; set; }
    public required string CustomerId { get; set; }
    public required string CustomerName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}