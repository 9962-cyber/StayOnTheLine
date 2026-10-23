namespace StayOnTheLine.Forms
{
    partial class DeleteShiftDialog
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
            pnlParchmentContainer = new Panel();
            lblFileHeader = new Label();
            lblClose = new Label();
            lblTitle = new Label();
            lblSubtitle = new Label();
            pnlDossierCard = new Panel();
            lblOperatorName = new Label();
            lblOperatorStats = new Label();
            lblTimestamp = new Label();
            lblAdvisory = new Label();
            btnCancel = new Label();
            btnConfirmPurge = new Label();
            pnlParchmentContainer.SuspendLayout();
            pnlDossierCard.SuspendLayout();
            SuspendLayout();
            // 
            // pnlParchmentContainer
            // 
            pnlParchmentContainer.BackColor = Color.FromArgb(218, 208, 185);
            pnlParchmentContainer.BackgroundImageLayout = ImageLayout.Stretch;
            pnlParchmentContainer.Controls.Add(lblFileHeader);
            pnlParchmentContainer.Controls.Add(lblClose);
            pnlParchmentContainer.Controls.Add(lblTitle);
            pnlParchmentContainer.Controls.Add(lblSubtitle);
            pnlParchmentContainer.Controls.Add(pnlDossierCard);
            pnlParchmentContainer.Controls.Add(lblAdvisory);
            pnlParchmentContainer.Controls.Add(btnCancel);
            pnlParchmentContainer.Controls.Add(btnConfirmPurge);
            pnlParchmentContainer.Dock = DockStyle.Fill;
            pnlParchmentContainer.Location = new Point(0, 0);
            pnlParchmentContainer.Name = "pnlParchmentContainer";
            pnlParchmentContainer.Size = new Size(680, 440);
            pnlParchmentContainer.TabIndex = 0;
            // 
            // lblFileHeader
            // 
            lblFileHeader.AutoSize = true;
            lblFileHeader.BackColor = Color.Transparent;
            lblFileHeader.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblFileHeader.ForeColor = Color.FromArgb(120, 32, 20);
            lblFileHeader.Location = new Point(68, 52);
            lblFileHeader.Name = "lblFileHeader";
            lblFileHeader.Size = new Size(380, 22);
            lblFileHeader.TabIndex = 0;
            lblFileHeader.Text = "INCINERATION PROTOCOL // FILE 01";
            // 
            // lblClose
            // 
            lblClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblClose.BackColor = Color.Transparent;
            lblClose.Cursor = Cursors.Hand;
            lblClose.Font = new Font("Georgia", 14F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblClose.ForeColor = Color.FromArgb(40, 25, 12);
            lblClose.Location = new Point(605, 46);
            lblClose.Name = "lblClose";
            lblClose.Size = new Size(36, 32);
            lblClose.TabIndex = 1;
            lblClose.Text = "✕";
            lblClose.TextAlign = ContentAlignment.MiddleCenter;
            lblClose.Click += lblClose_Click;
            // 
            // lblTitle
            // 
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("IM FELL DW Pica SC", 22F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(16, 10, 4);
            lblTitle.Location = new Point(65, 85);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(550, 42);
            lblTitle.TabIndex = 2;
            lblTitle.Text = "Purge operator dossier?";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSubtitle
            // 
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Font = new Font("IM FELL DW Pica", 11.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.FromArgb(55, 30, 16);
            lblSubtitle.Location = new Point(68, 128);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(550, 26);
            lblSubtitle.TabIndex = 3;
            lblSubtitle.Text = "Once eradicated, all recorded shift logs are permanently closed.";
            // 
            // pnlDossierCard
            // 
            pnlDossierCard.BackColor = Color.FromArgb(236, 226, 204);
            pnlDossierCard.Controls.Add(lblOperatorName);
            pnlDossierCard.Controls.Add(lblOperatorStats);
            pnlDossierCard.Controls.Add(lblTimestamp);
            pnlDossierCard.Location = new Point(68, 162);
            pnlDossierCard.Name = "pnlDossierCard";
            pnlDossierCard.Size = new Size(545, 110);
            pnlDossierCard.TabIndex = 4;
            // 
            // lblOperatorName
            // 
            lblOperatorName.BackColor = Color.Transparent;
            lblOperatorName.Font = new Font("IM FELL DW Pica SC", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblOperatorName.ForeColor = Color.FromArgb(16, 10, 4);
            lblOperatorName.Location = new Point(20, 14);
            lblOperatorName.Name = "lblOperatorName";
            lblOperatorName.Size = new Size(505, 32);
            lblOperatorName.TabIndex = 0;
            lblOperatorName.Text = "OPERATOR: MARA VOSS";
            lblOperatorName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblOperatorStats
            // 
            lblOperatorStats.BackColor = Color.Transparent;
            lblOperatorStats.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblOperatorStats.ForeColor = Color.FromArgb(50, 35, 20);
            lblOperatorStats.Location = new Point(20, 48);
            lblOperatorStats.Name = "lblOperatorStats";
            lblOperatorStats.Size = new Size(505, 24);
            lblOperatorStats.TabIndex = 1;
            lblOperatorStats.Text = "SHIFT 03 - 12 CALLS RECORDED";
            lblOperatorStats.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblTimestamp
            // 
            lblTimestamp.BackColor = Color.Transparent;
            lblTimestamp.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTimestamp.ForeColor = Color.FromArgb(70, 48, 28);
            lblTimestamp.Location = new Point(20, 74);
            lblTimestamp.Name = "lblTimestamp";
            lblTimestamp.Size = new Size(505, 22);
            lblTimestamp.TabIndex = 2;
            lblTimestamp.Text = "ARCHIVED: OCT 23 - 14:30";
            lblTimestamp.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblAdvisory
            // 
            lblAdvisory.AutoSize = true;
            lblAdvisory.BackColor = Color.Transparent;
            lblAdvisory.Font = new Font("IM FELL DW Pica", 9.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblAdvisory.ForeColor = Color.FromArgb(120, 32, 20);
            lblAdvisory.Location = new Point(68, 284);
            lblAdvisory.Name = "lblAdvisory";
            lblAdvisory.Size = new Size(480, 20);
            lblAdvisory.TabIndex = 5;
            lblAdvisory.Text = "“This action cannot be undone. The frequency will be permanently released.”";
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(232, 222, 200);
            btnCancel.BorderStyle = BorderStyle.FixedSingle;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.FromArgb(35, 22, 12);
            btnCancel.Location = new Point(260, 325);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(155, 46);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "CANCEL";
            btnCancel.TextAlign = ContentAlignment.MiddleCenter;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnConfirmPurge
            // 
            btnConfirmPurge.BackColor = Color.FromArgb(120, 32, 20);
            btnConfirmPurge.BorderStyle = BorderStyle.FixedSingle;
            btnConfirmPurge.Cursor = Cursors.Hand;
            btnConfirmPurge.Font = new Font("IM FELL DW Pica SC", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnConfirmPurge.ForeColor = Color.FromArgb(250, 244, 230);
            btnConfirmPurge.Location = new Point(425, 325);
            btnConfirmPurge.Name = "btnConfirmPurge";
            btnConfirmPurge.Size = new Size(188, 46);
            btnConfirmPurge.TabIndex = 7;
            btnConfirmPurge.Text = "PURGE DOSSIER";
            btnConfirmPurge.TextAlign = ContentAlignment.MiddleCenter;
            btnConfirmPurge.Click += btnConfirmPurge_Click;
            // 
            // DeleteShiftDialog
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(18, 18, 18);
            ClientSize = new Size(680, 440);
            Controls.Add(pnlParchmentContainer);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Name = "DeleteShiftDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Purge Operator Dossier";
            Load += DeleteShiftDialog_Load;
            pnlParchmentContainer.ResumeLayout(false);
            pnlParchmentContainer.PerformLayout();
            pnlDossierCard.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlParchmentContainer;
        private Label lblFileHeader;
        private Label lblClose;
        private Label lblTitle;
        private Label lblSubtitle;
        private Panel pnlDossierCard;
        private Label lblOperatorName;
        private Label lblOperatorStats;
        private Label lblTimestamp;
        private Label lblAdvisory;
        private Label btnCancel;
        private Label btnConfirmPurge;
    }
}
