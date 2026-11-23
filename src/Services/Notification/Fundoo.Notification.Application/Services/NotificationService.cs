
using Fundoo.Notification.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fundoo.Notification.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailSender emailSender,
        ILogger<NotificationService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException(
                "Recipient email is required.", nameof(to));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException(
                "Email subject is required.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException(
                "Email body is required.", nameof(body));
        }

        await _emailSender.SendEmailAsync(
            to,
            subject,
            body,
            cancellationToken);

        _logger.LogInformation("Email delivery completed successfully");
    }
}
