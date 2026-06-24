using System.Net.Sockets;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;
using DrawTogether.Client.Forms;
using NetworkMessage = DrawTogether.Shared.Messages.Message;

namespace DrawTogether.Client.Network;

public sealed class ClientSocket : IDisposable
{
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);

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

    public void AttachDrawingForm(DrawingForm form)
    {
        form.StrokeCompleted += (_, args) =>
        {
            _ = SendStrokeAsync(args.Stroke).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    ConnectionFailed?.Invoke(this, t.Exception?.InnerException ?? t.Exception!);
                }
            }, TaskScheduler.Default);
        };

        form.ClearRequested += (_, _) =>
        {
            _ = SendClearAsync(form.RoomId).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    ConnectionFailed?.Invoke(this, t.Exception?.InnerException ?? t.Exception!);
                }
            }, TaskScheduler.Default);
        };

        form.UndoRequested += (_, args) =>
        {
            _ = SendUndoAsync(form.RoomId, args.StrokeId).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    ConnectionFailed?.Invoke(this, t.Exception?.InnerException ?? t.Exception!);
                }
            }, TaskScheduler.Default);
        };

        form.ChatMessageSubmitted += (_, args) =>
        {
            _ = SendChatMessageAsync(args.Message).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    ConnectionFailed?.Invoke(this, t.Exception?.InnerException ?? t.Exception!);
                }
            }, TaskScheduler.Default);
        };

        StrokeReceived += (_, stroke) => form.ApplyRemoteStroke(stroke);
        ClearReceived += (_, _) => form.ApplyRemoteClear();
        UndoReceived += (_, strokeId) => form.ApplyRemoteUndo(strokeId);
        CanvasSyncReceived += (_, strokes) => form.LoadHistory(strokes);
        ChatMessageReceived += (_, chatMessage) => form.ApplyRemoteChatMessage(chatMessage);
    }

    public Task SendStrokeAsync(Stroke stroke)
    {
        var type = stroke.Tool is DrawingToolType.Line or DrawingToolType.Rectangle or DrawingToolType.Ellipse
            ? MessageType.DrawShape
            : MessageType.DrawStroke;

        return SendAsync(NetworkMessage.Create(type, stroke, roomId: stroke.RoomId, senderId: stroke.UserId));
    }

    public void SendStroke(Stroke stroke)
    {
        SendStrokeAsync(stroke).GetAwaiter().GetResult();
    }

    public Task JoinRoomAsync(string? roomId, string? senderId = null)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return Task.CompletedTask;
        }

        return SendAsync(NetworkMessage.Create(
            MessageType.JoinRoomRequest,
            new { roomId },
            roomId: roomId,
            senderId: senderId));
    }

    public Task SendClearAsync(string? roomId)
    {
        return SendAsync(NetworkMessage.Create(MessageType.ClearCanvasRequest, new { roomId }, roomId: roomId));
    }

    public Task SendUndoAsync(string? roomId, string strokeId)
    {
        return SendAsync(NetworkMessage.Create(MessageType.UndoRequest, new { roomId, strokeId }, roomId: roomId));
    }

    public Task SendChatMessageAsync(ChatMessage message)
    {
        var type = message.Attachment is null ? MessageType.ChatSend : MessageType.ChatFileSend;
        return SendAsync(NetworkMessage.Create(type, message, roomId: message.RoomId, senderId: message.SenderId));
    }

    public async Task SendAsync(NetworkMessage message)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        await _sendSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await MessageSerializer.WriteAsync(_stream, message).ConfigureAwait(false);
        }
        finally
        {
            _sendSemaphore.Release();
        }
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
