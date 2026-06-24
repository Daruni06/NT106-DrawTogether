using DrawTogether.Server.Configuration;
using DrawTogether.Server.Data;
using DrawTogether.Server.Features;
using DrawTogether.Server.Network;
using DrawTogether.Shared.Security;
using System.Net.Sockets;
using System.Text.Json;
using System.Text;

namespace DrawTogether.Server;

internal static class Program
{
    static void Main(string[] args)
    {
        var port = args.Length > 0 ? int.Parse(args[0]) : 5000;

        var dbOptions = new DatabaseOptions
        {
            ConnectionString =
                "Server=localhost;Port=3306;Database=draw_together;User ID=root;Password=YOUR_PASSWORD;"
        };

        var db = new MySqlDatabase(dbOptions);

        var userRepo = new UserRepository(db);
        var roomRepo = new RoomRepository(db);
        var drawRepo = new DrawHistoryRepository(db);
        var chatRepo = new ChatHistoryRepository(db);

        var jwt = new JwtTokenService(new JwtOptions
        {
            SecretKey = "THIS_IS_A_SUPER_LONG_SECRET_KEY_123456789",
            AccessTokenMinutes = 60
        });

        var auth = new AuthService(userRepo, jwt);
        var draw = new DrawService(roomRepo, drawRepo);
        var chat = new ChatService(roomRepo, chatRepo);
        var roomService = new RoomService(roomRepo, drawRepo);

        var router = new MessageRouter(auth, roomService, draw, chat);

        var server = new TcpServer(router);
        server.Start(port);

        // Try to register this drawing server with the load balancer (best-effort)
        try
        {
            RegisterWithLoadBalancer("127.0.0.1", 8088, port);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Load balancer registration failed: {ex.Message}");
        }

        Console.WriteLine("Server running...");
        Console.ReadLine();

        server.Stop();
    }

    private static void RegisterWithLoadBalancer(string lbHost, int lbPort, int serverPort)
    {
        try
        {
            using var client = new TcpClient();
            client.Connect(lbHost, lbPort);
            using var stream = client.GetStream();

            var addr = $"127.0.0.1:{serverPort}";
            var payload = new { type = "REGISTER_SERVER", payload = new { server_address = addr } };
            var json = JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();

            Console.WriteLine($"Registered with load balancer: {addr}");
        }
        catch
        {
            throw;
        }
    }
}