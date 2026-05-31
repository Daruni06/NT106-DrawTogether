using DrawTogether.Shared.Models;
using MySqlConnector;

namespace DrawTogether.Server.Data;

public sealed class DrawHistoryRepository
{
    private readonly MySqlDatabase _database;

    public DrawHistoryRepository(MySqlDatabase database)
    {
        _database = database;
    }

    public async Task<DrawAction> SaveAsync(
        string roomId,
        long userId,
        string actionType,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO draw_history (room_id, user_id, action_type, payload_json)
            VALUES (@room_id, @user_id, @action_type, CAST(@payload_json AS JSON));

            SELECT LAST_INSERT_ID();
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@action_type", actionType);
        command.Parameters.AddWithValue("@payload_json", payloadJson);

        var id = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));

        return new DrawAction
        {
            Id = id,
            RoomId = roomId,
            UserId = userId,
            ActionType = actionType,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<DrawAction>> GetByRoomIdAsync(
        string roomId,
        long afterId = 0,
        int limit = 5000,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, room_id, user_id, action_type, JSON_UNQUOTE(JSON_EXTRACT(payload_json, '$')) AS payload_json, created_at
            FROM draw_history
            WHERE room_id = @room_id
              AND id > @after_id
            ORDER BY id ASC
            LIMIT @limit;
            """;

        var actions = new List<DrawAction>();

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);
        command.Parameters.AddWithValue("@after_id", afterId);
        command.Parameters.AddWithValue("@limit", Math.Clamp(limit, 1, 20000));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            actions.Add(new DrawAction
            {
                Id = reader.GetInt64("id"),
                RoomId = reader.GetString("room_id"),
                UserId = reader.GetInt64("user_id"),
                ActionType = reader.GetString("action_type"),
                PayloadJson = reader.GetString("payload_json"),
                CreatedAt = reader.GetDateTime("created_at")
            });
        }

        return actions;
    }

    public async Task ClearRoomHistoryAsync(string roomId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM draw_history WHERE room_id = @room_id;";

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
