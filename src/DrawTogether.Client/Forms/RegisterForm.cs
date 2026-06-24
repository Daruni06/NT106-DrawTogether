using System;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using DrawTogether.Client.Auth;

namespace DrawTogether.Client.Forms
{
	public partial class RegisterForm : Form
	{
		private readonly AuthClient _auth = Program.AuthClient;

		public RegisterForm()
		{
			InitializeComponent();
		}

		private void BtnCancel_Click(object? sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void BtnSignup_Click(object? sender, EventArgs e)
		{
			var username = txtUsername.Text.Trim();
			var email = txtEmail.Text.Trim();
			var password = txtPassword.Text;

			if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
			{
				MessageBox.Show(this, "Please fill all fields.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			_auth.SendSignupOtp(email, username, password, out var otp);
			MessageBox.Show(this, $"Simulated: OTP sent to {email}: {otp}", "OTP Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);

			using var otpForm = new OtpForm(email, $"Enter the 6-digit code sent to {email}");
			if (otpForm.ShowDialog(this) == DialogResult.OK)
			{
				if (_auth.VerifySignupOtp(email, otpForm.EnteredCode))
				{
					_auth.Register(email, username, password);
					MessageBox.Show(this, "Registration successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
					DialogResult = DialogResult.OK;
					Close();
				}
				else
				{
					MessageBox.Show(this, "Invalid OTP.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
	}
}
