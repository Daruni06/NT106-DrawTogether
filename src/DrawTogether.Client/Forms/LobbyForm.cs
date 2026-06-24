using System;
using System.Drawing;
using System.Windows.Forms;
using DrawTogether.Client.Network;
namespace DrawTogether.Client.Forms
{
	public partial class LobbyForm : Form
	{
		public string Username { get; }

		public LobbyForm(string username)
		{
			Username = username;
			InitializeComponent();
			lblUsername.Text = username;
			LayoutCentered();
		}

		private void LayoutCentered()
		{
			if (picAvatar is not null)
				picAvatar.Left = (ClientSize.Width - picAvatar.Width) / 2;
			lblUsername.Left = (ClientSize.Width - lblUsername.PreferredWidth) / 2;
			lblUsername.Top = picAvatar.Bottom + 12;
			if (btnCreate is not null)
				btnCreate.Left = (ClientSize.Width - btnCreate.Width) / 2;
			if (btnJoin is not null)
				btnJoin.Left = btnCreate.Left;
			if (btnLogout is not null)
				btnLogout.Left = btnCreate.Left;
		}

		private async System.Threading.Tasks.Task OpenDrawingRoomAsync(string roomId)
		{
			var userId = Guid.NewGuid().ToString("N")[..8];
			var displayName = Username;

			using var socket = new DrawTogether.Client.Network.ClientSocket();
			using var form = new DrawTogether.Client.Forms.DrawingForm(roomId, userId, displayName);
			socket.AttachDrawingForm(form);

			form.Shown += async (_, _) =>
			{
				try
				{
                    string? serverAddress =
    await LoadBalancerClient.RequestServerAsync();

                    if (string.IsNullOrWhiteSpace(serverAddress))
                    {
                        throw new Exception(
                            "Khong nhan duoc server tu Load Balancer.");
                    }

                    string[] parts =
                        serverAddress.Split(':');

                    string host = parts[0];
                    int port = int.Parse(parts[1]);

                    socket.Connect(host, port);

                    await socket.JoinRoomAsync(
                        roomId,
                        userId);

                    form.Text =
                        $"Draw Together - {roomId} - connected {serverAddress}";
                }
				catch (Exception ex)
				{
					form.Text = $"Draw Together - {roomId} - offline";
					MessageBox.Show(
						form,
						$"Could not connect to server 127.0.0.1:5000.\nYou can still draw locally.\n\n{ex.Message}",
						"Connection failed",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
				}
			};

			Hide();
			form.ShowDialog(this);
			Show();
		}

		private void BtnCreate_Click(object? sender, EventArgs e)
		{
			_ = OpenDrawingRoomAsync(Guid.NewGuid().ToString("N")[..8]);
		}

		private void BtnJoin_Click(object? sender, EventArgs e)
		{
			var input = Microsoft.VisualBasic.Interaction.InputBox("Enter room id to join:", "Join Room", "DEMO");
			if (string.IsNullOrWhiteSpace(input)) return;
			_ = OpenDrawingRoomAsync(input.Trim());
		}

		private void BtnLogout_Click(object? sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void PicAvatar_Paint(object? sender, PaintEventArgs e)
		{
			using var b = new SolidBrush(Color.DarkGray);
			e.Graphics.FillEllipse(b, 0, 0, picAvatar.Width - 1, picAvatar.Height - 1);
		}

		private void LobbyForm_Resize(object? sender, EventArgs e)
		{
			LayoutCentered();
		}
	}
}
