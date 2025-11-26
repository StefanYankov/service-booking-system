using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceBookingSystem.Application.Interfaces;
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

        return services;
    }
}
