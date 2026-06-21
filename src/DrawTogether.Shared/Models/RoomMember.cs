namespace DrawTogether.Shared.Models;

public sealed class RoomMember
{
    public long Id { get; init; }
    public string RoomId { get; init; } = string.Empty;
    public long UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTime JoinedAt { get; init; }
    public DateTime? LeftAt { get; init; }

    public bool IsActive => LeftAt is null;
}
