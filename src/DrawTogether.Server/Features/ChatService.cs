
using DrawTogether.Server.Data;
using DrawTogether.Shared.Messages;

namespace DrawTogether.Server.Features;

public sealed class ChatService
{
    private readonly RoomRepository _rooms;
    private readonly ChatHistoryRepository _chatHistory;

    public ChatService(RoomRepository rooms, ChatHistoryRepository chatHistory)
    {
        _rooms = rooms;
        _chatHistory = chatHistory;
    }

    public async Task<ServiceResult<ChatHistoryResponse>> GetHistoryAsync(
        AuthenticatedUser currentUser,
        string roomId,
        CancellationToken cancellationToken = default)
    {
        if (!await _rooms.IsActiveMemberAsync(roomId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<ChatHistoryResponse>.Fail("You must join room before reading chat.");
        }

        var messages = await _chatHistory.GetByRoomIdAsync(roomId, cancellationToken: cancellationToken);
        return ServiceResult<ChatHistoryResponse>.Ok(new ChatHistoryResponse { Messages = messages });
    }

    public async Task<ServiceResult<ChatMessageSavedResponse>> SendMessageAsync(
        AuthenticatedUser currentUser,
        SendChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomId = request.RoomId.Trim();
        var message = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(roomId))
        {
            return ServiceResult<ChatMessageSavedResponse>.Fail("Room id is required.");
        }

        if (message.Length is < 1 or > 1000)
        {
            return ServiceResult<ChatMessageSavedResponse>.Fail("Chat message must be 1-1000 characters.");
        }

        if (!await _rooms.IsActiveMemberAsync(roomId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<ChatMessageSavedResponse>.Fail("You must join room before sending chat.");
        }

        var saved = await _chatHistory.SaveAsync(roomId, currentUser.UserId, message, cancellationToken);

        return ServiceResult<ChatMessageSavedResponse>.Ok(new ChatMessageSavedResponse
        {
            Message = saved
        }, "Message saved.");
    }
}

public sealed class ChatMessageSavedResponse
{
    public DrawTogether.Shared.Models.ChatMessage Message { get; init; } = new();
}

