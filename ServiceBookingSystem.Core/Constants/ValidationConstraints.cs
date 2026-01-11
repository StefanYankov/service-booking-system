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

    public static class User
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 50;
        
        public const int PasswordMinLength = 8;
        public const int PasswordMaxLength = 100;
        
        // Default ASP.NET Identity ID length
        public const int IdMaxLength = 450;
    }

    public static class Service
    {
        public const int NameMinLength = 5;
        public const int NameMaxLength = 200;

        public const int DescriptionMaxLength = 4000;

        public const int AddressStreetMaximumLength = 255;
        public const int AddressCityMaximumLength = 100;
        public const int PostalCodeMaximumLength = 10;

    }
    
    public static class Booking
    {
        public const int NotesMaxLength = 1000;
    }

    public static class Image
    {
        public const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    }
}