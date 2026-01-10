using Microsoft.Extensions.Logging;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Data.Entities.Domain;

namespace ServiceBookingSystem.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailService emailService;
    private readonly ITemplateService templateService;
    private readonly ILogger<NotificationService> logger;

    public NotificationService(
        IEmailService emailService,
        ITemplateService templateService,
        ILogger<NotificationService> logger)
    {
        this.emailService = emailService;
        this.templateService = templateService;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task NotifyBookingCreatedAsync(Booking booking)
    {
        try
        {
            // 1. Notify Customer
            var customerParams = new Dictionary<string, string>
            {
                { "UserName", booking.Customer.FirstName },
                { "ServiceName", booking.Service.Name },
                { "BookingDate", booking.BookingStart.ToString("f") }
            };
            var customerBody = await templateService.RenderTemplateAsync("BookingCreatedCustomer.html", customerParams);
            await emailService.SendEmailAsync(booking.Customer.Email!, "Booking Received", customerBody);

            // 2. Notify Provider
            var providerParams = new Dictionary<string, string>
            {
                { "ProviderName", booking.Service.Provider.FirstName },
                { "ServiceName", booking.Service.Name },
                { "CustomerName", $"{booking.Customer.FirstName} {booking.Customer.LastName}" },
                { "BookingDate", booking.BookingStart.ToString("f") }
            };
            var providerBody = await templateService.RenderTemplateAsync("BookingCreatedProvider.html", providerParams);
            await emailService.SendEmailAsync(booking.Service.Provider.Email!, "New Booking Request", providerBody);
            
            logger.LogInformation("Sent creation notifications for Booking {BookingId}", booking.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send creation notifications for Booking {BookingId}", booking.Id);
        }
    }

    /// <inheritdoc/>
    public async Task NotifyBookingConfirmedAsync(Booking booking)
    {
        try
        {
            var templateParams = new Dictionary<string, string>
            {
                { "UserName", booking.Customer.FirstName },
                { "ServiceName", booking.Service.Name },
                { "ProviderName", $"{booking.Service.Provider.FirstName} {booking.Service.Provider.LastName}" },
                { "BookingDate", booking.BookingStart.ToString("f") }
            };
            var body = await templateService.RenderTemplateAsync("BookingConfirmed.html", templateParams);
            await emailService.SendEmailAsync(booking.Customer.Email!, "Booking Confirmed", body);
            
            logger.LogInformation("Sent confirmation notification for Booking {BookingId}", booking.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation notification for Booking {BookingId}", booking.Id);
        }
    }

    /// <inheritdoc/>
    public async Task NotifyBookingDeclinedAsync(Booking booking)
    {
        try
        {
            var templateParams = new Dictionary<string, string>
            {
                { "UserName", booking.Customer.FirstName },
                { "ServiceName", booking.Service.Name },
                { "BookingDate", booking.BookingStart.ToString("f") }
            };
            var body = await templateService.RenderTemplateAsync("BookingDeclined.html", templateParams);
            await emailService.SendEmailAsync(booking.Customer.Email!, "Booking Declined", body);
            
            logger.LogInformation("Sent decline notification for Booking {BookingId}", booking.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send decline notification for Booking {BookingId}", booking.Id);
        }
    }

    /// <inheritdoc/>
    public async Task NotifyBookingCancelledAsync(Booking booking, bool cancelledByProvider)
    {
        try
        {
            // If Provider cancelled, notify Customer. If Customer cancelled, notify Provider.
            var recipient = cancelledByProvider ? booking.Customer : booking.Service.Provider;
            
            var templateParams = new Dictionary<string, string>
            {
                { "RecipientName", recipient.FirstName },
                { "ServiceName", booking.Service.Name },
                { "BookingDate", booking.BookingStart.ToString("f") }
            };
            var body = await templateService.RenderTemplateAsync("BookingCancelled.html", templateParams);
            await emailService.SendEmailAsync(recipient.Email!, "Booking Cancelled", body);
            
            logger.LogInformation("Sent cancellation notification for Booking {BookingId} to {Recipient}", booking.Id, recipient.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send cancellation notification for Booking {BookingId}", booking.Id);
        }
    }
}