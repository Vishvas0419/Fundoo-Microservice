using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces;

public interface INoteCache
{
    Task<Note?> GetAsync(
        Guid userId,
        Guid noteId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        Note note,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid userId,
        Guid noteId,
        CancellationToken cancellationToken = default);
}