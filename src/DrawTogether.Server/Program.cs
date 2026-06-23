using DrawTogether.Server.Network;

namespace DrawTogether.Server;

internal static class Program
{
    private static void Main(string[] args)
    {
        var port = args.Length > 0 && int.TryParse(args[0], out var parsedPort) ? parsedPort : 5000;

        var server = new TcpServer();
        server.Start(port);

        Console.WriteLine("Press Enter to stop drawing server.");
        Console.ReadLine();

        server.Stop();
    }
}
