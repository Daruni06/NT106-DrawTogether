using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrawTogether.Shared.Messages;

public sealed class Message
{
    [JsonConverter(typeof(MessageTypeJsonConverter))]
    public MessageType Type { get; set; }

    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    public string? Token { get; set; }

    public string? RoomId { get; set; }

    public string? SenderId { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public JsonElement Payload { get; set; } = MessageSerializer.EmptyPayload;

    public static Message Create(
        MessageType type,
        object? payload = null,
        string? token = null,
        string? roomId = null,
        string? senderId = null,
        string? requestId = null)
    {
        return new Message
        {
            Type = type,
            RequestId = string.IsNullOrWhiteSpace(requestId) ? Guid.NewGuid().ToString() : requestId,
            Token = token,
            RoomId = roomId,
            SenderId = senderId,
            Timestamp = DateTimeOffset.UtcNow,
            Payload = MessageSerializer.ToJsonElement(payload ?? new { })
        };
    }

    public static Message CreateResponse(
        Message request,
        MessageType responseType,
        bool success,
        string message,
        object? data = null,
        string? token = null,
        string? senderId = "server")
    {
        return Create(
            responseType,
            new ResponsePayload(success, message, MessageSerializer.ToJsonElement(data ?? new { })),
            token,
            request.RoomId,
            senderId,
            request.RequestId);
    }

    public static Message CreateError(
        Message? request,
        MessageErrorCode code,
        string message,
        string? senderId = "server")
    {
        return Create(
            MessageType.Error,
            new ErrorPayload(code, message),
            roomId: request?.RoomId,
            senderId: senderId,
            requestId: request?.RequestId);
    }

    public T? GetPayload<T>()
    {
        return Payload.Deserialize<T>(MessageSerializer.JsonOptions);
    }
}

public sealed record ResponsePayload(
    bool Success,
    string Message,
    JsonElement Data);

public sealed record ErrorPayload(
    [property: JsonConverter(typeof(MessageErrorCodeJsonConverter))]
    MessageErrorCode Code,
    string Message);