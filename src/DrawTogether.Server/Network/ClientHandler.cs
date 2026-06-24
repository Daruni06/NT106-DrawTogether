using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DrawTogether.Server.Network;

public class ClientHandler
{
private readonly TcpClient _client;


private readonly TcpServer _server;

private readonly Stream _stream;

private Thread? _thread;

private bool _running;

public ClientHandler(
    TcpClient client,
    SslStream ssl,
    TcpServer server)
{
    _client = client;
    _stream = ssl;
    _server = server;
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
    catch
    {
    }

    try
    {
        _client.Close();
    }
    catch
    {
    }
}

private void ProcessClient()
{
    byte[] buffer = new byte[4096];

    while (_running)
    {
        try
        {
            int bytesRead =
                _stream.Read(
                    buffer,
                    0,
                    buffer.Length);

            if (bytesRead == 0)
            {
                Console.WriteLine(
                    "Client disconnected");

                break;
            }

            string message =
                Encoding.UTF8.GetString(
                    buffer,
                    0,
                    bytesRead);

            Console.WriteLine(
                "Received: " + message);

            Send(
                "Server received: " + message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "ClientHandler error: " +
                ex.Message);

            break;
        }
    }

    _server.Unregister(this);

    Stop();
}

public void Send(string message)
{
    try
    {
        byte[] data =
            Encoding.UTF8.GetBytes(message);

        _stream.Write(
            data,
            0,
            data.Length);

        _stream.Flush();
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            "Send error: " +
            ex.Message);
    }
}


}
