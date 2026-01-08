namespace ServiceBookingSystem.Application.DTOs.Review;

public class ReviewSummaryDto
{
    public int ServiceId { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}