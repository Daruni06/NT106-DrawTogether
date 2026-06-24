using System;
using System.Windows.Forms;

namespace DrawTogether.Client.Forms
{
    public partial class OtpForm : Form
    {
        public string EnteredCode => txtCode.Text.Trim();

        public OtpForm(string email, string hintMessage)
        {
            InitializeComponent();
            lblHint.Text = hintMessage;
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
