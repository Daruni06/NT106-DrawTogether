using System;
using System.Drawing;
using System.Windows.Forms;

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

		private static async System.Threading.Tasks.Task<string?> QueryLoadBalancerAsync(string lbHost, int lbPort, string roomId)
		{
			try
			{
				using var client = new System.Net.Sockets.TcpClient();
				await client.ConnectAsync(lbHost, lbPort).ConfigureAwait(false);
				using var stream = client.GetStream();
				var msg = System.Text.Json.JsonSerializer.Serialize(new { type = "REQUEST_SERVER", payload = new { room_id = roomId } });
				var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
				await stream.WriteAsync(bytes.AsMemory(0, bytes.Length)).ConfigureAwait(false);
				await stream.FlushAsync().ConfigureAwait(false);

				using var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
				var line = await reader.ReadLineAsync().ConfigureAwait(false);
				if (string.IsNullOrWhiteSpace(line)) return null;
				using var doc = System.Text.Json.JsonDocument.Parse(line);
				var server = doc.RootElement.GetProperty("payload").GetProperty("server_address").GetString();
				return server;
			}
			catch
			{
				return null;
			}
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
				string host = "127.0.0.1";
				int port = 5000;
				try
				{
					// Ask load balancer for assigned server (best-effort)
					try
					{
						var assigned = await QueryLoadBalancerAsync("127.0.0.1", 8088, roomId).ConfigureAwait(true);
						if (!string.IsNullOrWhiteSpace(assigned))
						{
							var parts = assigned.Split(':');
							if (parts.Length == 2 && int.TryParse(parts[1], out var p))
							{
								host = parts[0];
								port = p;
							}
						}
					}
					catch { /* ignore LB failures and fallback to localhost */ }

					socket.Connect(host, port);
					await socket.JoinRoomAsync(roomId, userId).ConfigureAwait(true);
					form.Text = $"Draw Together - {roomId} - connected {host}:{port}";
				}
				catch (Exception ex)
				{
					form.Text = $"Draw Together - {roomId} - offline";
					MessageBox.Show(
						form,
						$"Could not connect to server {host}:{port}.\nYou can still draw locally.\n\n{ex.Message}",
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
