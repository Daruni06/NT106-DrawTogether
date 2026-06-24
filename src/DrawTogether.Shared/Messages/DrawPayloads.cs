using DrawTogether.Shared.Models;

namespace DrawTogether.Shared.Messages;

public sealed class SaveDrawActionRequest
{
    public string RoomId { get; init; } = string.Empty;

    // Examples: "line", "rectangle", "ellipse", "freehand", "eraser", "clear_canvas"
    public string ActionType { get; init; } = string.Empty;

    // Keep this as JSON so future tools can add fields without changing the DB schema.
    public string PayloadJson { get; init; } = "{}";
}

public sealed class DrawHistoryRequest
{
    public string RoomId { get; init; } = string.Empty;
    public long AfterId { get; init; }
}
public sealed class DrawHistoryResponse
{
    public IReadOnlyList<DrawAction> Actions { get; init; } = Array.Empty<DrawAction>();
}
