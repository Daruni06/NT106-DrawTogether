using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;

namespace DrawTogether.Server.Network;

public class ClientHandler
{
    private readonly TcpClient _client;
    private readonly Stream _stream; // Giữ Stream (SslStream) từ thượng nguồn để bảo mật
    private readonly TcpServer _server;
    private readonly MessageRouter _router;

    private bool _running;
    private Thread? _thread;

    // Thuộc tính cần thiết để lưu Room ID hiện tại của Client (được gọi từ TcpServer)
    public string? CurrentRoomId { get; set; }

    public ClientHandler(TcpClient client, Stream sslStream, TcpServer server, MessageRouter router)
    {
        _client = client;
        _stream = sslStream; // Sử dụng sslStream đã được authenticated
        _server = server;
        _router = router;
    }

    public void Start()
    {
        _running = true;

        _thread = new Thread(ProcessClient)
        {
            IsBackground = true
        };

        _thread.Start();
    }

    public void Stop()
    {
        _running = false;

        try
        {
            _stream.Close();
        }
        catch { }

        try
        {
            _client.Close();
        }
        catch { }
    }

    public void Send(Message message)
    {
        try
        {
            MessageSerializer.WriteAsync(_stream, message)
                .GetAwaiter().GetResult();
        }
        catch
        {
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

                if (message == null) break;

                var request = new NetworkRequest
                {
                    Type = message.Type.ToString(),
                    PayloadJson = JsonSerializer.Serialize(message.Payload),
                    Token = message.Token
                };

                var response = _router
                    .RouteAsync(request)
                    .GetAwaiter()
                    .GetResult();

                Send(Message.Create(
                    message.Type,
                    response.PayloadJson,
                    roomId: message.RoomId,
                    senderId: "server"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
        finally
        {
            _server.Unregister(this);
            Stop();
        }
    }
}