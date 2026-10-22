namespace StayOnTheLine
{
    partial class LoadingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoadingForm));
            lblSubtitle = new Label();
            lblTitleStayOn = new Label();
            lblTitleTheLine = new Label();
            lblStatus = new Label();
            pnlProgressBarBorder = new Panel();
            pnlProgressBarFill = new Panel();
            lblPercentage = new Label();
            lblPrompt = new Label();
            loadingTimer = new System.Windows.Forms.Timer(components);
            blinkTimer = new System.Windows.Forms.Timer(components);
            pnlProgressBarBorder.SuspendLayout();
            SuspendLayout();
            // 
            // lblSubtitle
            // 
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Font = new Font("IM FELL DW Pica SC", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.FromArgb(145, 148, 140);
            lblSubtitle.Location = new Point(660, 230);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(600, 30);
            lblSubtitle.TabIndex = 0;
            lblSubtitle.Text = "A  N I G H T   D I S P A T C H   S T O R Y";
            lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTitleStayOn
            // 
            lblTitleStayOn.BackColor = Color.Transparent;
            lblTitleStayOn.Font = new Font("IM FELL DW Pica SC", 62F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTitleStayOn.ForeColor = Color.FromArgb(220, 218, 208);
            lblTitleStayOn.Location = new Point(560, 268);
            lblTitleStayOn.Name = "lblTitleStayOn";
            lblTitleStayOn.Size = new Size(800, 115);
            lblTitleStayOn.TabIndex = 1;
            lblTitleStayOn.Text = "Stay On";
            lblTitleStayOn.TextAlign = ContentAlignment.MiddleCenter;
            lblTitleStayOn.Click += lblTitleStayOn_Click;
            // 
            // lblTitleTheLine
            // 
            lblTitleTheLine.BackColor = Color.Transparent;
            lblTitleTheLine.Font = new Font("IM FELL DW Pica", 56F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblTitleTheLine.ForeColor = Color.FromArgb(230, 227, 218);
            lblTitleTheLine.Location = new Point(560, 380);
            lblTitleTheLine.Name = "lblTitleTheLine";
            lblTitleTheLine.Size = new Size(800, 105);
            lblTitleTheLine.TabIndex = 2;
            lblTitleTheLine.Text = "The Line";
            lblTitleTheLine.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblStatus
            // 
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Font = new Font("IM FELL DW Pica SC", 13F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatus.ForeColor = Color.FromArgb(185, 190, 185);
            lblStatus.Location = new Point(560, 860);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(800, 32);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "INITIALIZING EMERGENCY TERMINAL...";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnlProgressBarBorder
            // 
            pnlProgressBarBorder.BackColor = Color.FromArgb(18, 20, 18);
            pnlProgressBarBorder.BorderStyle = BorderStyle.FixedSingle;
            pnlProgressBarBorder.Controls.Add(pnlProgressBarFill);
            pnlProgressBarBorder.Location = new Point(660, 900);
            pnlProgressBarBorder.Name = "pnlProgressBarBorder";
            pnlProgressBarBorder.Size = new Size(600, 12);
            pnlProgressBarBorder.TabIndex = 4;
            // 
            // pnlProgressBarFill
            // 
            pnlProgressBarFill.BackColor = Color.FromArgb(195, 198, 188);
            pnlProgressBarFill.Location = new Point(0, 0);
            pnlProgressBarFill.Name = "pnlProgressBarFill";
            pnlProgressBarFill.Size = new Size(0, 12);
            pnlProgressBarFill.TabIndex = 0;
            // 
            // lblPercentage
            // 
            lblPercentage.BackColor = Color.Transparent;
            lblPercentage.Font = new Font("IM FELL DW Pica SC", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPercentage.ForeColor = Color.FromArgb(145, 150, 145);
            lblPercentage.Location = new Point(860, 920);
            lblPercentage.Name = "lblPercentage";
            lblPercentage.Size = new Size(200, 26);
            lblPercentage.TabIndex = 5;
            lblPercentage.Text = "0%";
            lblPercentage.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblPrompt
            // 
            lblPrompt.BackColor = Color.Transparent;
            lblPrompt.Font = new Font("IM FELL DW Pica SC", 13F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPrompt.ForeColor = Color.FromArgb(235, 238, 230);
            lblPrompt.Location = new Point(610, 955);
            lblPrompt.Name = "lblPrompt";
            lblPrompt.Size = new Size(700, 32);
            lblPrompt.TabIndex = 6;
            lblPrompt.Text = "[ PRESS ANY KEY OR CLICK TO PROCEED ]";
            lblPrompt.TextAlign = ContentAlignment.MiddleCenter;
            lblPrompt.Visible = false;
            // 
            // loadingTimer
            // 
            loadingTimer.Interval = 35;
            loadingTimer.Tick += LoadingTimer_Tick;
            // 
            // blinkTimer
            // 
            blinkTimer.Interval = 550;
            blinkTimer.Tick += BlinkTimer_Tick;
            // 
            // LoadingForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Zoom;
            ClientSize = new Size(1920, 1080);
            Controls.Add(lblPrompt);
            Controls.Add(lblPercentage);
            Controls.Add(pnlProgressBarBorder);
            Controls.Add(lblStatus);
            Controls.Add(lblTitleTheLine);
            Controls.Add(lblTitleStayOn);
            Controls.Add(lblSubtitle);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Name = "LoadingForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Stay On The Line";
            WindowState = FormWindowState.Maximized;
            Load += LoadingForm_Load;
            Click += LoadingForm_Click;
            KeyDown += LoadingForm_KeyDown;
            Resize += LoadingForm_Resize;
            pnlProgressBarBorder.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Label lblSubtitle;
        private Label lblTitleStayOn;
        private Label lblTitleTheLine;
        private Label lblStatus;
        private Panel pnlProgressBarBorder;
        private Panel pnlProgressBarFill;
        private Label lblPercentage;
        private Label lblPrompt;
        private System.Windows.Forms.Timer loadingTimer;
        private System.Windows.Forms.Timer blinkTimer;
    }
}
