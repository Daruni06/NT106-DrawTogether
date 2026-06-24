using System;
using System.Windows.Forms;
using DrawTogether.Client.Auth;

namespace DrawTogether.Client.Forms
{
    public partial class ForgotPasswordForm : Form
    {
        private readonly AuthClient _auth = Program.AuthClient;

        public ForgotPasswordForm()
        {
            InitializeComponent();
        }

        private void BtnSend_Click(object? sender, EventArgs e)
        {
            var email = txtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show(this, "Please enter your email.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ok = _auth.SendPasswordResetOtp(email, out var otp);
            MessageBox.Show(this, $"Simulated: OTP sent to {email}: {otp}", "OTP Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // show OTP input
            using var otpForm = new OtpForm(email, $"Enter the 6-digit code sent to {email}");
            if (otpForm.ShowDialog(this) == DialogResult.OK)
            {
                if (!_auth.VerifyPasswordResetOtp(email, otpForm.EnteredCode))
                {
                    MessageBox.Show(this, "Invalid OTP.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using var newPassForm = new NewPasswordForm(email);
                if (newPassForm.ShowDialog(this) == DialogResult.OK)
                {
                    MessageBox.Show(this, "Password reset. Please login.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
        }
    }
}
