using System.Net.Sockets;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;

namespace DrawTogether.Server.Network;

public sealed class ClientHandler
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly TcpServer _server;
    private Thread? _thread;
    private bool _running;

    public ClientHandler(TcpClient client, TcpServer server)
    {
        _client = client;
        _stream = client.GetStream();
        _server = server;
    }

    public string? CurrentRoomId { get; set; }

    public void Start()
    {
        _running = true;
        _thread = new Thread(ProcessClient)
        {
            IsBackground = true,
            Name = "DrawTogether.ClientHandler"
        };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _stream.Close();
        _client.Close();
    }

    public void Send(Message message)
    {
        try
        {
            MessageSerializer.WriteAsync(_stream, message).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send error: {ex.Message}");
            Stop();
        }
    }

    private void ProcessClient()
    {
        try
        {
            while (_running)
            {
                var message = MessageSerializer
                    .ReadAsync(_stream)
                    .GetAwaiter()
                    .GetResult();

                HandleMessage(message);
            }
        }
        catch (EndOfStreamException)
        {
            Console.WriteLine("Client disconnected.");
        }
        catch (IOException)
        {
            Console.WriteLine("Client connection closed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ClientHandler error: {ex.Message}");
        }
        finally
        {
            _server.Unregister(this);
            Stop();
        }
    }

    private void HandleMessage(Message message)
    {
        switch (message.Type)
        {
            case MessageType.JoinRoomRequest:
                HandleJoinRoom(message);
                break;
            case MessageType.DrawStroke:
            case MessageType.DrawShape:
                HandleDraw(message);
                break;
            case MessageType.ClearCanvasRequest:
                HandleClear(message);
                break;
            case MessageType.UndoRequest:
                HandleUndo(message);
                break;
            case MessageType.ChatSend:
            case MessageType.ChatFileSend:
                HandleChat(message);
                break;
            default:
                Send(Message.CreateError(message, MessageErrorCode.UnknownMessageType, "Unsupported realtime message."));
                break;
        }
    }

    private void HandleJoinRoom(Message message)
    {
        var roomId = message.RoomId ?? message.GetPayload<RoomPayload>()?.RoomId;
        _server.JoinRoom(this, roomId);
    }

    private void HandleDraw(Message message)
    {
        var stroke = message.GetPayload<Stroke>();
        if (stroke is null || string.IsNullOrWhiteSpace(stroke.RoomId))
        {
            Send(Message.CreateError(message, MessageErrorCode.InvalidPayload, "Invalid stroke payload."));
            return;
        }

        _server.JoinRoom(this, stroke.RoomId);
        _server.AddStrokeToHistory(stroke);

        var broadcastType = message.Type == MessageType.DrawShape ? MessageType.DrawShape : MessageType.DrawStroke;
        _server.BroadcastToRoom(
            Message.Create(broadcastType, stroke, roomId: stroke.RoomId, senderId: stroke.UserId),
            except: this);
    }

    private void HandleClear(Message message)
    {
        var roomId = message.RoomId ?? message.GetPayload<RoomPayload>()?.RoomId ?? CurrentRoomId;
        if (string.IsNullOrWhiteSpace(roomId))
        {
            Send(Message.CreateError(message, MessageErrorCode.InvalidPayload, "Room id is required."));
            return;
        }

        _server.ClearHistory(roomId);
        _server.BroadcastToRoom(
            Message.Create(MessageType.ClearCanvasEvent, new { roomId }, roomId: roomId, senderId: message.SenderId),
            except: this);
    }

    private void HandleUndo(Message message)
    {
        var payload = message.GetPayload<UndoPayload>();
        var roomId = message.RoomId ?? payload?.RoomId ?? CurrentRoomId;
        var strokeId = payload?.StrokeId;

        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(strokeId))
        {
            Send(Message.CreateError(message, MessageErrorCode.InvalidPayload, "Room id and stroke id are required."));
            return;
        }

        _server.RemoveStrokeFromHistory(roomId, strokeId);
        _server.BroadcastToRoom(
            Message.Create(MessageType.UndoEvent, new { roomId, strokeId }, roomId: roomId, senderId: message.SenderId),
            except: this);
    }

    private void HandleChat(Message message)
    {
        var chatMessage = message.GetPayload<ChatMessage>();
        if (chatMessage is null || string.IsNullOrWhiteSpace(chatMessage.RoomId))
        {
            Send(Message.CreateError(message, MessageErrorCode.InvalidPayload, "Invalid chat payload."));
            return;
        }

        _server.JoinRoom(this, chatMessage.RoomId);
        var type = chatMessage.Attachment is null ? MessageType.ChatMessage : MessageType.ChatFileMessage;
        _server.BroadcastToRoom(
            Message.Create(type, chatMessage, roomId: chatMessage.RoomId, senderId: chatMessage.SenderId),
            except: this);
    }

    private sealed class RoomPayload
    {
        public string RoomId { get; set; } = string.Empty;
    }

    private sealed class UndoPayload
    {
        public string? RoomId { get; set; }
        public string StrokeId { get; set; } = string.Empty;
    }
}
