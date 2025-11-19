namespace ServiceBookingSystem.Data.Common;

/// <summary>
/// Contains constant string values for role names used throughout the application.
/// Using constants helps prevent typos and "magic strings" in the code,
/// making it more robust and easier to maintain.
/// </summary>
public static class RoleConstants
{
    /// <summary>
    /// The role for the system owner/manager with global control over the application.
    /// </summary>
    public const string Administrator = "Administrator";

    /// <summary>
    /// The role for a service seller or offeror who registers to provide services.
    /// </summary>
    public const string Provider = "Provider";

    /// <summary>
    /// The role for a service buyer or booker who registers to book available services.
    /// </summary>
    public const string Customer = "Customer";
}
