using System;
using System.Net.Sockets;
using System.Text;

namespace DrawTogether.Client.Network
{
    public class ClientSocket
    {
        private TcpClient _client;
        private NetworkStream _stream;

        private ReceiveThread _receiver;

        public bool Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient();

                _client.Connect(ip, port);

                _stream = _client.GetStream();

                Console.WriteLine("Connected to server");

                StartReceiveThread();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connect error: " + ex.Message);

                return false;
            }
        }

        private void StartReceiveThread()
        {
            _receiver = new ReceiveThread(_stream);

            _receiver.OnMessageReceived = HandleMessage;

            _receiver.Start();
        }

        private void HandleMessage(string message)
        {
            Console.WriteLine("Server: " + message);

            // TODO:
            // Parse JSON
            // Update UI
            // Draw realtime
        }

        public void SendMessage(string message)
        {
            try
            {
                if (_stream == null)
                    return;

                byte[] data = Encoding.UTF8.GetBytes(message);

                _stream.Write(data, 0, data.Length);

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

                _stream?.Close();

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