using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Application.Interfaces;

namespace ServiceBookingSystem.Infrastructure.Messaging;

/// <summary>
/// A null implementation of the email service that does not send real emails.
/// It logs the email content to the console, which is useful for development and testing.
/// </summary>
public class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> logger;

    public NullEmailService(ILogger<NullEmailService> logger)
    {
        this.logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        logger.LogInformation("--- NULL EMAIL SENT (not really) ---");
        logger.LogInformation("To: {To}", to);
        logger.LogInformation("Subject: {Subject}", subject);
        logger.LogInformation("Body: {Body}", htmlContent);
        logger.LogInformation("------------------------------------");

        return Task.CompletedTask;
    }
}
