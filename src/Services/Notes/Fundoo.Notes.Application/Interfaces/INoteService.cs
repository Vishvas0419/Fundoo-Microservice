
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces;

public interface INoteService
{
    Task<IReadOnlyList<Note>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Note?> GetByIdAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Note> CreateAsync(
        Guid userId,
        string title,
        string content,
        string? color,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid noteId,
        Guid userId,
        string title,
        string content,
        string? color,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<bool> SetPinnedAsync(
    Guid noteId,
    Guid userId,
    bool isPinned,
    CancellationToken cancellationToken = default);

    Task<bool> SetArchivedAsync(
    Guid noteId,
    Guid userId,
    bool isArchived,
    CancellationToken cancellationToken = default);
    Task<bool> SetTrashedAsync(
    Guid noteId,
    Guid userId,
    bool isTrashed,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> GetByCategoryAsync(
    Guid userId,
    string category,
    CancellationToken cancellationToken = default);

}

//This interface defines what the API can ask the business layer to do. It does not contain SQL or database connection logic.