using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Application.DTOs.Identity;

public class ChangePasswordDto
{
    [Required]
    public required string OldPassword { get; init; }

    [Required]
    [StringLength(ValidationConstraints.User.PasswordMaxLength, MinimumLength = ValidationConstraints.User.PasswordMinLength)]
    public required string NewPassword { get; init; }

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = ErrorMessages.PasswordsDoNotMatch)]
    public required string ConfirmNewPassword { get; init; }
}