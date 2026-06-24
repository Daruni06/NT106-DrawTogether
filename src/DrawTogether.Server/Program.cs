using DrawTogether.Server.Configuration;
using DrawTogether.Server.Data;
using DrawTogether.Server.Features;
using DrawTogether.Server.Network;
using DrawTogether.Shared.Security;

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

        Console.WriteLine("Server running...");
        Console.ReadLine();

        server.Stop();
    }
}