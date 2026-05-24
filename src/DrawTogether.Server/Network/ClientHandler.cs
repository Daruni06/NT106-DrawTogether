// Xu ly mot client dang ket noi.
// Doc message JSON, goi service phu hop va gui response/broadcast.
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using DrawTogether.Shared.Messages;
using DrawTogether.Server.Features;

namespace DrawTogether.Server.Network
{
    public class ClientHandler
    {
        private readonly TcpClient _client;

        private readonly NetworkStream _stream;

        private Thread _thread;

        private bool _running;

        public ClientHandler(TcpClient client)
        {
            _client = client;

            _stream = client.GetStream();
        }

        public void Start()
        {
            _running = true;

            _thread = new Thread(ProcessClient);

            _thread.IsBackground = true;

            _thread.Start();
        }

        private void ProcessClient()
        {
            byte[] buffer = new byte[4096];

            while (_running)
            {
                try
                {
                    int bytesRead =
                        _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconnected");

                        break;
                    }

                    string json =
                        Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Message msg =
                        MessageSerializer.Deserialize(json);

                    HandleMessage(msg);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "ClientHandler error: " + ex.Message);

                    break;
                }
            }
        }

        private void HandleMessage(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.Chat:

                    ChatService.Handle(msg);

                    break;

                case MessageType.Draw:

                    DrawService.Handle(msg);

                    break;

                default:

                    Console.WriteLine("Unknown message");

                    break;
            }
        }

        public void Send(string message)
        {
            try
            {
                byte[] data =
                    Encoding.UTF8.GetBytes(message);

                _stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Send error: " + ex.Message);
            }
        }
    }
}