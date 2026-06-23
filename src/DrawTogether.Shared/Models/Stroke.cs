// Model net ve/shape gui giua client va server.
// Chua toa do, mau, do day net, tool va userId/roomId.
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DrawTogether.Shared.Models;

public enum DrawingToolType
{
    Pen,
    Eraser,
    Line,
    Rectangle,
    Ellipse
}

public sealed class Stroke
{
    public string StrokeId { get; set; } = Guid.NewGuid().ToString();

    public string? RoomId { get; set; }

    public string? UserId { get; set; }

    [JsonConverter(typeof(DrawingToolTypeJsonConverter))]
    public DrawingToolType Tool { get; set; } = DrawingToolType.Pen;

    public string Color { get; set; } = "#000000";

    public int Thickness { get; set; } = 3;

    public List<CanvasPoint> Points { get; set; } = new();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsCompleted { get; set; }

    public Stroke Clone()
    {
        return new Stroke
        {
            StrokeId = StrokeId,
            RoomId = RoomId,
            UserId = UserId,
            Tool = Tool,
            Color = Color,
            Thickness = Thickness,
            CreatedAt = CreatedAt,
            IsCompleted = IsCompleted,
            Points = Points.Select(point => new CanvasPoint(point.X, point.Y)).ToList()
        };
    }
}

public sealed record CanvasPoint(float X, float Y);

public sealed class DrawingToolTypeJsonConverter : JsonConverter<DrawingToolType>
{
    private static readonly IReadOnlyDictionary<DrawingToolType, string> TypeToWireName =
        new Dictionary<DrawingToolType, string>
        {
            [DrawingToolType.Pen] = "PEN",
            [DrawingToolType.Eraser] = "ERASER",
            [DrawingToolType.Line] = "LINE",
            [DrawingToolType.Rectangle] = "RECTANGLE",
            [DrawingToolType.Ellipse] = "ELLIPSE"
        };

    private static readonly IReadOnlyDictionary<string, DrawingToolType> WireNameToType =
        TypeToWireName.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public override DrawingToolType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var wireName = reader.GetString();

        if (wireName is not null && WireNameToType.TryGetValue(wireName, out var toolType))
        {
            return toolType;
        }

        throw new JsonException($"Unknown drawing tool type: {wireName}");
    }

    public override void Write(Utf8JsonWriter writer, DrawingToolType value, JsonSerializerOptions options)
    {
        if (!TypeToWireName.TryGetValue(value, out var wireName))
        {
            throw new JsonException($"Unknown drawing tool type: {value}");
        }

        writer.WriteStringValue(wireName);
    }
}