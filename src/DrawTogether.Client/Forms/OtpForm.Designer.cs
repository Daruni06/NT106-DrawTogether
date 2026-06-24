using System.Drawing;
using System.Windows.Forms;

namespace DrawTogether.Client.Forms
{
    partial class OtpForm
    {
        private Label lblHint;
        private TextBox txtCode;
        private Button btnConfirm;

        private void InitializeComponent()
        {
            lblHint = new Label();
            txtCode = new TextBox();
            btnConfirm = new Button();
            SuspendLayout();
            // 
            // lblHint
            // 
            lblHint.AutoSize = true;
            lblHint.Location = new Point(12, 12);
            lblHint.Name = "lblHint";
            lblHint.Size = new Size(0, 20);
            lblHint.TabIndex = 0;
            // 
            // txtCode
            // 
            txtCode.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtCode.Location = new Point(12, 32);
            txtCode.Name = "txtCode";
            txtCode.Size = new Size(358, 34);
            txtCode.TabIndex = 1;
            // 
            // btnConfirm
            // 
            btnConfirm.BackColor = Color.FromArgb(52, 152, 219);
            btnConfirm.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnConfirm.ForeColor = SystemColors.ControlLightLight;
            btnConfirm.Location = new Point(10, 72);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.Size = new Size(102, 39);
            btnConfirm.TabIndex = 2;
            btnConfirm.Text = "Confirm";
            btnConfirm.UseVisualStyleBackColor = false;
            btnConfirm.Click += BtnConfirm_Click;
            // 
            // OtpForm
            // 
            ClientSize = new Size(382, 113);
            Controls.Add(lblHint);
            Controls.Add(txtCode);
            Controls.Add(btnConfirm);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Name = "OtpForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Enter OTP";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
