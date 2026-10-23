namespace StayOnTheLine
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            pbBackground = new PictureBox();
            lblPlaceholder = new Label();
            lblHeaderLeft = new Label();
            lblHeaderRight = new Label();
            lblKicker = new Label();
            lblTitle1 = new Label();
            lblTitle2 = new Label();
            lblTagline = new Label();
            lblNewShift = new Label();
            lblContinue = new Label();
            lblQuit = new Label();
            lblFooterLeft = new Label();
            lblFooterRight = new Label();
            ((System.ComponentModel.ISupportInitialize)pbBackground).BeginInit();
            pbBackground.SuspendLayout();
            SuspendLayout();
            // 
            // pbBackground
            // 
            pbBackground.BackColor = Color.FromArgb(20, 20, 20);
            pbBackground.Controls.Add(lblPlaceholder);
            pbBackground.Controls.Add(lblHeaderLeft);
            pbBackground.Controls.Add(lblHeaderRight);
            pbBackground.Controls.Add(lblKicker);
            pbBackground.Controls.Add(lblTitle1);
            pbBackground.Controls.Add(lblTitle2);
            pbBackground.Controls.Add(lblTagline);
            pbBackground.Controls.Add(lblNewShift);
            pbBackground.Controls.Add(lblContinue);
            pbBackground.Controls.Add(lblQuit);
            pbBackground.Controls.Add(lblFooterLeft);
            pbBackground.Controls.Add(lblFooterRight);
            pbBackground.Dock = DockStyle.Fill;
            pbBackground.Image = (Image)resources.GetObject("pbBackground.Image");
            pbBackground.Location = new Point(0, 0);
            pbBackground.Name = "pbBackground";
            pbBackground.Size = new Size(1920, 1080);
            pbBackground.SizeMode = PictureBoxSizeMode.StretchImage;
            pbBackground.TabIndex = 0;
            pbBackground.TabStop = false;
            pbBackground.WaitOnLoad = true;
            // 
            // lblPlaceholder
            // 
            lblPlaceholder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblPlaceholder.BackColor = Color.Transparent;
            lblPlaceholder.Font = new Font("Consolas", 14F);
            lblPlaceholder.ForeColor = Color.FromArgb(70, 70, 68);
            lblPlaceholder.Location = new Point(1000, 480);
            lblPlaceholder.Name = "lblPlaceholder";
            lblPlaceholder.Size = new Size(800, 80);
            lblPlaceholder.TabIndex = 0;
            lblPlaceholder.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblHeaderLeft
            // 
            lblHeaderLeft.AutoSize = true;
            lblHeaderLeft.BackColor = Color.Transparent;
            lblHeaderLeft.Font = new Font("IM FELL DW Pica", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblHeaderLeft.ForeColor = Color.FromArgb(110, 110, 106);
            lblHeaderLeft.Location = new Point(67, 95);
            lblHeaderLeft.Name = "lblHeaderLeft";
            lblHeaderLeft.Size = new Size(399, 19);
            lblHeaderLeft.TabIndex = 1;
            lblHeaderLeft.Text = "THE NIGHT LINE: SOMEONE IS ALWAYS LISTENING";
            // 
            // lblHeaderRight
            // 
            lblHeaderRight.BackColor = Color.Transparent;
            lblHeaderRight.Font = new Font("IM FELL DW Pica", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblHeaderRight.ForeColor = Color.FromArgb(110, 110, 106);
            lblHeaderRight.Location = new Point(1453, 95);
            lblHeaderRight.Name = "lblHeaderRight";
            lblHeaderRight.Size = new Size(400, 22);
            lblHeaderRight.TabIndex = 2;
            lblHeaderRight.Text = "TERMINAL 04";
            lblHeaderRight.TextAlign = ContentAlignment.TopRight;
            // 
            // lblKicker
            // 
            lblKicker.AutoSize = true;
            lblKicker.BackColor = Color.Transparent;
            lblKicker.Font = new Font("IM FELL DW Pica", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblKicker.ForeColor = Color.FromArgb(140, 138, 130);
            lblKicker.Location = new Point(173, 149);
            lblKicker.Name = "lblKicker";
            lblKicker.Size = new Size(317, 22);
            lblKicker.TabIndex = 3;
            lblKicker.Text = "A  N I G H T   D I S P A T C H   S T O R Y";
            lblKicker.Click += lblKicker_Click;
            // 
            // lblTitle1
            // 
            lblTitle1.AutoSize = true;
            lblTitle1.BackColor = Color.Transparent;
            lblTitle1.Font = new Font("IM FELL DW Pica", 66F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle1.ForeColor = Color.FromArgb(214, 212, 204);
            lblTitle1.Location = new Point(165, 165);
            lblTitle1.Name = "lblTitle1";
            lblTitle1.Size = new Size(416, 137);
            lblTitle1.TabIndex = 4;
            lblTitle1.Text = "Stay On";
            lblTitle1.Click += lblTitle1_Click;
            // 
            // lblTitle2
            // 
            lblTitle2.AutoSize = true;
            lblTitle2.BackColor = Color.Transparent;
            lblTitle2.Font = new Font("IM FELL DW Pica", 70.2000046F, FontStyle.Italic);
            lblTitle2.ForeColor = Color.FromArgb(214, 212, 204);
            lblTitle2.Location = new Point(190, 285);
            lblTitle2.Name = "lblTitle2";
            lblTitle2.Size = new Size(474, 148);
            lblTitle2.TabIndex = 5;
            lblTitle2.Text = "The Line";
            // 
            // lblTagline
            // 
            lblTagline.AutoSize = true;
            lblTagline.BackColor = Color.Transparent;
            lblTagline.Font = new Font("IM FELL DW Pica", 16.2F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblTagline.ForeColor = Color.FromArgb(130, 128, 122);
            lblTagline.Location = new Point(178, 455);
            lblTagline.Name = "lblTagline";
            lblTagline.Size = new Size(426, 35);
            lblTagline.TabIndex = 6;
            lblTagline.Text = "“There are eight  calls on tonight’s list.”";
            lblTagline.Click += lblTagline_Click;
            // 
            // lblNewShift
            // 
            lblNewShift.AutoSize = true;
            lblNewShift.BackColor = Color.Transparent;
            lblNewShift.Cursor = Cursors.Hand;
            lblNewShift.Font = new Font("IM FELL DW Pica", 28.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblNewShift.ForeColor = Color.FromArgb(200, 198, 190);
            lblNewShift.Location = new Point(190, 540);
            lblNewShift.Name = "lblNewShift";
            lblNewShift.Size = new Size(206, 59);
            lblNewShift.TabIndex = 7;
            lblNewShift.Text = "New shift";
            lblNewShift.Click += MenuItem_Click;
            lblNewShift.MouseEnter += MenuItem_MouseEnter;
            lblNewShift.MouseLeave += MenuItem_MouseLeave;
            // 
            // lblContinue
            // 
            lblContinue.AutoSize = true;
            lblContinue.BackColor = Color.Transparent;
            lblContinue.Cursor = Cursors.Hand;
            lblContinue.Font = new Font("IM FELL DW Pica", 28.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblContinue.ForeColor = Color.FromArgb(200, 198, 190);
            lblContinue.Location = new Point(190, 637);
            lblContinue.Name = "lblContinue";
            lblContinue.Size = new Size(198, 59);
            lblContinue.TabIndex = 8;
            lblContinue.Text = "Continue";
            lblContinue.Click += MenuItem_Click;
            lblContinue.MouseEnter += MenuItem_MouseEnter;
            lblContinue.MouseLeave += MenuItem_MouseLeave;
            // 
            // lblQuit
            // 
            lblQuit.AutoSize = true;
            lblQuit.BackColor = Color.Transparent;
            lblQuit.Cursor = Cursors.Hand;
            lblQuit.Font = new Font("IM FELL DW Pica", 28.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblQuit.ForeColor = Color.FromArgb(120, 118, 112);
            lblQuit.Location = new Point(190, 734);
            lblQuit.Name = "lblQuit";
            lblQuit.Size = new Size(110, 59);
            lblQuit.TabIndex = 9;
            lblQuit.Text = "Quit";
            lblQuit.Click += MenuItem_Click;
            lblQuit.MouseEnter += MenuItem_MouseEnter;
            lblQuit.MouseLeave += MenuItem_MouseLeave;
            // 
            // lblFooterLeft
            // 
            lblFooterLeft.AutoSize = true;
            lblFooterLeft.BackColor = Color.Transparent;
            lblFooterLeft.Font = new Font("IM FELL DW Pica", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblFooterLeft.ForeColor = Color.FromArgb(110, 110, 106);
            lblFooterLeft.Location = new Point(67, 985);
            lblFooterLeft.Name = "lblFooterLeft";
            lblFooterLeft.Size = new Size(328, 19);
            lblFooterLeft.TabIndex = 10;
            lblFooterLeft.Text = "Night Watch Emergency Services - November 9. 1996";
            // 
            // lblFooterRight
            // 
            lblFooterRight.BackColor = Color.Transparent;
            lblFooterRight.Font = new Font("IM FELL DW Pica", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblFooterRight.ForeColor = Color.FromArgb(110, 110, 106);
            lblFooterRight.Location = new Point(1453, 985);
            lblFooterRight.Name = "lblFooterRight";
            lblFooterRight.Size = new Size(400, 22);
            lblFooterRight.TabIndex = 11;
            lblFooterRight.Text = "Connection: Local";
            lblFooterRight.TextAlign = ContentAlignment.TopRight;
            // 
            // MainForm
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            ClientSize = new Size(1920, 1080);
            Controls.Add(pbBackground);
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            Name = "MainForm";
            StartPosition = FormStartPosition.Manual;
            Text = "Stay On The Line";
            WindowState = FormWindowState.Maximized;
            Load += MainMenu_Load;
            KeyDown += MainMenu_KeyDown;
            ((System.ComponentModel.ISupportInitialize)pbBackground).EndInit();
            pbBackground.ResumeLayout(false);
            pbBackground.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.PictureBox pbBackground;
        private System.Windows.Forms.Label lblPlaceholder;
        private System.Windows.Forms.Label lblHeaderLeft;
        private System.Windows.Forms.Label lblHeaderRight;
        private System.Windows.Forms.Label lblKicker;
        private System.Windows.Forms.Label lblTitle1;
        private System.Windows.Forms.Label lblTitle2;
        private System.Windows.Forms.Label lblTagline;
        private System.Windows.Forms.Label lblNewShift;
        private System.Windows.Forms.Label lblContinue;
        private System.Windows.Forms.Label lblQuit;
        private System.Windows.Forms.Label lblFooterLeft;
        private System.Windows.Forms.Label lblFooterRight;
    }
}