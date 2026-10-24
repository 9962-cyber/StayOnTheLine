namespace StayOnTheLine.Forms
{
    partial class OperatorArchiveForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlParchmentLedger = new Panel();
            lblHeaderStartGame = new Label();
            lblHeaderSubtitle = new Label();
            lblClose = new Label();
            lblSectionArchive = new Label();
            lblSectionChoose = new Label();
            lblQuote = new Label();
            pnlSlotsContainer = new Panel();
            pnlSlot1 = new Panel();
            lblSlot1Num = new Label();
            lblSlot1Name = new Label();
            lblSlot1Details = new Label();
            lblSlot1Time = new Label();
            lblSlot1Delete = new Label();
            pnlSlot2 = new Panel();
            lblSlot2Num = new Label();
            lblSlot2Name = new Label();
            lblSlot2Details = new Label();
            lblSlot2Time = new Label();
            lblSlot2Delete = new Label();
            pnlSlot3 = new Panel();
            lblSlot3Num = new Label();
            lblSlot3Name = new Label();
            lblSlot3Details = new Label();
            lblSlot3Time = new Label();
            lblSlot3Delete = new Label();
            pnlParchmentLedger.SuspendLayout();
            pnlSlotsContainer.SuspendLayout();
            pnlSlot1.SuspendLayout();
            pnlSlot2.SuspendLayout();
            pnlSlot3.SuspendLayout();
            SuspendLayout();
            // 
            // pnlParchmentLedger
            // 
            pnlParchmentLedger.BackColor = Color.FromArgb(218, 208, 185);
            pnlParchmentLedger.BackgroundImageLayout = ImageLayout.Stretch;
            pnlParchmentLedger.Controls.Add(lblHeaderStartGame);
            pnlParchmentLedger.Controls.Add(lblHeaderSubtitle);
            pnlParchmentLedger.Controls.Add(lblClose);
            pnlParchmentLedger.Controls.Add(lblSectionArchive);
            pnlParchmentLedger.Controls.Add(lblSectionChoose);
            pnlParchmentLedger.Controls.Add(lblQuote);
            pnlParchmentLedger.Controls.Add(pnlSlotsContainer);
            pnlParchmentLedger.Location = new Point(380, 150);
            pnlParchmentLedger.Name = "pnlParchmentLedger";
            pnlParchmentLedger.Size = new Size(1160, 760);
            pnlParchmentLedger.TabIndex = 0;
            // 
            // lblHeaderStartGame
            // 
            lblHeaderStartGame.BackColor = Color.Transparent;
            lblHeaderStartGame.Font = new Font("IM FELL DW Pica SC", 30F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderStartGame.ForeColor = Color.FromArgb(16, 10, 4);
            lblHeaderStartGame.Location = new Point(180, 80);
            lblHeaderStartGame.Name = "lblHeaderStartGame";
            lblHeaderStartGame.Size = new Size(800, 48);
            lblHeaderStartGame.TabIndex = 1;
            lblHeaderStartGame.Text = "START GAME";
            lblHeaderStartGame.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblHeaderSubtitle
            // 
            lblHeaderSubtitle.BackColor = Color.Transparent;
            lblHeaderSubtitle.Font = new Font("IM FELL DW Pica", 12.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblHeaderSubtitle.ForeColor = Color.FromArgb(48, 32, 18);
            lblHeaderSubtitle.Location = new Point(180, 130);
            lblHeaderSubtitle.Name = "lblHeaderSubtitle";
            lblHeaderSubtitle.Size = new Size(800, 26);
            lblHeaderSubtitle.TabIndex = 2;
            lblHeaderSubtitle.Text = "The line has been waiting for you.";
            lblHeaderSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblClose
            // 
            lblClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblClose.BackColor = Color.Transparent;
            lblClose.Cursor = Cursors.Hand;
            lblClose.Font = new Font("Segoe UI Symbol", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblClose.ForeColor = Color.FromArgb(40, 25, 12);
            lblClose.Location = new Point(1036, 70);
            lblClose.Name = "lblClose";
            lblClose.Size = new Size(48, 48);
            lblClose.TabIndex = 3;
            lblClose.Text = "✕";
            lblClose.TextAlign = ContentAlignment.MiddleCenter;
            lblClose.Click += lblClose_Click;
            // 
            // lblSectionArchive
            // 
            lblSectionArchive.AutoSize = true;
            lblSectionArchive.BackColor = Color.Transparent;
            lblSectionArchive.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSectionArchive.ForeColor = Color.FromArgb(135, 45, 30);
            lblSectionArchive.Location = new Point(85, 170);
            lblSectionArchive.Name = "lblSectionArchive";
            lblSectionArchive.Size = new Size(335, 22);
            lblSectionArchive.TabIndex = 4;
            lblSectionArchive.Text = "RECOVERED OPERATOR ARCHIVE";
            // 
            // lblSectionChoose
            // 
            lblSectionChoose.AutoSize = false;
            lblSectionChoose.BackColor = Color.Transparent;
            lblSectionChoose.Font = new Font("IM FELL DW Pica SC", 22F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSectionChoose.ForeColor = Color.FromArgb(18, 12, 6);
            lblSectionChoose.Location = new Point(82, 192);
            lblSectionChoose.Name = "lblSectionChoose";
            lblSectionChoose.Size = new Size(520, 44);
            lblSectionChoose.TabIndex = 5;
            lblSectionChoose.Text = "CHOOSE WHO ANSWERS";
            lblSectionChoose.Visible = true;
            // 
            // lblQuote
            // 
            lblQuote.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblQuote.BackColor = Color.Transparent;
            lblQuote.Font = new Font("IM FELL DW Pica", 11.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblQuote.ForeColor = Color.FromArgb(40, 26, 16);
            lblQuote.Location = new Point(520, 204);
            lblQuote.Name = "lblQuote";
            lblQuote.Size = new Size(540, 26);
            lblQuote.TabIndex = 6;
            lblQuote.Text = "Every voice is recorded. Not every voice belongs to the living.";
            lblQuote.TextAlign = ContentAlignment.TopRight;
            // 
            // pnlSlotsContainer
            // 
            pnlSlotsContainer.BackColor = Color.Transparent;
            pnlSlotsContainer.Controls.Add(pnlSlot1);
            pnlSlotsContainer.Controls.Add(pnlSlot2);
            pnlSlotsContainer.Controls.Add(pnlSlot3);
            pnlSlotsContainer.Location = new Point(80, 250);
            pnlSlotsContainer.Name = "pnlSlotsContainer";
            pnlSlotsContainer.Size = new Size(1000, 420);
            pnlSlotsContainer.TabIndex = 7;
            // 
            // pnlSlot1
            // 
            pnlSlot1.BackColor = Color.Transparent;
            pnlSlot1.Controls.Add(lblSlot1Num);
            pnlSlot1.Controls.Add(lblSlot1Name);
            pnlSlot1.Controls.Add(lblSlot1Details);
            pnlSlot1.Controls.Add(lblSlot1Time);
            pnlSlot1.Controls.Add(lblSlot1Delete);
            pnlSlot1.Cursor = Cursors.Hand;
            pnlSlot1.Location = new Point(0, 0);
            pnlSlot1.Name = "pnlSlot1";
            pnlSlot1.Size = new Size(1000, 135);
            pnlSlot1.TabIndex = 0;
            // 
            // lblSlot1Num
            // 
            lblSlot1Num.BackColor = Color.Transparent;
            lblSlot1Num.Font = new Font("IM FELL DW Pica SC", 22F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot1Num.ForeColor = Color.FromArgb(145, 65, 45);
            lblSlot1Num.Location = new Point(20, 22);
            lblSlot1Num.Name = "lblSlot1Num";
            lblSlot1Num.Size = new Size(72, 46);
            lblSlot1Num.TabIndex = 0;
            lblSlot1Num.Text = "01";
            lblSlot1Num.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSlot1Name
            // 
            lblSlot1Name.BackColor = Color.Transparent;
            lblSlot1Name.Font = new Font("IM FELL DW Pica SC", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot1Name.ForeColor = Color.FromArgb(16, 10, 4);
            lblSlot1Name.Location = new Point(98, 20);
            lblSlot1Name.Name = "lblSlot1Name";
            lblSlot1Name.Size = new Size(580, 42);
            lblSlot1Name.TabIndex = 1;
            lblSlot1Name.Text = "+ CREATE NEW SHIFT";
            lblSlot1Name.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSlot1Details
            // 
            lblSlot1Details.BackColor = Color.Transparent;
            lblSlot1Details.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot1Details.ForeColor = Color.FromArgb(140, 70, 55);
            lblSlot1Details.Location = new Point(98, 68);
            lblSlot1Details.Name = "lblSlot1Details";
            lblSlot1Details.Size = new Size(620, 28);
            lblSlot1Details.TabIndex = 2;
            lblSlot1Details.Text = "EMPTY OPERATOR ARCHIVE";
            lblSlot1Details.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSlot1Time
            // 
            lblSlot1Time.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSlot1Time.BackColor = Color.Transparent;
            lblSlot1Time.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot1Time.ForeColor = Color.FromArgb(55, 38, 22);
            lblSlot1Time.Location = new Point(660, 26);
            lblSlot1Time.Name = "lblSlot1Time";
            lblSlot1Time.Size = new Size(205, 30);
            lblSlot1Time.TabIndex = 3;
            lblSlot1Time.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblSlot1Delete
            // 
            lblSlot1Delete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSlot1Delete.BackColor = Color.Transparent;
            lblSlot1Delete.Cursor = Cursors.Hand;
            lblSlot1Delete.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot1Delete.ForeColor = Color.FromArgb(170, 25, 25);
            lblSlot1Delete.Location = new Point(885, 22);
            lblSlot1Delete.Name = "lblSlot1Delete";
            lblSlot1Delete.Size = new Size(95, 36);
            lblSlot1Delete.TabIndex = 4;
            lblSlot1Delete.Text = "DELETE";
            lblSlot1Delete.TextAlign = ContentAlignment.MiddleCenter;
            lblSlot1Delete.Visible = false;
            // 
            // pnlSlot2
            // 
            pnlSlot2.BackColor = Color.Transparent;
            pnlSlot2.Controls.Add(lblSlot2Num);
            pnlSlot2.Controls.Add(lblSlot2Name);
            pnlSlot2.Controls.Add(lblSlot2Details);
            pnlSlot2.Controls.Add(lblSlot2Time);
            pnlSlot2.Controls.Add(lblSlot2Delete);
            pnlSlot2.Cursor = Cursors.Hand;
            pnlSlot2.Location = new Point(0, 138);
            pnlSlot2.Name = "pnlSlot2";
            pnlSlot2.Size = new Size(1000, 135);
            pnlSlot2.TabIndex = 1;
            // 
            // lblSlot2Num
            // 
            lblSlot2Num.BackColor = Color.Transparent;
            lblSlot2Num.Font = new Font("IM FELL DW Pica SC", 22F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot2Num.ForeColor = Color.FromArgb(145, 65, 45);
            lblSlot2Num.Location = new Point(20, 22);
            lblSlot2Num.Name = "lblSlot2Num";
            lblSlot2Num.Size = new Size(72, 46);
            lblSlot2Num.TabIndex = 0;
            lblSlot2Num.Text = "02";
            lblSlot2Num.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSlot2Name
            // 
            lblSlot2Name.BackColor = Color.Transparent;
            lblSlot2Name.Font = new Font("IM FELL DW Pica SC", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot2Name.ForeColor = Color.FromArgb(16, 10, 4);
            lblSlot2Name.Location = new Point(98, 20);
            lblSlot2Name.Name = "lblSlot2Name";
            lblSlot2Name.Size = new Size(580, 42);
            lblSlot2Name.TabIndex = 1;
            lblSlot2Name.Text = "+ CREATE NEW SHIFT";
            lblSlot2Name.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSlot2Details
            // 
            lblSlot2Details.BackColor = Color.Transparent;
            lblSlot2Details.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot2Details.ForeColor = Color.FromArgb(140, 70, 55);
            lblSlot2Details.Location = new Point(98, 68);
            lblSlot2Details.Name = "lblSlot2Details";
            lblSlot2Details.Size = new Size(620, 28);
            lblSlot2Details.TabIndex = 2;
            lblSlot2Details.Text = "EMPTY OPERATOR ARCHIVE";
            lblSlot2Details.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSlot2Time
            // 
            lblSlot2Time.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSlot2Time.BackColor = Color.Transparent;
            lblSlot2Time.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot2Time.ForeColor = Color.FromArgb(55, 38, 22);
            lblSlot2Time.Location = new Point(660, 26);
            lblSlot2Time.Name = "lblSlot2Time";
            lblSlot2Time.Size = new Size(205, 30);
            lblSlot2Time.TabIndex = 3;
            lblSlot2Time.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblSlot2Delete
            // 
            lblSlot2Delete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSlot2Delete.BackColor = Color.Transparent;
            lblSlot2Delete.Cursor = Cursors.Hand;
            lblSlot2Delete.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot2Delete.ForeColor = Color.FromArgb(170, 25, 25);
            lblSlot2Delete.Location = new Point(885, 22);
            lblSlot2Delete.Name = "lblSlot2Delete";
            lblSlot2Delete.Size = new Size(95, 36);
            lblSlot2Delete.TabIndex = 4;
            lblSlot2Delete.Text = "DELETE";
            lblSlot2Delete.TextAlign = ContentAlignment.MiddleCenter;
            lblSlot2Delete.Visible = false;
            // 
            // pnlSlot3
            // 
            pnlSlot3.BackColor = Color.Transparent;
            pnlSlot3.Controls.Add(lblSlot3Num);
            pnlSlot3.Controls.Add(lblSlot3Name);
            pnlSlot3.Controls.Add(lblSlot3Details);
            pnlSlot3.Controls.Add(lblSlot3Time);
            pnlSlot3.Controls.Add(lblSlot3Delete);
            pnlSlot3.Cursor = Cursors.Hand;
            pnlSlot3.Location = new Point(0, 276);
            pnlSlot3.Name = "pnlSlot3";
            pnlSlot3.Size = new Size(1000, 135);
            pnlSlot3.TabIndex = 2;
            // 
            // lblSlot3Num
            // 
            lblSlot3Num.BackColor = Color.Transparent;
            lblSlot3Num.Font = new Font("IM FELL DW Pica SC", 22F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot3Num.ForeColor = Color.FromArgb(145, 65, 45);
            lblSlot3Num.Location = new Point(20, 22);
            lblSlot3Num.Name = "lblSlot3Num";
            lblSlot3Num.Size = new Size(72, 46);
            lblSlot3Num.TabIndex = 0;
            lblSlot3Num.Text = "03";
            lblSlot3Num.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSlot3Name
            // 
            lblSlot3Name.BackColor = Color.Transparent;
            lblSlot3Name.Font = new Font("IM FELL DW Pica SC", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot3Name.ForeColor = Color.FromArgb(16, 10, 4);
            lblSlot3Name.Location = new Point(98, 20);
            lblSlot3Name.Name = "lblSlot3Name";
            lblSlot3Name.Size = new Size(580, 42);
            lblSlot3Name.TabIndex = 1;
            lblSlot3Name.Text = "+ CREATE NEW SHIFT";
            lblSlot3Name.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSlot3Details
            // 
            lblSlot3Details.BackColor = Color.Transparent;
            lblSlot3Details.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot3Details.ForeColor = Color.FromArgb(140, 70, 55);
            lblSlot3Details.Location = new Point(98, 68);
            lblSlot3Details.Name = "lblSlot3Details";
            lblSlot3Details.Size = new Size(620, 28);
            lblSlot3Details.TabIndex = 2;
            lblSlot3Details.Text = "EMPTY OPERATOR ARCHIVE";
            lblSlot3Details.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSlot3Time
            // 
            lblSlot3Time.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSlot3Time.BackColor = Color.Transparent;
            lblSlot3Time.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot3Time.ForeColor = Color.FromArgb(55, 38, 22);
            lblSlot3Time.Location = new Point(660, 26);
            lblSlot3Time.Name = "lblSlot3Time";
            lblSlot3Time.Size = new Size(205, 30);
            lblSlot3Time.TabIndex = 3;
            lblSlot3Time.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblSlot3Delete
            // 
            lblSlot3Delete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblSlot3Delete.BackColor = Color.Transparent;
            lblSlot3Delete.Cursor = Cursors.Hand;
            lblSlot3Delete.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSlot3Delete.ForeColor = Color.FromArgb(170, 25, 25);
            lblSlot3Delete.Location = new Point(885, 22);
            lblSlot3Delete.Name = "lblSlot3Delete";
            lblSlot3Delete.Size = new Size(95, 36);
            lblSlot3Delete.TabIndex = 4;
            lblSlot3Delete.Text = "DELETE";
            lblSlot3Delete.TextAlign = ContentAlignment.MiddleCenter;
            lblSlot3Delete.Visible = false;
            // 
            // OperatorArchiveForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(10, 10, 10);
            ClientSize = new Size(1920, 1080);
            Controls.Add(pnlParchmentLedger);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Name = "OperatorArchiveForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Recovered Operator Archive";
            WindowState = FormWindowState.Maximized;
            Load += OperatorArchiveForm_Load;
            KeyDown += OperatorArchiveForm_KeyDown;
            Resize += OperatorArchiveForm_Resize;
            pnlParchmentLedger.ResumeLayout(false);
            pnlParchmentLedger.PerformLayout();
            pnlSlotsContainer.ResumeLayout(false);
            pnlSlot1.ResumeLayout(false);
            pnlSlot2.ResumeLayout(false);
            pnlSlot3.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlParchmentLedger;
        private Label lblHeaderStartGame;
        private Label lblHeaderSubtitle;
        private Label lblClose;
        private Label lblSectionArchive;
        private Label lblSectionChoose;
        private Label lblQuote;
        private Panel pnlSlotsContainer;
        private Panel pnlSlot1;
        private Label lblSlot1Num;
        private Label lblSlot1Name;
        private Label lblSlot1Details;
        private Label lblSlot1Time;
        private Label lblSlot1Delete;
        private Panel pnlSlot2;
        private Label lblSlot2Num;
        private Label lblSlot2Name;
        private Label lblSlot2Details;
        private Label lblSlot2Time;
        private Label lblSlot2Delete;
        private Panel pnlSlot3;
        private Label lblSlot3Num;
        private Label lblSlot3Name;
        private Label lblSlot3Details;
        private Label lblSlot3Time;
        private Label lblSlot3Delete;
    }
}
