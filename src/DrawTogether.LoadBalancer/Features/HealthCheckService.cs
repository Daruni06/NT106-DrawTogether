using System.Net.Sockets;

namespace DrawTogether.LoadBalancer.Features
{
    public class HealthCheckService
    {
        public async Task<bool> PingServerAsync(
            string serverAddress)
        {
            try
            {
                string[] parts =
                    serverAddress.Split(':');

                using TcpClient client =
                    new TcpClient();

                Task connectTask =
                    client.ConnectAsync(
                        parts[0],
                        int.Parse(parts[1]));

                if (await Task.WhenAny(
                        connectTask,
                        Task.Delay(2000))
                    != connectTask)
                {
                    return false;
                }

                return client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}