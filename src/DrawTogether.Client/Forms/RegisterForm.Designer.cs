using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace DrawTogether.Client.Forms
{
    partial class RegisterForm
    {
        private Guna2TextBox txtUsername;
        private Guna2TextBox txtEmail;
        private Guna2TextBox txtPassword;
        private Guna2Button btnSignup;
        private Guna2Button btnCancel;
        private Label lblU;
        private Label lblE;
        private Label lblP;

        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges9 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges10 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            txtUsername = new Guna2TextBox();
            txtEmail = new Guna2TextBox();
            txtPassword = new Guna2TextBox();
            btnSignup = new Guna2Button();
            btnCancel = new Guna2Button();
            lblU = new Label();
            lblE = new Label();
            lblP = new Label();
            SuspendLayout();
            // 
            // txtUsername
            // 
            txtUsername.CustomizableEdges = customizableEdges1;
            txtUsername.DefaultText = "";
            txtUsername.Font = new Font("Consolas", 12F);
            txtUsername.Location = new Point(12, 32);
            txtUsername.Margin = new Padding(4, 5, 4, 5);
            txtUsername.Name = "txtUsername";
            txtUsername.PlaceholderText = "Display name";
            txtUsername.SelectedText = "";
            txtUsername.ShadowDecoration.CustomizableEdges = customizableEdges2;
            txtUsername.Size = new Size(380, 55);
            txtUsername.TabIndex = 1;
            // 
            // txtEmail
            // 
            txtEmail.CustomizableEdges = customizableEdges3;
            txtEmail.DefaultText = "";
            txtEmail.Font = new Font("Consolas", 12F);
            txtEmail.Location = new Point(12, 84);
            txtEmail.Margin = new Padding(4, 5, 4, 5);
            txtEmail.Name = "txtEmail";
            txtEmail.PlaceholderText = "you@example.com";
            txtEmail.SelectedText = "";
            txtEmail.ShadowDecoration.CustomizableEdges = customizableEdges4;
            txtEmail.Size = new Size(380, 55);
            txtEmail.TabIndex = 3;
            // 
            // txtPassword
            // 
            txtPassword.CustomizableEdges = customizableEdges5;
            txtPassword.DefaultText = "";
            txtPassword.Font = new Font("Consolas", 12F);
            txtPassword.Location = new Point(12, 136);
            txtPassword.Margin = new Padding(4, 5, 4, 5);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.PlaceholderText = "Password";
            txtPassword.SelectedText = "";
            txtPassword.ShadowDecoration.CustomizableEdges = customizableEdges6;
            txtPassword.Size = new Size(380, 55);
            txtPassword.TabIndex = 5;
            // 
            // btnSignup
            // 
            btnSignup.BorderRadius = 6;
            btnSignup.CustomizableEdges = customizableEdges7;
            btnSignup.FillColor = Color.FromArgb(46, 204, 113);
            btnSignup.Font = new Font("Segoe UI", 12F);
            btnSignup.ForeColor = Color.White;
            btnSignup.Location = new Point(13, 196);
            btnSignup.Name = "btnSignup";
            btnSignup.ShadowDecoration.CustomizableEdges = customizableEdges8;
            btnSignup.Size = new Size(120, 45);
            btnSignup.TabIndex = 6;
            btnSignup.Text = "Sign up";
            btnSignup.Click += BtnSignup_Click;
            // 
            // btnCancel
            // 
            btnCancel.BorderRadius = 6;
            btnCancel.CustomizableEdges = customizableEdges9;
            btnCancel.FillColor = Color.FromArgb(189, 195, 199);
            btnCancel.Font = new Font("Segoe UI", 12F);
            btnCancel.ForeColor = Color.Black;
            btnCancel.Location = new Point(149, 196);
            btnCancel.Name = "btnCancel";
            btnCancel.ShadowDecoration.CustomizableEdges = customizableEdges10;
            btnCancel.Size = new Size(120, 45);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.Click += BtnCancel_Click;
            // 
            // lblU
            // 
            lblU.AutoSize = true;
            lblU.Font = new Font("Segoe UI", 12F);
            lblU.Location = new Point(12, 3);
            lblU.Name = "lblU";
            lblU.Size = new Size(74, 28);
            lblU.TabIndex = 0;
            lblU.Text = "Signup";
            lblU.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblE
            // 
            lblE.AutoSize = true;
            lblE.Location = new Point(12, 64);
            lblE.Name = "lblE";
            lblE.Size = new Size(46, 20);
            lblE.TabIndex = 2;
            lblE.Text = "Email";
            // 
            // lblP
            // 
            lblP.AutoSize = true;
            lblP.Location = new Point(12, 116);
            lblP.Name = "lblP";
            lblP.Size = new Size(70, 20);
            lblP.TabIndex = 4;
            lblP.Text = "Password";
            // 
            // RegisterForm
            // 
            ClientSize = new Size(402, 253);
            Controls.Add(lblU);
            Controls.Add(txtUsername);
            Controls.Add(lblE);
            Controls.Add(txtEmail);
            Controls.Add(lblP);
            Controls.Add(txtPassword);
            Controls.Add(btnSignup);
            Controls.Add(btnCancel);
            Name = "RegisterForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Sign Up";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
