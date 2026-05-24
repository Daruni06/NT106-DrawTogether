using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconnected");

                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Console.WriteLine("Received: " + message);

                    Send("Server received: " + message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ClientHandler error: " + ex.Message);

                    break;
                }
            }
        }

        public void Send(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);

                _stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send error: " + ex.Message);
            }
        }
    }
}