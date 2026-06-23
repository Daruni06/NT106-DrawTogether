// Xu ly chat trong phong.
// Nhan tin nhan, validate noi dung, broadcast va co the luu lich su chat.
using DrawTogether.Server.Data;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;

namespace DrawTogether.Server.Features;

public sealed class ChatService
{
    public const int MaxTextLength = 500;
    public const long MaxAttachmentBytes = 5 * 1024 * 1024;

    private readonly RoomRepository? _rooms;
    private readonly ChatHistoryRepository? _chatHistory;

    public ChatService()
    {
    }

    public ChatService(RoomRepository rooms, ChatHistoryRepository chatHistory)
    {
        _rooms = rooms;
        _chatHistory = chatHistory;
    }

    public ChatMessage CreateTextMessage(
        string roomId,
        string senderId,
        string senderName,
        string content)
    {
        ValidateRoomAndSender(roomId, senderId);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Chat content is required.", nameof(content));
        }

        if (content.Length > MaxTextLength)
        {
            throw new ArgumentException($"Chat content must be <= {MaxTextLength} characters.", nameof(content));
        }

        return ChatMessage.CreateText(roomId, senderId, senderName, content);
    }

    public ChatMessage CreateFileMessage(
        string roomId,
        string senderId,
        string senderName,
        ChatAttachment attachment,
        string? caption = null)
    {
        ValidateRoomAndSender(roomId, senderId);
        ValidateAttachment(attachment);

        if (caption is not null && caption.Length > MaxTextLength)
        {
            throw new ArgumentException($"Caption must be <= {MaxTextLength} characters.", nameof(caption));
        }

        return ChatMessage.CreateFile(roomId, senderId, senderName, attachment, caption);
    }

    public void ValidateIncoming(ChatMessage message)
    {
        if (message.ContentType == ChatContentType.Text)
        {
            CreateTextMessage(
                message.RoomId ?? string.Empty,
                message.SenderId ?? string.Empty,
                message.SenderName,
                message.Content);
            return;
        }

        if (message.Attachment is null)
        {
            throw new ArgumentException("Attachment is required for file/image chat.", nameof(message));
        }

        CreateFileMessage(
            message.RoomId ?? string.Empty,
            message.SenderId ?? string.Empty,
            message.SenderName,
            message.Attachment,
            message.Content);
    }

    public async Task<ServiceResult<ChatMessage>> SendMessageAsync(
        AuthenticatedUser currentUser,
        SendChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomId = request.RoomId.Trim();
        var content = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(roomId))
        {
            return ServiceResult<ChatMessage>.Fail("Room id is required.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return ServiceResult<ChatMessage>.Fail("Message is required.");
        }

        if (content.Length > MaxTextLength)
        {
            return ServiceResult<ChatMessage>.Fail($"Message must be <= {MaxTextLength} characters.");
        }

        if (_rooms is not null &&
            !await _rooms.IsActiveMemberAsync(roomId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<ChatMessage>.Fail("You must join room before chatting.");
        }

        ChatMessage message;
        if (_chatHistory is not null)
        {
            message = await _chatHistory.SaveAsync(roomId, currentUser.UserId, content, cancellationToken);
            message.Username = currentUser.Username;
            message.DisplayName = currentUser.DisplayName;
        }
        else
        {
            message = ChatMessage.CreateText(
                roomId,
                currentUser.UserId.ToString(),
                currentUser.DisplayName,
                content);
            message.Username = currentUser.Username;
        }

        return ServiceResult<ChatMessage>.Ok(message, "Message sent.");
    }

    public async Task<ServiceResult<ChatHistoryResponse>> GetHistoryAsync(
        AuthenticatedUser currentUser,
        string roomId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return ServiceResult<ChatHistoryResponse>.Fail("Room id is required.");
        }

        if (_rooms is not null &&
            !await _rooms.IsActiveMemberAsync(roomId, currentUser.UserId, cancellationToken))
        {
            return ServiceResult<ChatHistoryResponse>.Fail("You must join room before loading chat history.");
        }

        var messages = _chatHistory is null
            ? Array.Empty<ChatMessage>()
            : await _chatHistory.GetByRoomIdAsync(roomId, cancellationToken: cancellationToken);

        return ServiceResult<ChatHistoryResponse>.Ok(new ChatHistoryResponse { Messages = messages });
    }

    private static void ValidateRoomAndSender(string roomId, string senderId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            throw new ArgumentException("Room id is required.", nameof(roomId));
        }

        if (string.IsNullOrWhiteSpace(senderId))
        {
            throw new ArgumentException("Sender id is required.", nameof(senderId));
        }
    }

    private static void ValidateAttachment(ChatAttachment attachment)
    {
        if (string.IsNullOrWhiteSpace(attachment.FileName))
        {
            throw new ArgumentException("Attachment file name is required.", nameof(attachment));
        }

        if (string.IsNullOrWhiteSpace(attachment.Base64Data))
        {
            throw new ArgumentException("Attachment data is required.", nameof(attachment));
        }

        if (attachment.Size <= 0)
        {
            throw new ArgumentException("Attachment size is invalid.", nameof(attachment));
        }

        if (attachment.Size > MaxAttachmentBytes)
        {
            throw new ArgumentException($"Attachment must be <= {MaxAttachmentBytes} bytes.", nameof(attachment));
        }
    }
}
