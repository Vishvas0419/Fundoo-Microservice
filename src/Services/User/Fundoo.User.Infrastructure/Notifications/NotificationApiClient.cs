using System.Net.Http.Json;
using Fundoo.User.Application.Interfaces;

namespace Fundoo.User.Infrastructure.Notifications;

public class NotificationApiClient : INotificationClient
{
    private readonly HttpClient _httpClient;

    public NotificationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            To = to,
            Subject = subject,
            Body = body
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "api/notifications/email",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}