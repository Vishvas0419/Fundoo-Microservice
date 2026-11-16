using System.Text.Json;
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Fundoo.Notes.Infrastructure.Caching;

public sealed class RedisNoteCache : INoteCache
{
    private static readonly TimeSpan CacheDuration =
        TimeSpan.FromMinutes(5);

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisNoteCache> _logger;

    public RedisNoteCache(
        IDistributedCache cache,
        ILogger<RedisNoteCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    //retrieve note from the redis
    public async Task<Note?> GetAsync(
        Guid userId,
        Guid noteId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(
                CreateKey(userId, noteId),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var cachedNote = JsonSerializer.Deserialize<CachedNote>(json);

            return cachedNote?.ToNote();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Redis is an optional cache. The service can still read PostgreSQL.
            _logger.LogWarning(
                exception,
                "Redis note cache read failed. Falling back to PostgreSQL.");

            return null;
        }
    }

    public async Task SetAsync(
        Note note,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Cache scalar note fields only. Note has navigation properties,
            // which should not be serialized as part of this cache entry.
            var cachedNote = CachedNote.FromNote(note);
            var json = JsonSerializer.Serialize(cachedNote);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            };

            await _cache.SetStringAsync(
                CreateKey(note.UserId, note.Id),
                json,
                options,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis note cache write failed.");
        }
    }

    public async Task RemoveAsync(
        Guid userId,
        Guid noteId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(
                CreateKey(userId, noteId),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis note cache invalidation failed.");
        }
    }

    private static string CreateKey(Guid userId, Guid noteId)
    {
        return $"notes:{userId}:item:{noteId}";
    }

    private sealed record CachedNote(
        Guid Id,
        Guid UserId,
        string Title,
        string Content,
        string? Color,
        bool IsPinned,
        bool IsArchived,
        bool IsTrashed,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc)
    {
        public static CachedNote FromNote(Note note)
        {
            return new CachedNote(
                note.Id,
                note.UserId,
                note.Title,
                note.Content,
                note.Color,
                note.IsPinned,
                note.IsArchived,
                note.IsTrashed,
                note.CreatedAtUtc,
                note.UpdatedAtUtc);
        }

        public Note ToNote()
        {
            return new Note
            {
                Id = Id,
                UserId = UserId,
                Title = Title,
                Content = Content,
                Color = Color,
                IsPinned = IsPinned,
                IsArchived = IsArchived,
                IsTrashed = IsTrashed,
                CreatedAtUtc = CreatedAtUtc,
                UpdatedAtUtc = UpdatedAtUtc
            };
        }
    }
}