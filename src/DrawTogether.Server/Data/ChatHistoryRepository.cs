using DrawTogether.Shared.Models;
using MySqlConnector;

namespace DrawTogether.Server.Data;

public sealed class ChatHistoryRepository
{
    private readonly MySqlDatabase _database;

    public ChatHistoryRepository(MySqlDatabase database)
    {
        _database = database;
    }

    public async Task<ChatMessage> SaveAsync(
        string roomId,
        long userId,
        string message,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO chat_history (room_id, user_id, message)
            VALUES (@room_id, @user_id, @message);

            SELECT LAST_INSERT_ID();
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@message", message);

        var id = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));

        return new ChatMessage
        {
            Id = id,
            RoomId = roomId,
            UserId = userId,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<ChatMessage>> GetByRoomIdAsync(
        string roomId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ch.id, ch.room_id, ch.user_id, u.username, u.display_name, ch.message, ch.created_at
            FROM chat_history ch
            JOIN users u ON u.id = ch.user_id
            WHERE ch.room_id = @room_id
            ORDER BY ch.id DESC
            LIMIT @limit;
            """;

        var messages = new List<ChatMessage>();

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);
        command.Parameters.AddWithValue("@limit", Math.Clamp(limit, 1, 500));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            messages.Add(new ChatMessage
            {
                Id = reader.GetInt64("id"),
                RoomId = reader.GetString("room_id"),
                UserId = reader.GetInt64("user_id"),
                Username = reader.GetString("username"),
                DisplayName = reader.GetString("display_name"),
                Message = reader.GetString("message"),
                CreatedAt = reader.GetDateTime("created_at")
            });
        }

        messages.Reverse();
        return messages;
    }
}
