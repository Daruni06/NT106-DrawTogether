// Luu trang thai canvas phia client.
// Quan ly danh sach stroke, clear, undo, import/export va rebuild canvas khi join phong.
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using DrawTogether.Shared.Models;

namespace DrawTogether.Client.Drawing;

public sealed class CanvasState : IDisposable
{
    private readonly object _sync = new();
    private readonly List<Stroke> _strokes = new();
    private readonly List<Stroke> _redo = new();
    private Image? _backgroundImage;

    public event EventHandler? Changed;

    public Color BackgroundColor { get; set; } = Color.White;

    public IReadOnlyList<Stroke> Strokes
    {
        get
        {
            lock (_sync)
            {
                return _strokes.ToList();
            }
        }
    }

    public void SetHistory(IEnumerable<Stroke> strokes)
    {
        lock (_sync)
        {
            _strokes.Clear();
            _strokes.AddRange(strokes.Select(stroke => stroke.Clone()));
            _redo.Clear();
        }

        OnChanged();
    }

    public void AddStroke(Stroke stroke)
    {
        if (stroke.Points.Count == 0)
        {
            return;
        }

        lock (_sync)
        {
            _strokes.Add(stroke.Clone());
            // new action clears redo stack
            _redo.Clear();
        }

        try
        {
            Console.WriteLine($"[CanvasState] AddStroke id={stroke.StrokeId} user={stroke.UserId} points={stroke.Points.Count} total={_strokes.Count}");
        }
        catch { }

        OnChanged();
    }

    public Stroke? UndoLast(string? userId = null)
    {
        lock (_sync)
        {
            for (var index = _strokes.Count - 1; index >= 0; index--)
            {
                if (userId is null || _strokes[index].UserId == userId)
                {
                    var removed = _strokes[index];
                    _strokes.RemoveAt(index);
                    // push into redo stack
                    _redo.Add(removed.Clone());
                    OnChanged();
                    return removed;
                }
            }
        }

        return null;
    }

    public Stroke? RedoLast(string? userId = null)
    {
        lock (_sync)
        {
            for (var index = _redo.Count - 1; index >= 0; index--)
            {
                if (userId is null || _redo[index].UserId == userId)
                {
                    var restored = _redo[index];
                    _redo.RemoveAt(index);
                    _strokes.Add(restored.Clone());
                    OnChanged();
                    return restored;
                }
            }
        }

        return null;
    }

    public void Clear()
    {
        lock (_sync)
        {
            _strokes.Clear();
            _redo.Clear();
        }

        OnChanged();
    }

    public void SetBackgroundImage(Image? image)
    {
        lock (_sync)
        {
            _backgroundImage?.Dispose();
            _backgroundImage = image is null ? null : new Bitmap(image);
        }

        OnChanged();
    }

    public void Render(Graphics graphics, Size canvasSize, Stroke? previewStroke = null)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackgroundColor);

        if (_backgroundImage is not null)
        {
            graphics.DrawImage(_backgroundImage, new Rectangle(Point.Empty, canvasSize));
        }

        List<Stroke> strokes;
        lock (_sync)
        {
            strokes = _strokes.Select(stroke => stroke.Clone()).ToList();
        }

        foreach (var stroke in strokes)
        {
            RenderStroke(graphics, stroke);
        }

        if (previewStroke is not null)
        {
            RenderStroke(graphics, previewStroke);
        }
    }

    public Bitmap ToBitmap(Size canvasSize)
    {
        var bitmap = new Bitmap(canvasSize.Width, canvasSize.Height);

        using var graphics = Graphics.FromImage(bitmap);
        Render(graphics, canvasSize);

        return bitmap;
    }

    public void SavePng(string filePath, Size canvasSize)
    {
        using var bitmap = ToBitmap(canvasSize);
        bitmap.Save(filePath, ImageFormat.Png);
    }

    public void Dispose()
    {
        _backgroundImage?.Dispose();
    }

    private void RenderStroke(Graphics graphics, Stroke stroke)
    {
        if (stroke.Points.Count == 0)
        {
            return;
        }

        var color = stroke.Tool == DrawingToolType.Eraser
            ? BackgroundColor
            : DrawingTool.FromHex(stroke.Color);

        using var pen = new Pen(color, stroke.Thickness)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        switch (stroke.Tool)
        {
            case DrawingToolType.Pen:
            case DrawingToolType.Eraser:
                RenderFreehand(graphics, pen, stroke.Points);
                break;
            case DrawingToolType.Line:
                RenderLine(graphics, pen, stroke.Points);
                break;
            case DrawingToolType.Rectangle:
                graphics.DrawRectangle(pen, GetBounds(stroke.Points));
                break;
            case DrawingToolType.Ellipse:
                graphics.DrawEllipse(pen, GetBounds(stroke.Points));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stroke.Tool), stroke.Tool, "Unsupported drawing tool.");
        }
    }

    private static void RenderFreehand(Graphics graphics, Pen pen, IReadOnlyList<CanvasPoint> points)
    {
        if (points.Count == 1)
        {
            graphics.DrawEllipse(
                pen,
                points[0].X - pen.Width / 2,
                points[0].Y - pen.Width / 2,
                pen.Width,
                pen.Width);
            return;
        }

        for (var index = 1; index < points.Count; index++)
        {
            graphics.DrawLine(
                pen,
                points[index - 1].X,
                points[index - 1].Y,
                points[index].X,
                points[index].Y);
        }
    }

    private static void RenderLine(Graphics graphics, Pen pen, IReadOnlyList<CanvasPoint> points)
    {
        var first = points[0];
        var last = points[^1];
        graphics.DrawLine(pen, first.X, first.Y, last.X, last.Y);
    }

    private static RectangleF GetBounds(IReadOnlyList<CanvasPoint> points)
    {
        var first = points[0];
        var last = points[^1];

        var left = Math.Min(first.X, last.X);
        var top = Math.Min(first.Y, last.Y);
        var width = Math.Abs(first.X - last.X);
        var height = Math.Abs(first.Y - last.Y);

        return new RectangleF(left, top, width, height);
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}