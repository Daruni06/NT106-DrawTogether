
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrawTogether.Shared.Messages;

public enum MessageType
{
    Error,

    SignupRequest,
    SignupResponse,
    LoginRequest,
    LoginResponse,
    LogoutRequest,
    LogoutResponse,

    CreateRoomRequest,
    CreateRoomResponse,
    JoinRoomRequest,
    JoinRoomResponse,
    LeaveRoomRequest,
    LeaveRoomResponse,
    ListRoomsRequest,
    ListRoomsResponse,
    UserJoined,
    UserLeft,

    DrawStroke,
    DrawShape,
    UndoRequest,
    UndoEvent,
    ClearCanvasRequest,
    ClearCanvasEvent,
    CanvasSync,

    ChatSend,
    ChatMessage,

    ServerRegister,
    ServerLoadUpdate,
    ServerHeartbeat,
    RequestServer,
    ServerAssigned
}

public enum MessageErrorCode
{
    InvalidJson,
    UnknownMessageType,
    InvalidPayload,
    InvalidToken,
    PermissionDenied,
    RoomNotFound,
    ServerUnavailable,
    InternalError
}

public sealed class MessageTypeJsonConverter : JsonConverter<MessageType>
{
    private static readonly IReadOnlyDictionary<MessageType, string> TypeToWireName =
        new Dictionary<MessageType, string>
        {
            [MessageType.Error] = "ERROR",
            [MessageType.SignupRequest] = "SIGNUP_REQUEST",
            [MessageType.SignupResponse] = "SIGNUP_RESPONSE",
            [MessageType.LoginRequest] = "LOGIN_REQUEST",
            [MessageType.LoginResponse] = "LOGIN_RESPONSE",
            [MessageType.LogoutRequest] = "LOGOUT_REQUEST",
            [MessageType.LogoutResponse] = "LOGOUT_RESPONSE",
            [MessageType.CreateRoomRequest] = "CREATE_ROOM_REQUEST",
            [MessageType.CreateRoomResponse] = "CREATE_ROOM_RESPONSE",
            [MessageType.JoinRoomRequest] = "JOIN_ROOM_REQUEST",
            [MessageType.JoinRoomResponse] = "JOIN_ROOM_RESPONSE",
            [MessageType.LeaveRoomRequest] = "LEAVE_ROOM_REQUEST",
            [MessageType.LeaveRoomResponse] = "LEAVE_ROOM_RESPONSE",
            [MessageType.ListRoomsRequest] = "LIST_ROOMS_REQUEST",
            [MessageType.ListRoomsResponse] = "LIST_ROOMS_RESPONSE",
            [MessageType.UserJoined] = "USER_JOINED",
            [MessageType.UserLeft] = "USER_LEFT",
            [MessageType.DrawStroke] = "DRAW_STROKE",
            [MessageType.DrawShape] = "DRAW_SHAPE",
            [MessageType.UndoRequest] = "UNDO_REQUEST",
            [MessageType.UndoEvent] = "UNDO_EVENT",
            [MessageType.ClearCanvasRequest] = "CLEAR_CANVAS_REQUEST",
            [MessageType.ClearCanvasEvent] = "CLEAR_CANVAS_EVENT",
            [MessageType.CanvasSync] = "CANVAS_SYNC",
            [MessageType.ChatSend] = "CHAT_SEND",
            [MessageType.ChatMessage] = "CHAT_MESSAGE",
            [MessageType.ServerRegister] = "SERVER_REGISTER",
            [MessageType.ServerLoadUpdate] = "SERVER_LOAD_UPDATE",
            [MessageType.ServerHeartbeat] = "SERVER_HEARTBEAT",
            [MessageType.RequestServer] = "REQUEST_SERVER",
            [MessageType.ServerAssigned] = "SERVER_ASSIGNED"
        };

    private static readonly IReadOnlyDictionary<string, MessageType> WireNameToType =
        TypeToWireName.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public override MessageType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var wireName = reader.GetString();

        if (wireName is not null && WireNameToType.TryGetValue(wireName, out var messageType))
        {
            return messageType;
        }

        throw new JsonException($"Unknown message type: {wireName}");
    }

    public override void Write(Utf8JsonWriter writer, MessageType value, JsonSerializerOptions options)
    {
        if (!TypeToWireName.TryGetValue(value, out var wireName))
        {
            throw new JsonException($"Unknown message type: {value}");
        }

        writer.WriteStringValue(wireName);
    }

    public static string ToWireName(MessageType value)
    {
        return TypeToWireName.TryGetValue(value, out var wireName)
            ? wireName
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown message type.");
    }
}

public sealed class MessageErrorCodeJsonConverter : JsonConverter<MessageErrorCode>
{
    private static readonly IReadOnlyDictionary<MessageErrorCode, string> CodeToWireName =
        new Dictionary<MessageErrorCode, string>
        {
            [MessageErrorCode.InvalidJson] = "INVALID_JSON",
            [MessageErrorCode.UnknownMessageType] = "UNKNOWN_MESSAGE_TYPE",
            [MessageErrorCode.InvalidPayload] = "INVALID_PAYLOAD",
            [MessageErrorCode.InvalidToken] = "INVALID_TOKEN",
            [MessageErrorCode.PermissionDenied] = "PERMISSION_DENIED",
            [MessageErrorCode.RoomNotFound] = "ROOM_NOT_FOUND",
            [MessageErrorCode.ServerUnavailable] = "SERVER_UNAVAILABLE",
            [MessageErrorCode.InternalError] = "INTERNAL_ERROR"
        };

    private static readonly IReadOnlyDictionary<string, MessageErrorCode> WireNameToCode =
        CodeToWireName.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public override MessageErrorCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var wireName = reader.GetString();

        if (wireName is not null && WireNameToCode.TryGetValue(wireName, out var errorCode))
        {
            return errorCode;
        }

        throw new JsonException($"Unknown message error code: {wireName}");
    }

    public override void Write(Utf8JsonWriter writer, MessageErrorCode value, JsonSerializerOptions options)
    {
        if (!CodeToWireName.TryGetValue(value, out var wireName))
        {
            throw new JsonException($"Unknown message error code: {value}");
        }

        writer.WriteStringValue(wireName);
    }
}

