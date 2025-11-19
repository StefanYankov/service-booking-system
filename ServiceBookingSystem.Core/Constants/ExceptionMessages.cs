namespace ServiceBookingSystem.Core.Constants;

/// <summary>
/// Provides a centralized collection of constant string values for error messages used throughout the application.
/// Using constants helps prevent "magic strings" in the code, ensures consistency,
/// and simplifies maintenance and localization.
/// </summary>
public static class ExceptionMessages
{
    /// <summary>
    /// Error message for when a category with the same name already exists.
    /// Expects one format argument: {0} = category name.
    /// </summary>
    public const string CategoryAlreadyExists = "A category with the name '{0}' already exists.";

    /// <summary>
    /// Error message for when a requested entity is not found.
    /// Expects two format arguments: {0} = entity name, {1} = entity key.
    /// </summary>
    public const string EntityNotFound = "The requested {0} with ID '{1}' was not found.";
    
}
