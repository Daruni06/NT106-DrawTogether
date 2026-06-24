using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

static class Program
{
    static async Task Main()
    {
        string host = "127.0.0.1";
        int port = 8088;

        var servers = new[] { "127.0.0.1:5000", "127.0.0.1:5001" };

        Console.WriteLine("Registering servers...");
        foreach (var s in servers)
        {
            await SendOneAsync(host, port, JsonSerializer.Serialize(new { type = "REGISTER_SERVER", payload = new { server_address = s } }));
            Console.WriteLine($"Registered {s}");
        }

        Console.WriteLine("Requesting assignments...");
        for (int i = 0; i < 6; i++)
        {
            var resp = await RequestOneAsync(host, port, JsonSerializer.Serialize(new { type = "REQUEST_SERVER", payload = new { } }));
            Console.WriteLine($"Assign {i + 1}: {resp}");
        }
    }

    static async Task SendOneAsync(string host, int port, string json)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        using var stream = client.GetStream();
        var bytes = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
        client.Close();
    }

    static async Task<string> RequestOneAsync(string host, int port, string json)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        using var stream = client.GetStream();
        var bytes = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();

        using var reader = new StreamReader(stream, Encoding.UTF8);
        // wait for a line or timeout
        var readTask = reader.ReadLineAsync();
        var completed = await Task.WhenAny(readTask, Task.Delay(2000));
        if (completed != readTask)
        {
            return "<timeout>";
        }

        var line = await readTask;
        return line ?? string.Empty;
    }
}
