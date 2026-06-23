using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DrawTogether.Server.Network
{
    public class TcpServer
    {
        private TcpListener _listener;

        private bool _running;

        public void Start(int port)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);

                _listener.Start();

                _running = true;

                Console.WriteLine("Server started on port " + port);

                Thread acceptThread = new Thread(AcceptLoop);

                acceptThread.IsBackground = true;

                acceptThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Server start error: " + ex.Message);
            }
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();

                    Console.WriteLine("Client connected");

                    ClientHandler handler = new ClientHandler(client);

                    handler.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Accept error: " + ex.Message);
                }
            }
        }

        public void Stop()
        {
            _running = false;

            _listener?.Stop();
        }
    }
}