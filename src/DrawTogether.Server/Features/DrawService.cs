
using System.Text.Json;
using DrawTogether.Server.Data;
using DrawTogether.Shared.Messages;

namespace DrawTogether.Server.Features;

public sealed class DrawService
{
    private readonly RoomRepository _rooms;
    private readonly DrawHistoryRepository _drawHistory;

    public DrawService(RoomRepository rooms, DrawHistoryRepository drawHistory)
    {
        _rooms = rooms;
        _drawHistory = drawHistory;
    }

    public async Task<ServiceResult<DrawHistoryResponse>> GetCanvasHistoryAsync(
        AuthenticatedUser currentUser,
        string roomId,
        long afterId = 0,
        CancellationToken cancellationToken = default)
    {
        if (!await _rooms.IsActiveMemberAsync(roomId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<DrawHistoryResponse>.Fail("You must join room before loading canvas history.");
        }

        var actions = await _drawHistory.GetByRoomIdAsync(roomId, afterId, cancellationToken: cancellationToken);
        return ServiceResult<DrawHistoryResponse>.Ok(new DrawHistoryResponse { Actions = actions });
    }

    public async Task<ServiceResult<DrawActionSavedResponse>> SaveDrawActionAsync(
        AuthenticatedUser currentUser,
        SaveDrawActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomId = request.RoomId.Trim();
        var actionType = request.ActionType.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(roomId))
        {
            return ServiceResult<DrawActionSavedResponse>.Fail("Room id is required.");
        }

        if (actionType.Length is < 2 or > 40)
        {
            return ServiceResult<DrawActionSavedResponse>.Fail("Invalid draw action type.");
        }

        if (!IsValidJson(request.PayloadJson))
        {
            return ServiceResult<DrawActionSavedResponse>.Fail("Draw payload must be valid JSON.");
        }

        if (!await _rooms.IsActiveMemberAsync(roomId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<DrawActionSavedResponse>.Fail("You must join room before drawing.");
        }

        if (actionType == "clear_canvas")
        {
            await _drawHistory.ClearRoomHistoryAsync(roomId, cancellationToken);
        }

        var savedAction = await _drawHistory.SaveAsync(
            roomId,
            currentUser.UserId,
            actionType,
            request.PayloadJson,
            cancellationToken);

        return ServiceResult<DrawActionSavedResponse>.Ok(new DrawActionSavedResponse
        {
            Action = savedAction
        }, "Draw action saved.");
    }

    private static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class DrawActionSavedResponse
{
    public DrawTogether.Shared.Models.DrawAction Action { get; init; } = new();
}

