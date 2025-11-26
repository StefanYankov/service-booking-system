namespace ServiceBookingSystem.Application.Interfaces;

/// <summary>
/// Defines the contract for a service that sends emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email.
    /// </summary>
    /// <param name="to">The recipient's email address.</param>
    /// <param name="subject">The subject of the email.</param>
    /// <param name="htmlContent">The HTML content of the email body.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    Task SendEmailAsync(string to, string subject, string htmlContent);
}
