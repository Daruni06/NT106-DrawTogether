using DrawTogether.Server.Configuration;
using DrawTogether.Server.Data;
using DrawTogether.Server.Features;
using DrawTogether.Server.Network;
<<<<<<< Updated upstream
using DrawTogether.Shared.Security;
=======
using DrawTogether.Server.Features;
>>>>>>> Stashed changes

namespace DrawTogether.Server;

internal static class Program
{
    static void Main(string[] args)
    {
<<<<<<< Updated upstream
        var port = args.Length > 0 ? int.Parse(args[0]) : 5000;
=======
        var port = args.Length > 0 && int.TryParse(args[0], out var parsedPort)
            ? parsedPort
            : 5000;
>>>>>>> Stashed changes

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

<<<<<<< Updated upstream
        Console.WriteLine("Server running...");
=======
        ServerRegistrationService
            .RegisterAsync(
                "127.0.0.1",
                8088,
                $"127.0.0.1:{port}")
            .GetAwaiter()
            .GetResult();

        Console.WriteLine("Press Enter to stop drawing server.");
>>>>>>> Stashed changes
        Console.ReadLine();

        server.Stop();
    }
}