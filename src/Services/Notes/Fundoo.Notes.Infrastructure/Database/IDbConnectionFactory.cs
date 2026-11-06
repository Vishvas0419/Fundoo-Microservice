using Npgsql;

namespace Fundoo.Notes.Infrastructure.Database;

public interface IDbConnectionFactory
{
    NpgsqlConnection CreateConnection();
}