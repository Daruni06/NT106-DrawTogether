using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DrawTogether.LoadBalancer.Handlers
{
    public class LoadBalancerHandler
    {
        private readonly TcpClient _client;
        private static readonly List<string> _drawingServers = new List<string>();
        private static int _currentIndex = 0;
        private static readonly object _lock = new object();

        public LoadBalancerHandler(TcpClient client)
        {
            _client = client;
        }

        // ==========================================
        // KHU VỰC 1: XỬ LÝ KHÁCH HÀNG & SERVER ĐĂNG KÝ
        // ==========================================
        public async Task HandleClientAsync()
        {
            try
            {
                using NetworkStream stream = _client.GetStream();
                byte[] buffer = new byte[1024];

                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) return;

                string requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                using JsonDocument doc = JsonDocument.Parse(requestJson);
                string type = doc.RootElement.GetProperty("type").GetString();

                if (type == "REGISTER_SERVER")
                {
                    string serverAddress = doc.RootElement.GetProperty("payload").GetProperty("server_address").GetString();
                    lock (_lock)
                    {
                        if (!_drawingServers.Contains(serverAddress))
                        {
                            _drawingServers.Add(serverAddress);
                        }
                    }
                    Console.WriteLine($"[Load Balancer] Vua them Server moi vao so: {serverAddress}");
                }
                else if (type == "REQUEST_SERVER")
                {
                    string targetServer = "";
                    lock (_lock)
                    {
                        if (_drawingServers.Count == 0)
                        {
                            Console.WriteLine("[Load Balancer] Tiem dang trong, khong co Server nao song de phan bo!");
                            return;
                        }

                        targetServer = _drawingServers[_currentIndex];
                        _currentIndex = (_currentIndex + 1) % _drawingServers.Count;
                    }

                    var response = new
                    {
                        type = "ASSIGN_SERVER",
                        payload = new { server_address = targetServer },
                        timestamp = DateTime.UtcNow.ToString("o")
                    };

                    string jsonResponse = JsonSerializer.Serialize(response);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse + "\n");

                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    await stream.FlushAsync();
                    _client.Client.Shutdown(SocketShutdown.Both);

                    Console.WriteLine($"[Load Balancer] Da dieu huong 1 Client toi: {targetServer}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Load Balancer] Loi xu ly: {ex.Message}");
            }
            finally
            {
                _client.Close();
            }
        }

        // ==========================================
        // KHU VỰC 2: HEALTHCHECK
        // ==========================================
        public static async Task StartHealthCheckAsync()
        {
            while (true)
            {
                await Task.Delay(15000); 

                List<string> serversToCheck;
                lock (_lock)
                {
                    serversToCheck = new List<string>(_drawingServers); 
                }

                foreach (var server in serversToCheck)
                {
                    bool isAlive = await PingServerAsync(server);
                    if (!isAlive)
                    {
                        Console.WriteLine($"[HealthCheck] Phat hien Server [{server}] DA CHET! => Xoa khoi so.");
                        lock (_lock)
                        {
                            _drawingServers.Remove(server);
                            if (_drawingServers.Count == 0) _currentIndex = 0;
                            else _currentIndex = _currentIndex % _drawingServers.Count;
                        }
                    }
                }
            }
        }

        private static async Task<bool> PingServerAsync(string serverAddress)
        {
            try
            {
                string[] parts = serverAddress.Split(':');
                using TcpClient pingClient = new TcpClient();

                Task connectTask = pingClient.ConnectAsync(parts[0], int.Parse(parts[1]));
                if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
                {
                    return false; // Quá thời gian (Timeout)
                }
                return pingClient.Connected;
            }
            catch
            {
                return false; // Lỗi từ chối kết nối
            }
        }
    }
}