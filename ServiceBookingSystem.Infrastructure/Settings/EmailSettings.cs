namespace ServiceBookingSystem.Infrastructure.Settings;

/// <summary>
/// Holds the configuration settings for email services.
/// This class is populated from the "EmailSettings" section of the configuration file.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to use the real email service (e.g., SendGrid).
    /// If false, a null service that does nothing will be used.
    /// </summary>
    public bool EnableRealEmail { get; set; }

    /// <summary>
    /// Gets or sets the API key for the email provider (e.g., SendGrid).
    /// </summary>
    public string? SendGridApiKey { get; set; }
    
    /// <summary>
    /// Gets or sets the email address that emails will be sent from.
    /// </summary>
    public string? FromAddress { get; set; }

    /// <summary>
    /// Gets or sets the name associated with the from address (e.g., "Service Booking System Support").
    /// </summary>
    public string? FromName { get; set; }
}
