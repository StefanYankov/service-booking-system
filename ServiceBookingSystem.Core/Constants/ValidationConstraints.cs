namespace ServiceBookingSystem.Core.Constants;

/// <summary>
/// Provides a centralized collection of constant integer values for validation constraints,
/// such as minimum and maximum string lengths.
/// Using constants ensures that validation rules are consistent between the data model attributes
/// and any manual validation logic.
/// </summary>
public static class ValidationConstraints
{
    public static class Category
    {
        public const int NameMinLength = 3;
        public const int NameMaxLength = 100;
        
        public const int DescriptionMaxLength = 4000;
    }

    public static class Service
    {
        public const int NameMinLength = 5;
        public const int NameMaxLength = 200;

        public const int DescriptionMaxLength = 4000;
    }
}