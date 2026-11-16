using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using System.Data.Common;

namespace Fundoo.Notes.Application.Services;

public class NoteService : INoteService
{
    private readonly INoteRepository _noteRepository;
    private readonly INoteCache _noteCache;
    public NoteService(INoteRepository noteRepository,INoteCache noteCache)
    {
        _noteRepository = noteRepository;
        _noteCache = noteCache;
    }

    public Task<IReadOnlyList<Note>> GetByCategoryAsync(
    Guid userId,
    string category,
    CancellationToken cancellationToken = default)
    {
        return _noteRepository.GetByCategoryAsync(
            userId, category, cancellationToken);
    }

    public Task<IReadOnlyList<Note>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _noteRepository.GetAllAsync(
            userId, cancellationToken);
    }

    //public Task<Note?> GetByIdAsync(
    //    Guid noteId,
    //    Guid userId,
    //    CancellationToken cancellationToken = default)
    //{
    //    return _noteRepository.GetByIdAsync(
    //        noteId, userId, cancellationToken);
    //}
    public async Task<Note?> GetByIdAsync(
    Guid noteId,
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        var cachedNote = await _noteCache.GetAsync(
            userId,
            noteId,
            cancellationToken);
        //cache hit
        if (cachedNote is not null)
        {
            return cachedNote;
        }
        //cache miss will go to PG db
        var note = await _noteRepository.GetByIdAsync(
            noteId,
            userId,
            cancellationToken);

        //if cache miss add to redis cache DB first, then return the note
        if (note is not null)
        {
            await _noteCache.SetAsync(note, cancellationToken);
        }

        return note;
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
        var note = await _noteRepository.GetByIdAsync(
            noteId,
            userId,
            cancellationToken);

        if (note is null)
        {
            return false;
        }

        note.Title = title.Trim();
        note.Content = content ?? "";
        note.Color = string.IsNullOrWhiteSpace(color)
            ? null
            : color.Trim();
        note.UpdatedAtUtc = DateTime.UtcNow;

        //pg updation 
        var updated = await _noteRepository.UpdateAsync(
            note,
            cancellationToken);

        if (updated)
        {
            await _noteCache.RemoveAsync( //cache invalidation, removing the stale data
                userId,
                noteId,
                cancellationToken);
        }

        return updated;
    }

    public async Task<bool> DeleteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _noteRepository.DeleteAsync(
            noteId, userId, cancellationToken);
        if(deleted)
        {
            await _noteCache.RemoveAsync(userId, noteId, cancellationToken);
        }
        return deleted;
    }

    public async Task<bool> SetPinnedAsync(
    Guid noteId,
    Guid userId,
    bool isPinned,
    CancellationToken cancellationToken = default)
    {
         var updated = await _noteRepository.SetPinnedAsync(
            noteId, userId, isPinned, cancellationToken);

        if(updated)
        {
            await _noteCache.RemoveAsync(userId, noteId, cancellationToken);
        }
        return updated;
    }
    public async Task<bool> SetArchivedAsync(
    Guid noteId,
    Guid userId,
    bool isArchived,
    CancellationToken cancellationToken = default)
    {
        var archived =  await _noteRepository.SetArchivedAsync(
            noteId, userId, isArchived, cancellationToken);

        if(archived)
        {
            await _noteCache.RemoveAsync(userId, noteId, cancellationToken);
        }
        return archived;
    }

    public async Task<bool> SetTrashedAsync(
    Guid noteId,
    Guid userId,
    bool isTrashed,
    CancellationToken cancellationToken = default)
    {
        var trashed = await _noteRepository.SetTrashedAsync(
            noteId, userId, isTrashed, cancellationToken);

        if(trashed)
        {
            await _noteCache.RemoveAsync(userId, noteId, cancellationToken);
        }
        return trashed;
    }

    
}
