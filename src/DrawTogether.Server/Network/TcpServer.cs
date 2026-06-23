using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;

namespace DrawTogether.Server.Network;

public sealed class TcpServer
{
    private readonly ConcurrentDictionary<ClientHandler, byte> _clients = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<ClientHandler, byte>> _roomClients = new();
    private readonly ConcurrentDictionary<string, List<Stroke>> _roomHistory = new();

    private TcpListener? _listener;
    private bool _running;
    private Thread? _acceptThread;

    public int ClientCount => _clients.Count;

    public void Start(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _running = true;

        _acceptThread = new Thread(AcceptLoop)
        {
            IsBackground = true,
            Name = "DrawTogether.AcceptLoop"
        };
        _acceptThread.Start();

        Console.WriteLine($"Drawing server started on port {port}.");
    }

    public void Stop()
    {
        _running = false;
        _listener?.Stop();

        foreach (var client in _clients.Keys)
        {
            client.Stop();
        }

        _clients.Clear();
        _roomClients.Clear();
    }

    internal void Register(ClientHandler client)
    {
        _clients.TryAdd(client, 0);
    }

    internal void Unregister(ClientHandler client)
    {
        _clients.TryRemove(client, out _);

        foreach (var room in _roomClients.Values)
        {
            room.TryRemove(client, out _);
        }
    }

    internal void JoinRoom(ClientHandler client, string? roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        var normalizedRoomId = roomId.Trim().ToUpperInvariant();
        var clients = _roomClients.GetOrAdd(normalizedRoomId, _ => new ConcurrentDictionary<ClientHandler, byte>());
        var added = clients.TryAdd(client, 0);
        client.CurrentRoomId = normalizedRoomId;

        if (!added)
        {
            // Client already in room; no need to resend full canvas sync.
            return;
        }

        var history = _roomHistory.TryGetValue(normalizedRoomId, out var strokes)
            ? strokes.ToList()
            : new List<Stroke>();

        client.Send(Message.Create(
            MessageType.CanvasSync,
            new { strokes = history },
            roomId: normalizedRoomId,
            senderId: "server"));
    }

    internal void BroadcastToRoom(Message message, ClientHandler? except = null)
    {
        var roomId = message.RoomId;

        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        if (!_roomClients.TryGetValue(roomId.Trim().ToUpperInvariant(), out var clients))
        {
            return;
        }

        foreach (var client in clients.Keys)
        {
            if (ReferenceEquals(client, except))
            {
                continue;
            }

            client.Send(message);
        }
    }

    internal void AddStrokeToHistory(Stroke stroke)
    {
        if (string.IsNullOrWhiteSpace(stroke.RoomId))
        {
            return;
        }

        var roomId = stroke.RoomId.Trim().ToUpperInvariant();
        var history = _roomHistory.GetOrAdd(roomId, _ => new List<Stroke>());

        lock (history)
        {
            history.Add(stroke.Clone());
        }
    }

    internal void RemoveStrokeFromHistory(string? roomId, string strokeId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        if (_roomHistory.TryGetValue(roomId.Trim().ToUpperInvariant(), out var history))
        {
            lock (history)
            {
                history.RemoveAll(stroke => stroke.StrokeId == strokeId);
            }
        }
    }

    internal void ClearHistory(string? roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return;
        }

        if (_roomHistory.TryGetValue(roomId.Trim().ToUpperInvariant(), out var history))
        {
            lock (history)
            {
                history.Clear();
            }
        }
    }

    private void AcceptLoop()
    {
        while (_running)
        {
            try
            {
                var tcpClient = _listener!.AcceptTcpClient();
                var handler = new ClientHandler(tcpClient, this);
                Register(handler);
                handler.Start();
            }
            catch (SocketException) when (!_running)
            {
                break;
            }
            catch (ObjectDisposedException) when (!_running)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Accept error: {ex.Message}");
            }
        }
    }
}
