using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Fundoo.Notes.Infrastructure.Database;
using Npgsql;
//using NpgsqlTypes;

namespace Fundoo.Notes.Infrastructure.Repositories;

public class NoteRepository : INoteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NoteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Note>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, user_id, title, content, color,
                   is_pinned, is_archived, is_trashed,
                   created_at_utc, updated_at_utc
            FROM notes
            WHERE user_id = @userId
            ORDER BY updated_at_utc DESC;
            """;

        var notes = new List<Note>();

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            notes.Add(MapNote(reader));
        }

        return notes;
    }

    public async Task<Note?> GetByIdAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, user_id, title, content, color,
                   is_pinned, is_archived, is_trashed,
                   created_at_utc, updated_at_utc
            FROM notes
            WHERE id = @noteId AND user_id = @userId;
            """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("noteId", noteId);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapNote(reader);
    }

    public async Task AddAsync(
        Note note,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO notes
            (
                id, user_id, title, content, color,
                is_pinned, is_archived, is_trashed,
                created_at_utc, updated_at_utc
            )
            VALUES
            (
                @id, @userId, @title, @content, @color,
                @isPinned, @isArchived, @isTrashed,
                @createdAtUtc, @updatedAtUtc
            );
            """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", note.Id);
        command.Parameters.AddWithValue("userId", note.UserId);
        command.Parameters.AddWithValue("title", note.Title);
        command.Parameters.AddWithValue("content", note.Content);

        command.Parameters.AddWithValue(
            "color",
            (object?)note.Color ?? DBNull.Value);

        command.Parameters.AddWithValue("isPinned", note.IsPinned);
        command.Parameters.AddWithValue("isArchived", note.IsArchived);
        command.Parameters.AddWithValue("isTrashed", note.IsTrashed);
        command.Parameters.AddWithValue(
            "createdAtUtc", note.CreatedAtUtc);
        command.Parameters.AddWithValue(
            "updatedAtUtc", note.UpdatedAtUtc);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Note note,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE notes
            SET title = @title,
                content = @content,
                color = @color,
                is_pinned = @isPinned,
                is_archived = @isArchived,
                is_trashed = @isTrashed,
                updated_at_utc = @updatedAtUtc
            WHERE id = @id AND user_id = @userId;
            """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", note.Id);
        command.Parameters.AddWithValue("userId", note.UserId);
        command.Parameters.AddWithValue("title", note.Title);
        command.Parameters.AddWithValue("content", note.Content);

        command.Parameters.AddWithValue(
            "color",
            (object?)note.Color ?? DBNull.Value);

        command.Parameters.AddWithValue("isPinned", note.IsPinned);
        command.Parameters.AddWithValue("isArchived", note.IsArchived);
        command.Parameters.AddWithValue("isTrashed", note.IsTrashed);
        command.Parameters.AddWithValue(
            "updatedAtUtc", note.UpdatedAtUtc);

        int rowsAffected =
            await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM notes
            WHERE id = @noteId AND user_id = @userId AND is_trashed = TRUE;
            """;

        await using var connection =
            _connectionFactory.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("noteId", noteId);
        command.Parameters.AddWithValue("userId", userId);

        int rowsAffected =
            await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    private static Note MapNote(NpgsqlDataReader reader)
    {
        return new Note
        {
            Id = reader.GetGuid(0),
            UserId = reader.GetGuid(1),
            Title = reader.GetString(2),
            Content = reader.GetString(3),
            Color = reader.IsDBNull(4)
                ? null
                : reader.GetString(4),
            IsPinned = reader.GetBoolean(5),
            IsArchived = reader.GetBoolean(6),
            IsTrashed = reader.GetBoolean(7),
            CreatedAtUtc = reader.GetDateTime(8),
            UpdatedAtUtc = reader.GetDateTime(9)
        };
    }

    public async Task<bool> SetPinnedAsync(
    Guid noteId,
    Guid userId,
    bool isPinned,
    CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
        UPDATE notes
        SET is_pinned = @isPinned,
            updated_at_utc = @updatedAtUtc
        WHERE id = @noteId
          AND user_id = @userId
          AND is_trashed = FALSE;
        """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("isPinned", isPinned);
        command.Parameters.AddWithValue("updatedAtUtc", DateTime.UtcNow);
        command.Parameters.AddWithValue("noteId", noteId);
        command.Parameters.AddWithValue("userId", userId);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }


    public async Task<bool> SetArchivedAsync(
    Guid noteId,
    Guid userId,
    bool isArchived,
    CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
        UPDATE notes
        SET is_archived = @isArchived,
            updated_at_utc = @updatedAtUtc
        WHERE id = @noteId
          AND user_id = @userId
          AND is_trashed = FALSE;
        """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("isArchived", isArchived);
        command.Parameters.AddWithValue("updatedAtUtc", DateTime.UtcNow);
        command.Parameters.AddWithValue("noteId", noteId);
        command.Parameters.AddWithValue("userId", userId);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<bool> SetTrashedAsync(
    Guid noteId,
    Guid userId,
    bool isTrashed,
    CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
        UPDATE notes
        SET is_trashed = @isTrashed,
            is_archived = CASE
                WHEN @isTrashed THEN FALSE
                ELSE is_archived
            END,
            updated_at_utc = @updatedAtUtc
        WHERE id = @noteId
          AND user_id = @userId;
        """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("isTrashed", isTrashed);
        command.Parameters.AddWithValue("updatedAtUtc", DateTime.UtcNow);
        command.Parameters.AddWithValue("noteId", noteId);
        command.Parameters.AddWithValue("userId", userId);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }
    public async Task<IReadOnlyList<Note>> GetByCategoryAsync(
    Guid userId,
    string category,
    CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        string sql = category.ToLowerInvariant() switch
        {
            "active" => """
            SELECT id, user_id, title, content, color,
                   is_pinned, is_archived, is_trashed,
                   created_at_utc, updated_at_utc
            FROM notes
            WHERE user_id = @userId
              AND is_archived = FALSE
              AND is_trashed = FALSE
            ORDER BY is_pinned DESC, updated_at_utc DESC;
            """,

            "archived" => """
            SELECT id, user_id, title, content, color,
                   is_pinned, is_archived, is_trashed,
                   created_at_utc, updated_at_utc
            FROM notes
            WHERE user_id = @userId
              AND is_archived = TRUE
              AND is_trashed = FALSE
            ORDER BY updated_at_utc DESC;
            """,

            "trashed" => """
            SELECT id, user_id, title, content, color,
                   is_pinned, is_archived, is_trashed,
                   created_at_utc, updated_at_utc
            FROM notes
            WHERE user_id = @userId
              AND is_trashed = TRUE
            ORDER BY updated_at_utc DESC;
            """,

            _ => throw new ArgumentException(
                "Category must be active, archived, or trashed.")
        };

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        var notes = new List<Note>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            notes.Add(MapNote(reader));
        }

        return notes;
    } //This reuses the existing MapNote(reader) method in your repository. The SQL is selected from fixed strings, and the user ID is passed as a SQL parameter.


}
