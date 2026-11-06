
namespace Fundoo.Notification.Application.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}


//This interface defines the operation that the API controller will call. The implementation will be NotificationService.cs in the Application project's Services folder.