using DrawTogether.Shared.Models;

namespace DrawTogether.Shared.Messages;

public sealed class CreateRoomRequest
{
    public string Name { get; init; } = string.Empty;
    public int MaxMembers { get; init; } = 10;
}

public sealed class JoinRoomRequest
{
    public string RoomId { get; init; } = string.Empty;
}

public sealed class LeaveRoomRequest
{
    public string RoomId { get; init; } = string.Empty;
}

public sealed class RoomDetailsResponse
{
    public Room Room { get; init; } = new();
    public IReadOnlyList<RoomMember> Members { get; init; } = Array.Empty<RoomMember>();
    public IReadOnlyList<DrawAction> CanvasHistory { get; init; } = Array.Empty<DrawAction>();
}

public sealed class RoomListResponse
{
    public IReadOnlyList<Room> Rooms { get; init; } = Array.Empty<Room>();
}
