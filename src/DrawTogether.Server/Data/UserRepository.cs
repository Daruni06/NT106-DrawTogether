using DrawTogether.Shared.Models;
using MySqlConnector;

namespace DrawTogether.Server.Data;

public sealed class UserRepository
{
    private readonly MySqlDatabase _database;

    public UserRepository(MySqlDatabase database)
    {
        _database = database;
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM users WHERE username = @username;";

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@username", username);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, username, display_name, created_at
            FROM users
            WHERE username = @username
            LIMIT 1;
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapUser(reader);
    }

    public async Task<(User User, string PasswordHash)?> GetAuthRecordByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, username, display_name, password_hash, created_at
            FROM users
            WHERE username = @username
            LIMIT 1;
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var user = MapUser(reader);
        var passwordHash = reader.GetString("password_hash");

        return (user, passwordHash);
    }

    public async Task<User> CreateAsync(
        string username,
        string passwordHash,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO users (username, password_hash, display_name)
            VALUES (@username, @password_hash, @display_name);

            SELECT LAST_INSERT_ID();
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password_hash", passwordHash);
        command.Parameters.AddWithValue("@display_name", displayName);

        var newId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));

        return new User
        {
            Id = newId,
            Username = username,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static User MapUser(MySqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt64("id"),
            Username = reader.GetString("username"),
            DisplayName = reader.GetString("display_name"),
            CreatedAt = reader.GetDateTime("created_at")
        };
    }
}
