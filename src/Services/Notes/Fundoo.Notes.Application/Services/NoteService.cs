
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Services;

public class NoteService : INoteService
{
    private readonly INoteRepository _noteRepository;

    public NoteService(INoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    public Task<IReadOnlyList<Note>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _noteRepository.GetAllAsync(
            userId, cancellationToken);
    }

    public Task<Note?> GetByIdAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _noteRepository.GetByIdAsync(
            noteId, userId, cancellationToken);
    }

    public async Task<Note> CreateAsync(
        Guid userId,
        string title,
        string content,
        string? color,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }

        var now = DateTime.UtcNow;

        var note = new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Content = content ?? "",
            Color = string.IsNullOrWhiteSpace(color)
                ? null
                : color.Trim(),
            IsPinned = false,
            IsArchived = false,
            IsTrashed = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _noteRepository.AddAsync(note, cancellationToken);

        return note;
    }

    public async Task<bool> UpdateAsync(
        Guid noteId,
        Guid userId,
        string title,
        string content,
        string? color,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }

        var note = await _noteRepository.GetByIdAsync(
            noteId, userId, cancellationToken);

        if (note == null)
        {
            return false;
        }

        note.Title = title.Trim();
        note.Content = content ?? "";
        note.Color = string.IsNullOrWhiteSpace(color)
            ? null
            : color.Trim();
        note.UpdatedAtUtc = DateTime.UtcNow;

        return await _noteRepository.UpdateAsync(
            note, cancellationToken);
    }

    public Task<bool> DeleteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _noteRepository.DeleteAsync(
            noteId, userId, cancellationToken);
    }

    public Task<bool> SetPinnedAsync(
    Guid noteId,
    Guid userId,
    bool isPinned,
    CancellationToken cancellationToken = default)
    {
        return _noteRepository.SetPinnedAsync(
            noteId, userId, isPinned, cancellationToken);
    }
    public Task<bool> SetArchivedAsync(
    Guid noteId,
    Guid userId,
    bool isArchived,
    CancellationToken cancellationToken = default)
    {
        return _noteRepository.SetArchivedAsync(
            noteId, userId, isArchived, cancellationToken);
    }

    public Task<bool> SetTrashedAsync(
    Guid noteId,
    Guid userId,
    bool isTrashed,
    CancellationToken cancellationToken = default)
    {
        return _noteRepository.SetTrashedAsync(
            noteId, userId, isTrashed, cancellationToken);
    }

    public Task<IReadOnlyList<Note>> GetByCategoryAsync(
    Guid userId,
    string category,
    CancellationToken cancellationToken = default)
    {
        return _noteRepository.GetByCategoryAsync(
            userId, category, cancellationToken);
    }
}
