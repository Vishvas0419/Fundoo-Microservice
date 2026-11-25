namespace Fundoo.Contracts.Events;

public sealed class UserRegisteredEvent
{
    public Guid EventId { get; init; }

    public string EventType { get; init; } = "UserRegistered";

    public int EventVersion { get; init; } = 1;

    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; init; }
}