using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace DrawTogether.Server.Features;

public static class ServerRegistrationService
{
    public static async Task RegisterAsync(
        string loadBalancerHost,
        int loadBalancerPort,
        string serverAddress)
    {
        try
        {
            using var client = new TcpClient();

            await client.ConnectAsync(
                loadBalancerHost,
                loadBalancerPort);

            using var stream = client.GetStream();

            var request = new
            {
                type = "REGISTER_SERVER",
                payload = new
                {
                    server_address = serverAddress
                }
            };

            string json =
                JsonSerializer.Serialize(request);

            byte[] data =
                Encoding.UTF8.GetBytes(json);

            await stream.WriteAsync(data);

            Console.WriteLine(
                $"[REGISTER] Server da dang ky voi LB: {serverAddress}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[REGISTER ERROR] {ex.Message}");
        }
    }
}