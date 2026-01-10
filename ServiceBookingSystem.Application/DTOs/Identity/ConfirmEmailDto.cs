using System.ComponentModel.DataAnnotations;

namespace ServiceBookingSystem.Application.DTOs.Identity;

public class ConfirmEmailDto
{
    [Required]
    public required string UserId { get; init; }

    [Required]
    public required string Code { get; init; }
}