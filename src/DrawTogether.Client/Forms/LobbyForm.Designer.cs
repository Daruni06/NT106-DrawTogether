using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace DrawTogether.Client.Forms
{
    partial class LobbyForm
    {
        private PictureBox picAvatar;
        private Label lblUsername;
        private Guna2Button btnCreate;
        private Guna2Button btnJoin;
        private Guna2Button btnLogout;

        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            picAvatar = new PictureBox();
            lblUsername = new Label();
            btnCreate = new Guna2Button();
            btnJoin = new Guna2Button();
            btnLogout = new Guna2Button();
            ((System.ComponentModel.ISupportInitialize)picAvatar).BeginInit();
            SuspendLayout();
            // 
            // picAvatar
            // 
            picAvatar.Anchor = AnchorStyles.Top;
            picAvatar.BackColor = Color.LightGray;
            picAvatar.Location = new Point(311, 71);
            picAvatar.Name = "picAvatar";
            picAvatar.Size = new Size(120, 120);
            picAvatar.TabIndex = 0;
            picAvatar.TabStop = false;
            picAvatar.Paint += PicAvatar_Paint;
            // 
            // lblUsername
            // 
            lblUsername.Anchor = AnchorStyles.Top;
            lblUsername.AutoSize = true;
            lblUsername.Location = new Point(0, 200);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(0, 20);
            lblUsername.TabIndex = 1;
            // 
            // btnCreate
            // 
            btnCreate.BorderRadius = 8;
            btnCreate.CustomizableEdges = customizableEdges1;
            btnCreate.FillColor = Color.FromArgb(46, 204, 113);
            btnCreate.Font = new Font("Segoe UI", 12F);
            btnCreate.ForeColor = Color.White;
            btnCreate.Location = new Point(275, 213);
            btnCreate.Name = "btnCreate";
            btnCreate.ShadowDecoration.CustomizableEdges = customizableEdges2;
            btnCreate.Size = new Size(200, 50);
            btnCreate.TabIndex = 2;
            btnCreate.Text = "Create new room";
            btnCreate.Click += BtnCreate_Click;
            // 
            // btnJoin
            // 
            btnJoin.BorderRadius = 8;
            btnJoin.CustomizableEdges = customizableEdges3;
            btnJoin.FillColor = Color.FromArgb(52, 152, 219);
            btnJoin.Font = new Font("Segoe UI", 12F);
            btnJoin.ForeColor = Color.White;
            btnJoin.Location = new Point(275, 278);
            btnJoin.Name = "btnJoin";
            btnJoin.ShadowDecoration.CustomizableEdges = customizableEdges4;
            btnJoin.Size = new Size(200, 50);
            btnJoin.TabIndex = 3;
            btnJoin.Text = "Join a room";
            btnJoin.Click += BtnJoin_Click;
            // 
            // btnLogout
            // 
            btnLogout.BorderRadius = 8;
            btnLogout.CustomizableEdges = customizableEdges5;
            btnLogout.FillColor = Color.FromArgb(231, 76, 60);
            btnLogout.Font = new Font("Segoe UI", 12F);
            btnLogout.ForeColor = Color.White;
            btnLogout.Location = new Point(275, 342);
            btnLogout.Name = "btnLogout";
            btnLogout.ShadowDecoration.CustomizableEdges = customizableEdges6;
            btnLogout.Size = new Size(200, 50);
            btnLogout.TabIndex = 4;
            btnLogout.Text = "Logout";
            btnLogout.Click += BtnLogout_Click;
            // 
            // LobbyForm
            // 
            ClientSize = new Size(782, 553);
            Controls.Add(picAvatar);
            Controls.Add(lblUsername);
            Controls.Add(btnCreate);
            Controls.Add(btnJoin);
            Controls.Add(btnLogout);
            Name = "LobbyForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Lobby";
            Resize += LobbyForm_Resize;
            ((System.ComponentModel.ISupportInitialize)picAvatar).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
