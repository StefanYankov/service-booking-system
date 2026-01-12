using Microsoft.AspNetCore.Identity.UI.Services;
using ServiceBookingSystem.Application.Interfaces;

namespace ServiceBookingSystem.Infrastructure.Messaging;

public class IdentityEmailSenderAdapter : IEmailSender
{
    private readonly IEmailService _emailService;

    public IdentityEmailSenderAdapter(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        await _emailService.SendEmailAsync(email, subject, htmlMessage);
    }
}