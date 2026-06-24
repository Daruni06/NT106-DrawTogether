using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace DrawTogether.Client.Forms
{
    partial class ForgotPasswordForm
    {
        private Guna2TextBox txtEmail;
        private Guna2Button btnSend;
        private Label lblE;

        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            txtEmail = new Guna2TextBox();
            btnSend = new Guna2Button();
            lblE = new Label();
            SuspendLayout();
            // 
            // txtEmail
            // 
            txtEmail.CustomizableEdges = customizableEdges1;
            txtEmail.DefaultText = "";
            txtEmail.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtEmail.Location = new Point(12, 32);
            txtEmail.Margin = new Padding(4, 5, 4, 5);
            txtEmail.Name = "txtEmail";
            txtEmail.PlaceholderText = "";
            txtEmail.SelectedText = "";
            txtEmail.ShadowDecoration.CustomizableEdges = customizableEdges2;
            txtEmail.Size = new Size(380, 55);
            txtEmail.TabIndex = 1;
            // 
            // btnSend
            // 
            btnSend.CustomizableEdges = customizableEdges3;
            btnSend.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnSend.ForeColor = Color.White;
            btnSend.Location = new Point(12, 96);
            btnSend.Name = "btnSend";
            btnSend.ShadowDecoration.CustomizableEdges = customizableEdges4;
            btnSend.Size = new Size(131, 45);
            btnSend.TabIndex = 2;
            btnSend.Text = "Send code";
            btnSend.Click += BtnSend_Click;
            // 
            // lblE
            // 
            lblE.AutoSize = true;
            lblE.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblE.Location = new Point(12, 3);
            lblE.Name = "lblE";
            lblE.Size = new Size(59, 28);
            lblE.TabIndex = 0;
            lblE.Text = "Email";
            // 
            // ForgotPasswordForm
            // 
            ClientSize = new Size(402, 153);
            Controls.Add(lblE);
            Controls.Add(txtEmail);
            Controls.Add(btnSend);
            Name = "ForgotPasswordForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Forgot Password";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
