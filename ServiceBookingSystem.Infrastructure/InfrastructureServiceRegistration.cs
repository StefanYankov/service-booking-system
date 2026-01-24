using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Application.Interfaces.Infrastructure;
using ServiceBookingSystem.Infrastructure.FileStorage;
using ServiceBookingSystem.Infrastructure.Hubs;
using ServiceBookingSystem.Infrastructure.Messaging;
using ServiceBookingSystem.Infrastructure.Settings;
using ServiceBookingSystem.Infrastructure.Templating;

namespace ServiceBookingSystem.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Email Services ---
        var emailSettings = new EmailSettings();
        configuration.GetSection("EmailSettings").Bind(emailSettings);
        services.AddSingleton(emailSettings);

        if (emailSettings.EnableRealEmail && !string.IsNullOrEmpty(emailSettings.SendGridApiKey))
        {
            services.AddTransient<IEmailService, SendGridEmailService>();
        }
        else
        {
            services.AddTransient<IEmailService, NullEmailService>();
        }
        
        // Adapter for Identity UI
        services.AddTransient<IEmailSender, IdentityEmailSenderAdapter>();

        // --- Template Service ---
        services.AddTransient<ITemplateService, TemplateService>();

        // --- File Storage (Cloudinary) ---
        services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
        services.AddTransient<IImageService, CloudinaryImageService>();

        // --- Real-Time Notifications (SignalR) ---
        services.AddTransient<IRealTimeNotificationService, SignalRNotificationService>();
        
        // Ensure SignalR uses the correct User ID claim
        services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

        return services;
    }
}