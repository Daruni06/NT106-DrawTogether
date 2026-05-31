using DrawTogether.Shared.Models;

namespace DrawTogether.Shared.Messages;

public sealed class SendChatMessageRequest
{
    public string RoomId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public sealed class ChatHistoryResponse
{
    public IReadOnlyList<ChatMessage> Messages { get; init; } = Array.Empty<ChatMessage>();
}
