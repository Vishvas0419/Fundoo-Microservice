using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Infrastructure.Database;
using Npgsql;

namespace Fundoo.Notes.Infrastructure.Repositories;

public class NoteLabelRepository : INoteLabelRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NoteLabelRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> AddLabelToNoteAsync(
        Guid noteId, Guid labelId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO note_labels (note_id, label_id)
            SELECT n.id, l.id
            FROM notes n
            CROSS JOIN labels l
            WHERE n.id = @noteId
              AND n.user_id = @userId
              AND n.is_trashed = FALSE
              AND l.id = @labelId
              AND l.user_id = @userId
            ON CONFLICT (note_id, label_id) DO NOTHING;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("noteId", noteId);
        command.Parameters.AddWithValue("labelId", labelId);
        command.Parameters.AddWithValue("userId", userId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> RemoveLabelFromNoteAsync(
        Guid noteId, Guid labelId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            DELETE FROM note_labels nl
            USING notes n, labels l
            WHERE nl.note_id = n.id
              AND nl.label_id = l.id
              AND n.id = @noteId
              AND n.user_id = @userId
              AND l.id = @labelId
              AND l.user_id = @userId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("noteId", noteId);
        command.Parameters.AddWithValue("labelId", labelId);
        command.Parameters.AddWithValue("userId", userId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}