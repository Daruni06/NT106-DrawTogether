using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace DrawTogether.Client.Forms
{
    partial class NewPasswordForm
    {
        private Guna2TextBox txtPassword;
        private Guna2Button btnConfirm;
        private Label lblP;

        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            txtPassword = new Guna2TextBox();
            btnConfirm = new Guna2Button();
            lblP = new Label();
            SuspendLayout();
            // 
            // txtPassword
            // 
            txtPassword.CustomizableEdges = customizableEdges1;
            txtPassword.DefaultText = "";
            txtPassword.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtPassword.Location = new Point(12, 33);
            txtPassword.Margin = new Padding(4, 5, 4, 5);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.PlaceholderText = "";
            txtPassword.SelectedText = "";
            txtPassword.ShadowDecoration.CustomizableEdges = customizableEdges2;
            txtPassword.Size = new Size(380, 55);
            txtPassword.TabIndex = 1;
            // 
            // btnConfirm
            // 
            btnConfirm.CustomizableEdges = customizableEdges3;
            btnConfirm.Font = new Font("Segoe UI", 12F);
            btnConfirm.ForeColor = Color.White;
            btnConfirm.Location = new Point(12, 96);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.ShadowDecoration.CustomizableEdges = customizableEdges4;
            btnConfirm.Size = new Size(120, 45);
            btnConfirm.TabIndex = 2;
            btnConfirm.Text = "Confirm";
            btnConfirm.Click += BtnConfirm_Click;
            // 
            // lblP
            // 
            lblP.AutoSize = true;
            lblP.Font = new Font("Segoe UI", 12F);
            lblP.Location = new Point(12, 4);
            lblP.Name = "lblP";
            lblP.Size = new Size(137, 28);
            lblP.TabIndex = 0;
            lblP.Text = "New Password";
            // 
            // NewPasswordForm
            // 
            ClientSize = new Size(402, 153);
            Controls.Add(lblP);
            Controls.Add(txtPassword);
            Controls.Add(btnConfirm);
            Name = "NewPasswordForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Set New Password";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
