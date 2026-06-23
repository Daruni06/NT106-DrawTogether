using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DrawTogether.Shared.Messages;

public static class MessageSerializer
{
    public const int HeaderSize = 4;
    public const int MaxMessageSize = 8 * 1024 * 1024;

    public static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static JsonElement EmptyPayload { get; } = ToJsonElement(new { });

    public static string Serialize(Message message, bool indented = false)
    {
        Validate(message);

        if (!indented)
        {
            return JsonSerializer.Serialize(message, JsonOptions);
        }

        var options = CreateJsonOptions();
        options.WriteIndented = true;
        return JsonSerializer.Serialize(message, options);
    }

    public static Message Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new MessageFormatException(MessageErrorCode.InvalidJson, "JSON message is empty.");
        }

        try
        {
            var message = JsonSerializer.Deserialize<Message>(json, JsonOptions);

            if (message is null)
            {
                throw new MessageFormatException(MessageErrorCode.InvalidJson, "JSON message is null.");
            }

            Validate(message);
            return message;
        }
        catch (MessageFormatException)
        {
            throw;
        }
        catch (JsonException ex) when (ex.Message.Contains("Unknown message type", StringComparison.OrdinalIgnoreCase))
        {
            throw new MessageFormatException(MessageErrorCode.UnknownMessageType, ex.Message, ex);
        }
        catch (JsonException ex)
        {
            throw new MessageFormatException(MessageErrorCode.InvalidJson, "JSON message is invalid.", ex);
        }
    }

    public static byte[] EncodeFrame(Message message)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(Serialize(message));

        if (jsonBytes.Length > MaxMessageSize)
        {
            throw new MessageFormatException(
                MessageErrorCode.InvalidPayload,
                $"Message is too large: {jsonBytes.Length} bytes.");
        }

        var frame = new byte[HeaderSize + jsonBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, HeaderSize), jsonBytes.Length);
        Buffer.BlockCopy(jsonBytes, 0, frame, HeaderSize, jsonBytes.Length);
        return frame;
    }

    public static Message DecodeFrame(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < HeaderSize)
        {
            throw new MessageFormatException(MessageErrorCode.InvalidPayload, "Frame is missing length header.");
        }

        var length = BinaryPrimitives.ReadInt32BigEndian(frame[..HeaderSize]);

        if (length < 0 || length > MaxMessageSize)
        {
            throw new MessageFormatException(MessageErrorCode.InvalidPayload, $"Invalid frame length: {length}.");
        }

        if (frame.Length - HeaderSize != length)
        {
            throw new MessageFormatException(
                MessageErrorCode.InvalidPayload,
                $"Frame length mismatch. Header={length}, body={frame.Length - HeaderSize}.");
        }

        var json = Encoding.UTF8.GetString(frame[HeaderSize..]);
        return Deserialize(json);
    }

    public static async Task WriteAsync(
        Stream stream,
        Message message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var frame = EncodeFrame(message);
        await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Message> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = await ReadExactlyAsync(stream, HeaderSize, cancellationToken).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32BigEndian(header);

        if (length < 0 || length > MaxMessageSize)
        {
            throw new MessageFormatException(MessageErrorCode.InvalidPayload, $"Invalid frame length: {length}.");
        }

        var body = await ReadExactlyAsync(stream, length, cancellationToken).ConfigureAwait(false);
        var json = Encoding.UTF8.GetString(body);
        return Deserialize(json);
    }

    public static JsonElement ToJsonElement(object payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.Clone();
    }

    public static void Validate(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.RequestId))
        {
            throw new MessageFormatException(MessageErrorCode.InvalidPayload, "Message requestId is required.");
        }

        if (message.Timestamp == default)
        {
            throw new MessageFormatException(MessageErrorCode.InvalidPayload, "Message timestamp is required.");
        }

        if (message.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new MessageFormatException(MessageErrorCode.InvalidPayload, "Message payload is required.");
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = false
        };

        options.Converters.Add(new MessageTypeJsonConverter());
        options.Converters.Add(new MessageErrorCodeJsonConverter());
        return options;
    }

    private static async Task<byte[]> ReadExactlyAsync(
        Stream stream,
        int byteCount,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[byteCount];
        var offset = 0;

        while (offset < byteCount)
        {
            var read = await stream
                .ReadAsync(buffer.AsMemory(offset, byteCount - offset), cancellationToken)
                .ConfigureAwait(false);

            if (read == 0)
            {
                throw new EndOfStreamException("Socket stream closed before full message frame was read.");
            }

            offset += read;
        }

        return buffer;
    }
}

public sealed class MessageFormatException : Exception
{
    public MessageFormatException(
        MessageErrorCode code,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public MessageErrorCode Code { get; }
}