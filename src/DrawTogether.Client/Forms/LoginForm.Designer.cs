using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace DrawTogether.Client.Forms
{
    partial class LoginForm
    {
        private Guna2TextBox txtEmail;
        private Guna2TextBox txtPassword;
        private Guna2Button btnLogin;
        private Guna2Button btnSignup;
        private Guna2Button btnForgot;
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
            txtEmail = new Guna2TextBox();
            txtPassword = new Guna2TextBox();
            btnLogin = new Guna2Button();
            btnSignup = new Guna2Button();
            btnForgot = new Guna2Button();
            lblE = new Label();
            lblP = new Label();
            SuspendLayout();
            // 
            // txtEmail
            // 
            txtEmail.CustomizableEdges = customizableEdges1;
            txtEmail.DefaultText = "";
            txtEmail.Font = new Font("Consolas", 12F);
            txtEmail.Location = new Point(13, 34);
            txtEmail.Margin = new Padding(4, 5, 4, 5);
            txtEmail.Name = "txtEmail";
            txtEmail.PlaceholderText = "you@example.com";
            txtEmail.SelectedText = "";
            txtEmail.ShadowDecoration.CustomizableEdges = customizableEdges2;
            txtEmail.Size = new Size(380, 55);
            txtEmail.TabIndex = 1;
            // 
            // txtPassword
            // 
            txtPassword.CustomizableEdges = customizableEdges3;
            txtPassword.DefaultText = "";
            txtPassword.Font = new Font("Consolas", 12F);
            txtPassword.Location = new Point(12, 113);
            txtPassword.Margin = new Padding(4, 5, 4, 5);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.PlaceholderText = "Password";
            txtPassword.SelectedText = "";
            txtPassword.ShadowDecoration.CustomizableEdges = customizableEdges4;
            txtPassword.Size = new Size(380, 55);
            txtPassword.TabIndex = 3;
            // 
            // btnLogin
            // 
            btnLogin.BorderRadius = 6;
            btnLogin.CustomizableEdges = customizableEdges5;
            btnLogin.FillColor = Color.FromArgb(0, 120, 215);
            btnLogin.Font = new Font("Segoe UI", 12F);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(5, 176);
            btnLogin.Name = "btnLogin";
            btnLogin.ShadowDecoration.CustomizableEdges = customizableEdges6;
            btnLogin.Size = new Size(120, 45);
            btnLogin.TabIndex = 4;
            btnLogin.Text = "Login";
            btnLogin.Click += BtnLogin_Click;
            // 
            // btnSignup
            // 
            btnSignup.BorderRadius = 6;
            btnSignup.CustomizableEdges = customizableEdges7;
            btnSignup.FillColor = Color.FromArgb(46, 204, 113);
            btnSignup.Font = new Font("Segoe UI", 12F);
            btnSignup.ForeColor = Color.White;
            btnSignup.Location = new Point(141, 176);
            btnSignup.Name = "btnSignup";
            btnSignup.ShadowDecoration.CustomizableEdges = customizableEdges8;
            btnSignup.Size = new Size(120, 45);
            btnSignup.TabIndex = 5;
            btnSignup.Text = "Sign up";
            btnSignup.Click += BtnSignup_Click;
            // 
            // btnForgot
            // 
            btnForgot.BorderRadius = 6;
            btnForgot.CustomizableEdges = customizableEdges9;
            btnForgot.FillColor = Color.FromArgb(241, 196, 15);
            btnForgot.Font = new Font("Segoe UI", 12F);
            btnForgot.ForeColor = Color.White;
            btnForgot.Location = new Point(277, 176);
            btnForgot.Name = "btnForgot";
            btnForgot.ShadowDecoration.CustomizableEdges = customizableEdges10;
            btnForgot.Size = new Size(120, 45);
            btnForgot.TabIndex = 6;
            btnForgot.Text = "Forgot";
            btnForgot.Click += BtnForgot_Click;
            // 
            // lblE
            // 
            lblE.AutoSize = true;
            lblE.Font = new Font("Segoe UI", 12F);
            lblE.Location = new Point(12, 5);
            lblE.Name = "lblE";
            lblE.Size = new Size(59, 28);
            lblE.TabIndex = 0;
            lblE.Text = "Email";
            // 
            // lblP
            // 
            lblP.AutoSize = true;
            lblP.Font = new Font("Segoe UI", 12F);
            lblP.Location = new Point(12, 85);
            lblP.Name = "lblP";
            lblP.Size = new Size(93, 28);
            lblP.TabIndex = 2;
            lblP.Text = "Password";
            // 
            // LoginForm
            // 
            ClientSize = new Size(402, 233);
            Controls.Add(lblE);
            Controls.Add(txtEmail);
            Controls.Add(lblP);
            Controls.Add(txtPassword);
            Controls.Add(btnLogin);
            Controls.Add(btnSignup);
            Controls.Add(btnForgot);
            Font = new Font("Segoe UI", 10.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Login";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
