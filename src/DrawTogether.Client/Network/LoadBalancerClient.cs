using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace DrawTogether.Client.Network;

public static class LoadBalancerClient
{
    public static async Task<string?> RequestServerAsync(
        string host = "127.0.0.1",
        int port = 8088)
    {
        try
        {
            using var client = new TcpClient();

            await client.ConnectAsync(host, port);

            using var stream = client.GetStream();

            var request = new
            {
                type = "REQUEST_SERVER"
            };

            string json =
                JsonSerializer.Serialize(request);

            byte[] data =
                Encoding.UTF8.GetBytes(json);

            await stream.WriteAsync(data);

            byte[] buffer = new byte[1024];

            int bytesRead =
                await stream.ReadAsync(buffer);

            string responseJson =
                Encoding.UTF8.GetString(
                    buffer,
                    0,
                    bytesRead);

            using JsonDocument doc =
                JsonDocument.Parse(responseJson);

            return doc.RootElement
                .GetProperty("payload")
                .GetProperty("server_address")
                .GetString();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[LB CLIENT] {ex.Message}");

            return null;
        }
    }
}