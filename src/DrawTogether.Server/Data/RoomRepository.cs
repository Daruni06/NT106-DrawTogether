using DrawTogether.Shared.Models;
using MySqlConnector;

namespace DrawTogether.Server.Data;

public sealed class RoomRepository
{
    private readonly MySqlDatabase _database;

    public RoomRepository(MySqlDatabase database)
    {
        _database = database;
    }

    public async Task<Room> CreateAsync(
        string roomName,
        long ownerUserId,
        int maxMembers,
        CancellationToken cancellationToken = default)
    {
        var roomId = Guid.NewGuid().ToString();

        const string sql = """
            INSERT INTO rooms (id, name, owner_user_id, max_members)
            VALUES (@id, @name, @owner_user_id, @max_members);
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", roomId);
        command.Parameters.AddWithValue("@name", roomName);
        command.Parameters.AddWithValue("@owner_user_id", ownerUserId);
        command.Parameters.AddWithValue("@max_members", maxMembers);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new Room
        {
            Id = roomId,
            Name = roomName,
            OwnerUserId = ownerUserId,
            MaxMembers = maxMembers,
            IsClosed = false,
            CreatedAt = DateTime.UtcNow,
            ActiveMemberCount = 0
        };
    }

    public async Task<Room?> GetByIdAsync(string roomId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.id, r.name, r.owner_user_id, r.max_members, r.is_closed, r.created_at,
                   COUNT(active_members.id) AS active_member_count
            FROM rooms r
            LEFT JOIN room_members active_members
                ON active_members.room_id = r.id AND active_members.left_at IS NULL
            WHERE r.id = @room_id
            GROUP BY r.id, r.name, r.owner_user_id, r.max_members, r.is_closed, r.created_at
            LIMIT 1;
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapRoom(reader);
    }

    public async Task<IReadOnlyList<Room>> ListOpenRoomsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.id, r.name, r.owner_user_id, r.max_members, r.is_closed, r.created_at,
                   COUNT(active_members.id) AS active_member_count
            FROM rooms r
            LEFT JOIN room_members active_members
                ON active_members.room_id = r.id AND active_members.left_at IS NULL
            WHERE r.is_closed = 0
            GROUP BY r.id, r.name, r.owner_user_id, r.max_members, r.is_closed, r.created_at
            ORDER BY r.created_at DESC;
            """;

        var rooms = new List<Room>();

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rooms.Add(MapRoom(reader));
        }

        return rooms;
    }

    public async Task CloseRoomAsync(string roomId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE rooms
            SET is_closed = 1
            WHERE id = @room_id;
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CountActiveMembersAsync(string roomId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM room_members
            WHERE room_id = @room_id AND left_at IS NULL;
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> IsActiveMemberAsync(string roomId, long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM room_members
            WHERE room_id = @room_id
              AND user_id = @user_id
              AND left_at IS NULL;
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);
        command.Parameters.AddWithValue("@user_id", userId);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task JoinAsync(string roomId, long userId, CancellationToken cancellationToken = default)
    {
        const string closeOldMembershipSql = """
            UPDATE room_members
            SET left_at = CURRENT_TIMESTAMP
            WHERE room_id = @room_id
              AND user_id = @user_id
              AND left_at IS NULL;
            """;

        const string insertSql = """
            INSERT INTO room_members (room_id, user_id)
            VALUES (@room_id, @user_id);
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var closeCommand = connection.CreateCommand())
        {
            closeCommand.Transaction = transaction;
            closeCommand.CommandText = closeOldMembershipSql;
            closeCommand.Parameters.AddWithValue("@room_id", roomId);
            closeCommand.Parameters.AddWithValue("@user_id", userId);
            await closeCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insertCommand = connection.CreateCommand())
        {
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = insertSql;
            insertCommand.Parameters.AddWithValue("@room_id", roomId);
            insertCommand.Parameters.AddWithValue("@user_id", userId);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task LeaveAsync(string roomId, long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE room_members
            SET left_at = CURRENT_TIMESTAMP
            WHERE room_id = @room_id
              AND user_id = @user_id
              AND left_at IS NULL;
            """;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);
        command.Parameters.AddWithValue("@user_id", userId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoomMember>> ListActiveMembersAsync(
        string roomId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT rm.id, rm.room_id, rm.user_id, u.username, u.display_name, rm.joined_at, rm.left_at
            FROM room_members rm
            JOIN users u ON u.id = rm.user_id
            WHERE rm.room_id = @room_id
              AND rm.left_at IS NULL
            ORDER BY rm.joined_at ASC;
            """;

        var members = new List<RoomMember>();

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@room_id", roomId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            members.Add(new RoomMember
            {
                Id = reader.GetInt64("id"),
                RoomId = reader.GetString("room_id"),
                UserId = reader.GetInt64("user_id"),
                Username = reader.GetString("username"),
                DisplayName = reader.GetString("display_name"),
                JoinedAt = reader.GetDateTime("joined_at"),
                LeftAt = reader.IsDBNull(reader.GetOrdinal("left_at"))
                    ? null
                    : reader.GetDateTime("left_at")
            });
        }

        return members;
    }

    private static Room MapRoom(MySqlDataReader reader)
    {
        return new Room
        {
            Id = reader.GetString("id"),
            Name = reader.GetString("name"),
            OwnerUserId = reader.GetInt64("owner_user_id"),
            MaxMembers = reader.GetInt32("max_members"),
            IsClosed = reader.GetBoolean("is_closed"),
            CreatedAt = reader.GetDateTime("created_at"),
            ActiveMemberCount = reader.GetInt32("active_member_count")
        };
    }
}
