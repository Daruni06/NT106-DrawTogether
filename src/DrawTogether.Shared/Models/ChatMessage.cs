// Model tin nhan chat trong phong.
// Chua roomId, senderId, senderName, content va timestamp.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrawTogether.Shared.Models;

public enum ChatContentType
{
    Text,
    File,
    Image
}

public sealed class ChatMessage
{
    public long Id { get; set; }

    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    public string? RoomId { get; set; }

    public string? SenderId { get; set; }

    public long UserId
    {
        get => long.TryParse(SenderId, out var userId) ? userId : 0;
        set => SenderId = value == 0 ? SenderId : value.ToString();
    }

    public string Username { get; set; } = string.Empty;

    public string SenderName { get; set; } = "Unknown";

    public string DisplayName
    {
        get => SenderName;
        set => SenderName = string.IsNullOrWhiteSpace(value) ? SenderName : value;
    }

    public string Content { get; set; } = string.Empty;

    public string Message
    {
        get => Content;
        set => Content = value ?? string.Empty;
    }

    [JsonConverter(typeof(ChatContentTypeJsonConverter))]
    public ChatContentType ContentType { get; set; } = ChatContentType.Text;

    public ChatAttachment? Attachment { get; set; }

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTime CreatedAt
    {
        get => SentAt.UtcDateTime;
        set => SentAt = new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    public static ChatMessage CreateText(string? roomId, string? senderId, string senderName, string content)
    {
        return new ChatMessage
        {
            RoomId = roomId,
            SenderId = senderId,
            Username = senderId ?? string.Empty,
            SenderName = string.IsNullOrWhiteSpace(senderName) ? "Unknown" : senderName,
            Content = content.Trim(),
            ContentType = ChatContentType.Text,
            SentAt = DateTimeOffset.UtcNow
        };
    }

    public static ChatMessage CreateFile(
        string? roomId,
        string? senderId,
        string senderName,
        ChatAttachment attachment,
        string? caption = null)
    {
        return new ChatMessage
        {
            RoomId = roomId,
            SenderId = senderId,
            Username = senderId ?? string.Empty,
            SenderName = string.IsNullOrWhiteSpace(senderName) ? "Unknown" : senderName,
            Content = caption?.Trim() ?? string.Empty,
            ContentType = attachment.IsImage ? ChatContentType.Image : ChatContentType.File,
            Attachment = attachment,
            SentAt = DateTimeOffset.UtcNow
        };
    }
}

public sealed class ChatAttachment
{
    public string AttachmentId { get; set; } = Guid.NewGuid().ToString();

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/octet-stream";

    public long Size { get; set; }

    public string Base64Data { get; set; } = string.Empty;

    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    public static ChatAttachment FromBytes(string fileName, string contentType, byte[] bytes)
    {
        return new ChatAttachment
        {
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Size = bytes.LongLength,
            Base64Data = Convert.ToBase64String(bytes)
        };
    }

    public byte[] GetBytes()
    {
        return Convert.FromBase64String(Base64Data);
    }
}

public sealed class ChatContentTypeJsonConverter : JsonConverter<ChatContentType>
{
    private static readonly IReadOnlyDictionary<ChatContentType, string> TypeToWireName =
        new Dictionary<ChatContentType, string>
        {
            [ChatContentType.Text] = "TEXT",
            [ChatContentType.File] = "FILE",
            [ChatContentType.Image] = "IMAGE"
        };

    private static readonly IReadOnlyDictionary<string, ChatContentType> WireNameToType =
        TypeToWireName.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public override ChatContentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var wireName = reader.GetString();

        if (wireName is not null && WireNameToType.TryGetValue(wireName, out var contentType))
        {
            return contentType;
        }

        throw new JsonException($"Unknown chat content type: {wireName}");
    }

    public override void Write(Utf8JsonWriter writer, ChatContentType value, JsonSerializerOptions options)
    {
        if (!TypeToWireName.TryGetValue(value, out var wireName))
        {
            throw new JsonException($"Unknown chat content type: {value}");
        }

        writer.WriteStringValue(wireName);
    }
}
