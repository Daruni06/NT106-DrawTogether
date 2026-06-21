namespace DrawTogether.Shared.Models;

public sealed class DrawAction
{
    public long Id { get; init; }
    public string RoomId { get; init; } = string.Empty;
    public long UserId { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = "{}";
    public DateTime CreatedAt { get; init; }
}
