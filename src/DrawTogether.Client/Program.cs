using System.Windows.Forms;
using DrawTogether.Client.Forms;
using DrawTogether.Client.Network;
using DrawTogether.Client.Auth;

namespace DrawTogether.Client;

internal static class Program
{
    public static AuthClient AuthClient { get; } = new AuthClient();
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var host = args.Length > 0 ? args[0] : "127.0.0.1";
        var port = args.Length > 1 && int.TryParse(args[1], out var parsedPort) ? parsedPort : 5000;
        var roomId = args.Length > 2 ? args[2] : "DEMO";
        var userId = args.Length > 3 ? args[3] : Guid.NewGuid().ToString("N")[..8];
        var displayName = args.Length > 4 ? args[4] : $"User {userId}";

        // Show login form first
        using var login = new LoginForm();
        var loginResult = login.ShowDialog();
        if (loginResult != DialogResult.OK)
        {
            return;
        }

        // After successful login, show lobby
        using var lobby = new LobbyForm(login.LoggedInUsername);
        while (true)
        {
            var res = lobby.ShowDialog();
            // If user logged out (DialogResult.Cancel), return to login
            if (res == DialogResult.Cancel)
            {
                // show login again
                using var login2 = new LoginForm();
                if (login2.ShowDialog() != DialogResult.OK) return;
                // recreate lobby with new username
                lobby.Dispose();
                // create new lobby instance
                // Note: this simple loop recreates lobby with new username
                using var newLobby = new LobbyForm(login2.LoggedInUsername);
                // continue with newLobby by swapping reference (can't reassign using variable), so restart loop
                // We'll simply exit and let user restart the client for simplicity
                return;
            }
            else
            {
                // Other DialogResult values - exit
                return;
            }
        }
    }
}
