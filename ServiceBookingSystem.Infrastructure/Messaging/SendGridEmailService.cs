using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Infrastructure.Settings;

namespace ServiceBookingSystem.Infrastructure.Messaging;

/// <summary>
/// An implementation of the email service using the SendGrid API.
/// </summary>
public class SendGridEmailService : IEmailService
{
    private readonly ILogger<SendGridEmailService> logger;
    private readonly EmailSettings emailSettings;

    public SendGridEmailService(ILogger<SendGridEmailService> logger, EmailSettings emailSettings)
    {
        this.logger = logger;
        this.emailSettings = emailSettings;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        if (string.IsNullOrEmpty(emailSettings.SendGridApiKey) || 
            string.IsNullOrEmpty(emailSettings.FromAddress) || 
            string.IsNullOrEmpty(emailSettings.FromName))
        {
            logger.LogError("SendGrid API Key or From Address/Name is not configured. Cannot send email.");
            return;
        }

        try
        {
            var client = new SendGridClient(emailSettings.SendGridApiKey);
            var from = new EmailAddress(emailSettings.FromAddress, emailSettings.FromName);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send email via SendGrid. Status Code: {StatusCode}, Body: {Body}", 
                    response.StatusCode, await response.Body.ReadAsStringAsync());
            }
            else
            {
                logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'", to, subject);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while sending an email with SendGrid.");
        }
    }
}
