using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace DrawTogether.Server.Network;

public sealed class TcpServer
{
    private X509Certificate2? _certificate;
    private readonly ConcurrentDictionary<ClientHandler, byte> _clients = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<ClientHandler, byte>> _roomClients = new();
    private readonly ConcurrentDictionary<string, List<Stroke>> _roomHistory = new();

    private readonly MessageRouter _router;

    private TcpListener? _listener;
    private bool _running;

    public TcpServer(MessageRouter router)
    {
        _router = router;
    }

    public void Start(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Certificates", "drawtogether.pfx"),
            Path.Combine(AppContext.BaseDirectory, "..", "Certificates", "drawtogether.pfx"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "Certificates", "drawtogether.pfx"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Certificates", "drawtogether.pfx")
        };

        string? certPath = candidates.FirstOrDefault(File.Exists);
        if (certPath is null)
        {
            throw new FileNotFoundException($"Certificate file 'drawtogether.pfx' not found. Checked: {string.Join(';', candidates)}");
        }

        _certificate = new X509Certificate2(certPath, "123456");

        _listener.Start();
        _running = true;

        new Thread(AcceptLoop)
        {
            IsBackground = true
        }.Start();

        Console.WriteLine($"Server started on {port}");
    }

    public void Stop()
    {
        _running = false;
        _listener?.Stop();

        foreach (var c in _clients.Keys)
            c.Stop();

        _clients.Clear();
        _roomClients.Clear();
    }

    internal void Register(ClientHandler client) => _clients.TryAdd(client, 0);

    internal void Unregister(ClientHandler client)
    {
        _clients.TryRemove(client, out _);

        foreach (var room in _roomClients.Values)
            room.TryRemove(client, out _);
    }

    internal void JoinRoom(ClientHandler client, string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId)) return;

        roomId = roomId.Trim().ToUpperInvariant();

        var room = _roomClients.GetOrAdd(roomId,
            _ => new ConcurrentDictionary<ClientHandler, byte>());

        room.TryAdd(client, 0);
        client.CurrentRoomId = roomId;

        if (_roomHistory.TryGetValue(roomId, out var history))
        {
            client.Send(Message.Create(
                MessageType.CanvasSync,
                new { strokes = history },
                roomId: roomId,
                senderId: "server"));
        }
    }

    internal void BroadcastToRoom(Message message, ClientHandler? except = null)
    {
        if (string.IsNullOrWhiteSpace(message.RoomId)) return;

        var roomId = message.RoomId.Trim().ToUpperInvariant();

        if (!_roomClients.TryGetValue(roomId, out var clients)) return;

        foreach (var c in clients.Keys)
        {
            if (c == except) continue;
            c.Send(message);
        }
    }

    internal void AddStroke(Stroke stroke)
    {
        if (string.IsNullOrWhiteSpace(stroke?.RoomId)) return;

        var roomId = stroke.RoomId.Trim().ToUpperInvariant();

        var list = _roomHistory.GetOrAdd(roomId, _ => new List<Stroke>());

        lock (list)
        {
            list.Add(stroke.Clone());
        }
    }

    internal void ClearRoom(string roomId)
    {
        roomId = roomId.Trim().ToUpperInvariant();

        if (_roomHistory.TryGetValue(roomId, out var list))
        {
            lock (list) list.Clear();
        }
    }

    internal void RemoveStrokeFromHistory(string roomId, string strokeId)
    {
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(strokeId)) return;
        roomId = roomId.Trim().ToUpperInvariant();

        if (!_roomHistory.TryGetValue(roomId, out var list)) return;

        lock (list)
        {
            var idx = list.FindIndex(s => s.StrokeId == strokeId);
            if (idx >= 0) list.RemoveAt(idx);
        }
    }

    private void AcceptLoop()
    {
        while (_running)
        {
            try
            {
                var tcpClient = _listener!.AcceptTcpClient();

                // 1. Tạo luồng mã hóa SSL/TLS bảo mật kết nối
                SslStream ssl = new SslStream(tcpClient.GetStream(), false);

                // 2. Thực hiện xác thực TLS với chứng chỉ pfx
                ssl.AuthenticateAsServer(
                    _certificate!,
                    false,
                    SslProtocols.Tls12,
                    false);

                // 3. Khởi tạo ClientHandler với cả luồng SSL và Router xử lý logic tin nhắn
                var handler = new ClientHandler(tcpClient, ssl, this, _router);

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