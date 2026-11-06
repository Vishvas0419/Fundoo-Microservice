
using Fundoo.Notification.Application.Interfaces;
using Fundoo.Notification.Infrastructure.Email;
using Fundoo.Notification.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fundoo.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(
            configuration.GetSection("SmtpSettings"));

        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
