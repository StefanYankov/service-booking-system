using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Application.Interfaces.Infrastructure;
using ServiceBookingSystem.Infrastructure.FileStorage;
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

        // --- Template Service ---
        services.AddTransient<ITemplateService, TemplateService>();

        // --- File Storage (Cloudinary) ---
        services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
        services.AddTransient<IImageService, CloudinaryImageService>();

        return services;
    }
}