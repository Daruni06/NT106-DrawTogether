using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DrawTogether.LoadBalancer.Features;
namespace DrawTogether.LoadBalancer.Handlers
{
    public class LoadBalancerHandler
    {
        private readonly TcpClient _client;
        private static readonly ServerRegistry _registry = new();
        private static readonly LoadBalancingService _loadBalancer = new();
        private static readonly HealthCheckService _healthChecker = new();
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
                        _registry.Register(serverAddress);
                    }
                    Console.WriteLine($"[Load Balancer] Vua them Server moi vao so: {serverAddress}");
                }
                else if (type == "REQUEST_SERVER")
                {
                    string? targetServer;

                    lock (_lock)
                    {
                        if (_registry.Count == 0)
                        {
                            Console.WriteLine("[Load Balancer] Tiem dang trong, khong co Server nao song de phan bo!");
                            return;
                        }

                        targetServer =
                            _loadBalancer.GetNextServer(
                                _registry.GetAll());
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
                    serversToCheck = _registry.GetAll();
                }

                foreach (var server in serversToCheck)
                {
                    bool isAlive =
                        await _healthChecker
        .                   PingServerAsync(server);
                    if (!isAlive)
                    {
                        Console.WriteLine($"[HealthCheck] Phat hien Server [{server}] DA CHET! => Xoa khoi so.");
                        lock (_lock)
                        {
                            _registry.Remove(server);
                        }
                    }
                }
            }
        }
    }
}