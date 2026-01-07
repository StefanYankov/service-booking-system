using System.ComponentModel.DataAnnotations;

namespace ServiceBookingSystem.Application.DTOs.Review;

public class ReviewCreateDto
{
    [Required]
    public int ServiceId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [StringLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters.")]
    public string? Comment { get; set; }
}