namespace ServiceBookingSystem.Core.Constants;

/// <summary>
/// Provides a centralized collection of constant string templates for data validation error messages.
/// These messages are designed to be used with data annotation attributes (e.g., [Required], [StringLength]).
/// The validation framework will automatically format these strings with specific property details at runtime.
/// </summary>
public static class ErrorMessages
{
    /// <summary>
    /// Generic message for a required field.
    /// The validation framework will replace {0} with the property name.
    /// </summary>
    public const string RequiredField = "The {0} field is required.";

    /// <summary>
    /// Generic message for string length validation.
    /// The validation framework will replace {0} with the property name,
    /// {1} with the maximum length, and {2} with the minimum length.
    /// </summary>
    public const string StringLengthRange = "The {0} field must be between {2} and {1} characters long.";

    /// <summary>
    /// Generic message for the maximum string length validation.
    /// </summary>
    public const string StringLengthMaxRange = "The {0} field must be between no longer than {1} characters long.";
    
    public const string AtLeastOneRoleIsRequired = "At least one role must be assigned.";
    
    public const string AtLeastOneMinuteDuration = "Duration must be at least 1 minute.";
}