using DrawTogether.Server.Data;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;

namespace DrawTogether.Server.Features;

public sealed class RoomService
{
    private readonly RoomRepository _rooms;
    private readonly DrawHistoryRepository _drawHistory;

    public RoomService(RoomRepository rooms, DrawHistoryRepository drawHistory)
    {
        _rooms = rooms;
        _drawHistory = drawHistory;
    }

    public async Task<ServiceResult<RoomDetailsResponse>> CreateRoomAsync(
        AuthenticatedUser currentUser,
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomName = request.Name.Trim();
        if (roomName.Length is < 3 or > 100)
        {
            return ServiceResult<RoomDetailsResponse>.Fail("Room name must be 3-100 characters.");
        }

        var maxMembers = Math.Clamp(request.MaxMembers, 2, 50);

        var room = await _rooms.CreateAsync(roomName, currentUser.UserId, maxMembers, cancellationToken);
        await _rooms.JoinAsync(room.Id, currentUser.UserId, cancellationToken);

        var updatedRoom = await _rooms.GetByIdAsync(room.Id, cancellationToken) ?? room;
        var members = await _rooms.ListActiveMembersAsync(room.Id, cancellationToken);

        return ServiceResult<RoomDetailsResponse>.Ok(new RoomDetailsResponse
        {
            Room = updatedRoom,
            Members = members,
            CanvasHistory = Array.Empty<DrawAction>()
        }, "Room created.");
    }

    public async Task<ServiceResult<RoomDetailsResponse>> JoinRoomAsync(
        AuthenticatedUser currentUser,
        JoinRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomId = request.RoomId.Trim();
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return ServiceResult<RoomDetailsResponse>.Fail("Room id is required.");
        }

        var room = await _rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            return ServiceResult<RoomDetailsResponse>.Fail("Room not found.");
        }

        if (room.IsClosed)
        {
            return ServiceResult<RoomDetailsResponse>.Fail("Room is closed.");
        }

        if (room.ActiveMemberCount >= room.MaxMembers &&
            !await _rooms.IsActiveMemberAsync(roomId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<RoomDetailsResponse>.Fail("Room is full.");
        }

        await _rooms.JoinAsync(roomId, currentUser.UserId, cancellationToken);

        var updatedRoom = await _rooms.GetByIdAsync(roomId, cancellationToken) ?? room;
        var members = await _rooms.ListActiveMembersAsync(roomId, cancellationToken);
        var canvasHistory = await _drawHistory.GetByRoomIdAsync(roomId, cancellationToken: cancellationToken);

        return ServiceResult<RoomDetailsResponse>.Ok(new RoomDetailsResponse
        {
            Room = updatedRoom,
            Members = members,
            CanvasHistory = canvasHistory
        }, "Joined room.");
    }

    public async Task<ServiceResult<EmptyResult>> LeaveRoomAsync(
        AuthenticatedUser currentUser,
        LeaveRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomId = request.RoomId.Trim();
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return ServiceResult<EmptyResult>.Fail("Room id is required.");
        }

        await _rooms.LeaveAsync(roomId, currentUser.UserId, cancellationToken);
        return ServiceResult<EmptyResult>.Ok(EmptyResult.Value, "Left room.");
    }

    public async Task<ServiceResult<RoomListResponse>> ListOpenRoomsAsync(
        CancellationToken cancellationToken = default)
    {
        var rooms = await _rooms.ListOpenRoomsAsync(cancellationToken);

        return ServiceResult<RoomListResponse>.Ok(new RoomListResponse
        {
            Rooms = rooms
        });
    }

    public async Task<ServiceResult<EmptyResult>> CloseRoomAsync(
        AuthenticatedUser currentUser,
        string roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
        {
            return ServiceResult<EmptyResult>.Fail("Room not found.");
        }

        if (room.OwnerUserId != currentUser.UserId)
        {
            return ServiceResult<EmptyResult>.Fail("Only room owner can close this room.");
        }

        await _rooms.CloseRoomAsync(roomId, cancellationToken);
        return ServiceResult<EmptyResult>.Ok(EmptyResult.Value, "Room closed.");
    }
}
