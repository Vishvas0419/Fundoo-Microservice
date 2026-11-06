
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Fundoo.Notes.Infrastructure.Database;
using Npgsql;

namespace Fundoo.Notes.Infrastructure.Repositories;

public class LabelRepository : ILabelRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LabelRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Label>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT id, user_id, name
            FROM labels
            WHERE user_id = @userId
            ORDER BY name;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        var labels = new List<Label>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            labels.Add(MapLabel(reader));
        }

        return labels;
    }

    public async Task<Label?> GetByIdAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT id, user_id, name
            FROM labels
            WHERE id = @labelId AND user_id = @userId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("labelId", labelId);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? MapLabel(reader)
            : null;
    }

    public async Task AddAsync(
        Label label,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO labels (id, user_id, name)
            VALUES (@id, @userId, @name);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", label.Id);
        command.Parameters.AddWithValue("userId", label.UserId);
        command.Parameters.AddWithValue("name", label.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Label label,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE labels
            SET name = @name
            WHERE id = @id AND user_id = @userId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("name", label.Name);
        command.Parameters.AddWithValue("id", label.Id);
        command.Parameters.AddWithValue("userId", label.UserId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            DELETE FROM labels
            WHERE id = @labelId AND user_id = @userId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("labelId", labelId);
        command.Parameters.AddWithValue("userId", userId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static Label MapLabel(NpgsqlDataReader reader)
    {
        return new Label
        {
            Id = reader.GetGuid(0),
            UserId = reader.GetGuid(1),
            Name = reader.GetString(2)
        };
    }
}
