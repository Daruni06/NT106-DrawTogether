using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace DrawTogether.Client.Network
{
public class ClientSocket
{
private TcpClient _client;


    private SslStream _ssl;

    private ReceiveThread _receiver;

    public bool Connect(string ip, int port)
    {
        try
        {
            _client = new TcpClient();

            _client.Connect(ip, port);

            _ssl = new SslStream(
                _client.GetStream(),
                false,
                (sender, cert, chain, errors) => true);

            _ssl.AuthenticateAsClient(
                "drawtogether.local",
                null,
                SslProtocols.Tls12,
                false);

            _receiver = new ReceiveThread(_ssl);

            _receiver.OnMessageReceived = HandleMessage;

            _receiver.Start();

            Console.WriteLine("Connected to server (TLS)");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Connect error: " + ex.Message);

            return false;
        }
    }

    private void HandleMessage(string message)
    {
        Console.WriteLine("Server: " + message);
    }

    public void SendMessage(string message)
    {
        try
        {
            if (_ssl == null)
                return;

            byte[] data =
                Encoding.UTF8.GetBytes(message);

            _ssl.Write(data, 0, data.Length);

            _ssl.Flush();

            Console.WriteLine("Sent: " + message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Send error: " + ex.Message);
        }
    }

    public void Disconnect()
    {
        try
        {
            _receiver?.Stop();

            _ssl?.Close();

            _client?.Close();

            Console.WriteLine("Disconnected");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Disconnect error: " + ex.Message);
        }
    }
}

}
