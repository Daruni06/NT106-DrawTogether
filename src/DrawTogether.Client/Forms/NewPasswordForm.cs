using System;
using System.Windows.Forms;
using DrawTogether.Client.Auth;

namespace DrawTogether.Client.Forms
{
    public partial class NewPasswordForm : Form
    {
        private readonly AuthClient _auth = Program.AuthClient;
        private readonly string _email;

        public NewPasswordForm(string email)
        {
            _email = email;
            InitializeComponent();
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            var pw = txtPassword.Text;
            if (string.IsNullOrWhiteSpace(pw))
            {
                MessageBox.Show(this, "Please enter a password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _auth.SetNewPassword(_email, pw);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
