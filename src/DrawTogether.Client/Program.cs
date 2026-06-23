using System.Windows.Forms;
using DrawTogether.Client.Forms;
using DrawTogether.Client.Network;

namespace DrawTogether.Client;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var host = args.Length > 0 ? args[0] : "127.0.0.1";
        var port = args.Length > 1 && int.TryParse(args[1], out var parsedPort) ? parsedPort : 5000;
        var roomId = args.Length > 2 ? args[2] : "DEMO";
        var userId = args.Length > 3 ? args[3] : Guid.NewGuid().ToString("N")[..8];
        var displayName = args.Length > 4 ? args[4] : $"User {userId}";

        using var socket = new ClientSocket();
        var form = new DrawingForm(roomId, userId, displayName);
        socket.AttachDrawingForm(form);

        form.Shown += async (_, _) =>
        {
            try
            {
                socket.Connect(host, port);
                await socket.JoinRoomAsync(roomId, userId).ConfigureAwait(true);
                form.Text = $"Draw Together - {roomId} - connected {host}:{port}";
            }
            catch (Exception ex)
            {
                form.Text = $"Draw Together - {roomId} - offline";
                MessageBox.Show(
                    form,
                    $"Khong ket noi duoc server {host}:{port}.\nBan van co the ve local.\n\n{ex.Message}",
                    "Connection failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        };

        Application.Run(form);
    }
}
