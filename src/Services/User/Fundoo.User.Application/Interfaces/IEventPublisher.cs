namespace Fundoo.User.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        string topic,
        TEvent message,
        CancellationToken cancellationToken = default);
}