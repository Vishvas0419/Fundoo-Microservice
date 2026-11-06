using Microsoft.Extensions.Configuration;
using Npgsql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Fundoo.Notes.Infrastructure.Database;

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("NotesDb")
            ?? throw new InvalidOperationException(
                "Connection string 'NotesDb' is not configured.");
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}

//How it works:
//1. ASP.NET Core reads ConnectionStrings:NotesDb from User Secrets.
//2. The factory stores the connection string.
//3. Whenever a repository needs database access, it calls CreateConnection().
//4. The repository opens and disposes the connection when its database operation finishes.