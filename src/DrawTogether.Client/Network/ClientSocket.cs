using System.Net.Sockets;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;
using NetworkMessage = DrawTogether.Shared.Messages.Message;

namespace DrawTogether.Client.Network;

public sealed class ClientSocket : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private ReceiveThread? _receiver;

    public event EventHandler<Stroke>? StrokeReceived;

    public event EventHandler<string>? UndoReceived;

    public event EventHandler? ClearReceived;

    public event EventHandler<IReadOnlyList<Stroke>>? CanvasSyncReceived;

    public event EventHandler<ChatMessage>? ChatMessageReceived;

    public event EventHandler<Exception>? ConnectionFailed;

    public bool IsConnected => _client?.Connected == true && _stream is not null;

    public void Connect(string host, int port)
    {
        Disconnect();

        _client = new TcpClient();
        _client.Connect(host, port);
        _stream = _client.GetStream();

        _receiver = new ReceiveThread(_stream);
        _receiver.MessageReceived += (_, message) => HandleMessage(message);
        _receiver.ReceiveFailed += (_, exception) => ConnectionFailed?.Invoke(this, exception);
        _receiver.Start();
    }

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        Disconnect();

        _client = new TcpClient();
        await _client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        _stream = _client.GetStream();

        _receiver = new ReceiveThread(_stream);
        _receiver.MessageReceived += (_, message) => HandleMessage(message);
        _receiver.ReceiveFailed += (_, exception) => ConnectionFailed?.Invoke(this, exception);
        _receiver.Start();
    }

    public void AttachDrawingForm(DrawTogether.Client.Forms.DrawingForm form)
    {
        form.StrokeCompleted += (_, args) => SendStroke(args.Stroke);
        form.ClearRequested += (_, _) => SendClear(form.RoomId);
        form.UndoRequested += (_, args) => SendUndo(form.RoomId, args.StrokeId);
        form.ChatMessageSubmitted += (_, args) => SendChatMessage(args.Message);

        StrokeReceived += (_, stroke) => form.ApplyRemoteStroke(stroke);
        ClearReceived += (_, _) => form.ApplyRemoteClear();
        UndoReceived += (_, strokeId) => form.ApplyRemoteUndo(strokeId);
        CanvasSyncReceived += (_, strokes) => form.LoadHistory(strokes);
        ChatMessageReceived += (_, chatMessage) => form.ApplyRemoteChatMessage(chatMessage);
    }

    public void SendStroke(Stroke stroke)
    {
        var type = stroke.Tool is DrawingToolType.Line or DrawingToolType.Rectangle or DrawingToolType.Ellipse
            ? MessageType.DrawShape
            : MessageType.DrawStroke;

        Send(NetworkMessage.Create(type, stroke, roomId: stroke.RoomId, senderId: stroke.UserId));
    }

    public void JoinRoom(string? roomId, string? senderId = null)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        Send(NetworkMessage.Create(
            MessageType.JoinRoomRequest,
            new { roomId },
            roomId: roomId,
            senderId: senderId));
    }

    public void SendClear(string? roomId)
    {
        Send(NetworkMessage.Create(MessageType.ClearCanvasRequest, new { roomId }, roomId: roomId));
    }

    public void SendUndo(string? roomId, string strokeId)
    {
        Send(NetworkMessage.Create(MessageType.UndoRequest, new { roomId, strokeId }, roomId: roomId));
    }

    public void SendChatMessage(ChatMessage message)
    {
        var type = message.Attachment is null ? MessageType.ChatSend : MessageType.ChatFileSend;
        Send(NetworkMessage.Create(type, message, roomId: message.RoomId, senderId: message.SenderId));
    }

    public void Send(NetworkMessage message)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        MessageSerializer.WriteAsync(_stream, message).GetAwaiter().GetResult();
    }

    public void Disconnect()
    {
        _receiver?.Stop();
        _receiver = null;

        _stream?.Close();
        _stream = null;

        _client?.Close();
        _client = null;
    }

    public void Dispose()
    {
        Disconnect();
    }

    private void HandleMessage(NetworkMessage message)
    {
        switch (message.Type)
        {
            case MessageType.DrawStroke:
            case MessageType.DrawShape:
                RaiseIfPayload(message, StrokeReceived);
                break;
            case MessageType.ClearCanvasEvent:
                ClearReceived?.Invoke(this, EventArgs.Empty);
                break;
            case MessageType.UndoEvent:
                var undoPayload = message.GetPayload<UndoPayload>();
                if (!string.IsNullOrWhiteSpace(undoPayload?.StrokeId))
                {
                    UndoReceived?.Invoke(this, undoPayload.StrokeId);
                }
                break;
            case MessageType.CanvasSync:
                var syncPayload = message.GetPayload<CanvasSyncPayload>();
                CanvasSyncReceived?.Invoke(this, syncPayload?.Strokes ?? new List<Stroke>());
                break;
            case MessageType.ChatMessage:
            case MessageType.ChatFileMessage:
                RaiseIfPayload(message, ChatMessageReceived);
                break;
        }
    }

    private void RaiseIfPayload<T>(NetworkMessage message, EventHandler<T>? handler)
    {
        var payload = message.GetPayload<T>();
        if (payload is not null)
        {
            handler?.Invoke(this, payload);
        }
    }

    private sealed class UndoPayload
    {
        public string StrokeId { get; set; } = string.Empty;
    }

    private sealed class CanvasSyncPayload
    {
        public List<Stroke> Strokes { get; set; } = new();
    }
}
