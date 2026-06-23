// Dinh nghia cong cu ve: pen, eraser, line, rectangle, ellipse.
// Luu mau sac, do day net va cach tao stroke/shape tu thao tac chuot.
using System.Drawing;
using DrawTogether.Shared.Models;

namespace DrawTogether.Client.Drawing;

public sealed class DrawingTool
{
    public DrawingToolType ToolType { get; private set; } = DrawingToolType.Pen;

    public Color Color { get; private set; } = Color.Black;

    public int Thickness { get; private set; } = 3;

    public void SetTool(DrawingToolType toolType)
    {
        ToolType = toolType;
    }

    public void SetColor(Color color)
    {
        Color = color;
    }

    public void SetThickness(int thickness)
    {
        Thickness = Math.Clamp(thickness, 1, 50);
    }

    public Stroke BeginStroke(Point point, string? roomId, string? userId)
    {
        return new Stroke
        {
            RoomId = roomId,
            UserId = userId,
            Tool = ToolType,
            Color = ToHex(Color),
            Thickness = Thickness,
            CreatedAt = DateTimeOffset.UtcNow,
            Points = new List<CanvasPoint> { new(point.X, point.Y) }
        };
    }

    public void AddPoint(Stroke stroke, Point point)
    {
        stroke.Points.Add(new CanvasPoint(point.X, point.Y));
    }

    public void CompleteStroke(Stroke stroke, Point point)
    {
        if (stroke.Points.Count == 0 || stroke.Points[^1].X != point.X || stroke.Points[^1].Y != point.Y)
        {
            AddPoint(stroke, point);
        }

        stroke.IsCompleted = true;
    }

    public static Color FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return Color.Black;
        }

        return ColorTranslator.FromHtml(hex);
    }

    public static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}