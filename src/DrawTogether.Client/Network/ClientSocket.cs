using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using DrawTogether.Client.Forms;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;
using NetworkMessage = DrawTogether.Shared.Messages.Message;

namespace DrawTogether.Client.Network;

public sealed class ClientSocket : IDisposable
{
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);

    private TcpClient? _client;
    private Stream? _stream;
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

        var ssl = new SslStream(_client.GetStream(), false, (sender, cert, chain, errors) => true);
        ssl.AuthenticateAsClient(host);

        _stream = ssl;

        _receiver = new ReceiveThread(_stream);
        _receiver.MessageReceived += (_, message) => HandleMessage(message);
        _receiver.ReceiveFailed += (_, ex) => ConnectionFailed?.Invoke(this, ex);
        _receiver.Start();
    }

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        Disconnect();
        _client = new TcpClient();
        await _client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        var ssl = new SslStream(_client.GetStream(), false, (sender, cert, chain, errors) => true);
        await ssl.AuthenticateAsClientAsync(host).ConfigureAwait(false);

        _stream = ssl;

        _receiver = new ReceiveThread(_stream);
        _receiver.MessageReceived += (_, message) => HandleMessage(message);
        _receiver.ReceiveFailed += (_, ex) => ConnectionFailed?.Invoke(this, ex);
        _receiver.Start();
    }

    public Task JoinRoomAsync(string? roomId, string? senderId = null)
    {
        if (string.IsNullOrWhiteSpace(roomId)) return Task.CompletedTask;
        return SendAsync(NetworkMessage.Create(MessageType.JoinRoomRequest, new { roomId }, roomId: roomId, senderId: senderId));
    }

    public Task SendStrokeAsync(Stroke stroke)
    {
        var type = stroke.Tool is DrawingToolType.Line or DrawingToolType.Rectangle or DrawingToolType.Ellipse ? MessageType.DrawShape : MessageType.DrawStroke;
        return SendAsync(NetworkMessage.Create(type, stroke, roomId: stroke.RoomId, senderId: stroke.UserId));
    }

    public Task SendClearAsync(string? roomId) => SendAsync(NetworkMessage.Create(MessageType.ClearCanvasRequest, new { roomId }, roomId: roomId));
    public Task SendUndoAsync(string? roomId, string strokeId) => SendAsync(NetworkMessage.Create(MessageType.UndoRequest, new { roomId, strokeId }, roomId: roomId));
    public Task SendChatMessageAsync(ChatMessage message) => SendAsync(NetworkMessage.Create(message.Attachment is null ? MessageType.ChatSend : MessageType.ChatFileSend, message, roomId: message.RoomId, senderId: message.SenderId));

    public async Task SendAsync(NetworkMessage message)
    {
        if (_stream is null) throw new InvalidOperationException("Client is not connected.");
        await _sendSemaphore.WaitAsync().ConfigureAwait(false);
        try { await MessageSerializer.WriteAsync(_stream, message).ConfigureAwait(false); }
        finally { _sendSemaphore.Release(); }
    }

    public void Disconnect()
    {
        _receiver?.Stop(); _receiver = null;
        _stream?.Close(); _stream = null;
        _client?.Close(); _client = null;
    }

    public void Dispose() { Disconnect(); }

    public void AttachDrawingForm(DrawTogether.Client.Forms.DrawingForm form)
    {
        form.StrokeCompleted += (_, args) =>
        {
            _ = SendStrokeAsync(args.Stroke).ContinueWith(t =>
            {
                if (t.IsFaulted) ConnectionFailed?.Invoke(this, t.Exception?.InnerException ?? t.Exception!);
            }, TaskScheduler.Default);
        };

        form.ClearRequested += (_, _) =>
        {
            _ = SendClearAsync(form.RoomId).ContinueWith(t =>
            {
                if (t.IsFaulted) ConnectionFailed?.Invoke(this, t.Exception?.InnerException ?? t.Exception!);
            }, TaskScheduler.Default);
        };

        form.UndoRequested += (_, args) =>
        {
            _ = SendUndoAsync(form.RoomId, args.StrokeId).ContinueWith(t =>
            {
                if (t.IsFaulted) ConnectionFailed?.Invoke(this, t.Exception?.InnerException ?? t.Exception!);
            }, TaskScheduler.Default);
        };

        form.ChatMessageSubmitted += (_, args) =>
        {
            _ = SendChatMessageAsync(args.Message).ContinueWith(t =>
            {
                if (t.IsFaulted) ConnectionFailed?.Invoke(this, t.Exception?.InnerException ?? t.Exception!);
            }, TaskScheduler.Default);
        };

        StrokeReceived += (_, stroke) => form.ApplyRemoteStroke(stroke);
        ClearReceived += (_, _) => form.ApplyRemoteClear();
        UndoReceived += (_, strokeId) => form.ApplyRemoteUndo(strokeId);
        CanvasSyncReceived += (_, strokes) => form.LoadHistory(strokes);
        ChatMessageReceived += (_, chat) => form.ApplyRemoteChatMessage(chat);
    }

    private void HandleMessage(NetworkMessage message)
    {
        switch (message.Type)
        {
            case MessageType.DrawStroke:
            case MessageType.DrawShape:
                var stroke = message.GetPayload<Stroke>();
                try { Console.WriteLine($"[ClientSocket] Received draw type={message.Type} id={stroke?.StrokeId} sender={message.SenderId} room={message.RoomId}"); } catch { }
                if (stroke is not null) StrokeReceived?.Invoke(this, stroke);
                break;
            case MessageType.ClearCanvasEvent:
                ClearReceived?.Invoke(this, EventArgs.Empty);
                break;
            case MessageType.UndoEvent:
                var undo = message.GetPayload<UndoPayload>();
                if (!string.IsNullOrWhiteSpace(undo?.StrokeId)) UndoReceived?.Invoke(this, undo.StrokeId);
                break;
            case MessageType.CanvasSync:
                var sync = message.GetPayload<CanvasSyncPayload>();
                CanvasSyncReceived?.Invoke(this, sync?.Strokes ?? new List<Stroke>());
                break;
            case MessageType.ChatMessage:
            case MessageType.ChatFileMessage:
                var chat = message.GetPayload<ChatMessage>(); if (chat is not null) ChatMessageReceived?.Invoke(this, chat);
                break;
        }
    }

    private sealed class UndoPayload { public string StrokeId { get; set; } = string.Empty; }
    private sealed class CanvasSyncPayload { public List<Stroke> Strokes { get; set; } = new(); }
}