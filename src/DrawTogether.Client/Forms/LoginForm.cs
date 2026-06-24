using System;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using DrawTogether.Client.Auth;

namespace DrawTogether.Client.Forms
{
	public partial class LoginForm : Form
	{
		private readonly AuthClient _auth = Program.AuthClient;

		public string LoggedInUsername { get; private set; } = string.Empty;

		public LoginForm()
		{
			InitializeComponent();
		}

		private void BtnLogin_Click(object? sender, EventArgs e)
		{
			var email = txtEmail.Text.Trim();
			var pw = txtPassword.Text;
			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pw))
			{
				MessageBox.Show(this, "Please enter email and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (_auth.Login(email, pw, out var username))
			{
				LoggedInUsername = username;
				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				MessageBox.Show(this, "Invalid credentials.", "Login failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnSignup_Click(object? sender, EventArgs e)
		{
			using var reg = new RegisterForm();
			reg.ShowDialog(this);
		}

		private void BtnForgot_Click(object? sender, EventArgs e)
		{
			using var forgot = new ForgotPasswordForm();
			forgot.ShowDialog(this);
		}
	}
}
