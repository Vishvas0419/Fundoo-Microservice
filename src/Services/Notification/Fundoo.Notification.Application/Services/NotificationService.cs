
using Fundoo.Notification.Application.Interfaces;

namespace Fundoo.Notification.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;

    public NotificationService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
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
    }
}
