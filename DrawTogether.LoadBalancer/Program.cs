using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DrawTogether.LoadBalancer.Handlers;

namespace DrawTogether.LoadBalancer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int port = 8088;
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine($"[Load Balancer] Dang chay tren cong {port}...");

            // ---> KÍCH HOẠT BÁC SĨ CHẠY NGẦM Ở ĐÂY <---
            _ = LoadBalancerHandler.StartHealthCheckAsync();
            Console.WriteLine("[HealthCheck] He thong giam sat Server da bat!");

            Console.WriteLine("[Load Balancer] San sang dieu phoi Client!\n");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                LoadBalancerHandler handler = new LoadBalancerHandler(client);
                _ = handler.HandleClientAsync();
            }
        }
    }
}