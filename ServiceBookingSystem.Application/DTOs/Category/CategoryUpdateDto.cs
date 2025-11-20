using System.ComponentModel.DataAnnotations;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Application.DTOs.Category;

public class CategoryUpdateDto
{
    [Required(ErrorMessage = $"{ErrorMessages.RequiredField} - {nameof(Id)}")]
    public int Id { get; set; }
    
    [Required(ErrorMessage = $"{ErrorMessages.RequiredField}")]
    [StringLength(ValidationConstraints.Category.NameMaxLength, MinimumLength =  ValidationConstraints.Category.NameMinLength)]
    public required string Name { get; set; }

    [StringLength(ValidationConstraints.Category.DescriptionMaxLength)]
    public string? Description { get; set; }
}