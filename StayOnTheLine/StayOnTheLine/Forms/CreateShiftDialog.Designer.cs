namespace StayOnTheLine.Forms
{
    partial class CreateShiftDialog
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
            lblInputLabel = new Label();
            pnlInputBox = new Panel();
            txtOperatorName = new TextBox();
            lblAdvisory = new Label();
            btnCancel = new Label();
            btnStartShift = new Label();
            pnlParchmentContainer.SuspendLayout();
            pnlInputBox.SuspendLayout();
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
            pnlParchmentContainer.Controls.Add(lblInputLabel);
            pnlParchmentContainer.Controls.Add(pnlInputBox);
            pnlParchmentContainer.Controls.Add(lblAdvisory);
            pnlParchmentContainer.Controls.Add(btnCancel);
            pnlParchmentContainer.Controls.Add(btnStartShift);
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
            lblFileHeader.ForeColor = Color.FromArgb(30, 18, 10);
            lblFileHeader.Location = new Point(68, 52);
            lblFileHeader.Name = "lblFileHeader";
            lblFileHeader.Size = new Size(456, 22);
            lblFileHeader.TabIndex = 0;
            lblFileHeader.Text = "VERIFIED PERSONNEL ASSIGNMENT · FILE 01";
            // 
            // lblClose
            // 
            lblClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblClose.BackColor = Color.Transparent;
            lblClose.Cursor = Cursors.Hand;
            lblClose.Font = new Font("Segoe UI Symbol", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblClose.ForeColor = Color.FromArgb(40, 25, 12);
            lblClose.Location = new Point(572, 42);
            lblClose.Name = "lblClose";
            lblClose.Size = new Size(40, 38);
            lblClose.TabIndex = 1;
            lblClose.Text = "✕";
            lblClose.TextAlign = ContentAlignment.MiddleCenter;
            lblClose.Click += lblClose_Click;
            // 
            // lblTitle
            // 
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("IM FELL DW Pica SC", 23F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(16, 10, 4);
            lblTitle.Location = new Point(65, 85);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(550, 44);
            lblTitle.TabIndex = 2;
            lblTitle.Text = "Give the voice your name.";
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSubtitle
            // 
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Font = new Font("IM FELL DW Pica", 12F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.FromArgb(45, 28, 16);
            lblSubtitle.Location = new Point(68, 128);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(550, 28);
            lblSubtitle.TabIndex = 3;
            lblSubtitle.Text = "Once it knows who answered, the line cannot be closed.";
            // 
            // lblInputLabel
            // 
            lblInputLabel.AutoSize = true;
            lblInputLabel.BackColor = Color.Transparent;
            lblInputLabel.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblInputLabel.ForeColor = Color.FromArgb(32, 18, 10);
            lblInputLabel.Location = new Point(68, 172);
            lblInputLabel.Name = "lblInputLabel";
            lblInputLabel.Size = new Size(205, 22);
            lblInputLabel.TabIndex = 4;
            lblInputLabel.Text = "ENTER YOUR NAME";
            // 
            // pnlInputBox
            // 
            pnlInputBox.BackColor = Color.FromArgb(252, 248, 238);
            pnlInputBox.BorderStyle = BorderStyle.FixedSingle;
            pnlInputBox.Controls.Add(txtOperatorName);
            pnlInputBox.Location = new Point(68, 196);
            pnlInputBox.Name = "pnlInputBox";
            pnlInputBox.Padding = new Padding(12, 9, 12, 9);
            pnlInputBox.Size = new Size(545, 52);
            pnlInputBox.TabIndex = 5;
            // 
            // txtOperatorName
            // 
            txtOperatorName.BackColor = Color.FromArgb(252, 248, 238);
            txtOperatorName.BorderStyle = BorderStyle.None;
            txtOperatorName.Dock = DockStyle.Fill;
            txtOperatorName.Font = new Font("IM FELL DW Pica SC", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            txtOperatorName.ForeColor = Color.FromArgb(16, 10, 4);
            txtOperatorName.Location = new Point(12, 9);
            txtOperatorName.MaxLength = 24;
            txtOperatorName.Name = "txtOperatorName";
            txtOperatorName.PlaceholderText = "OPERATOR CALLSIGN / SURNAME";
            txtOperatorName.Size = new Size(519, 34);
            txtOperatorName.TabIndex = 0;
            txtOperatorName.TextChanged += txtOperatorName_TextChanged;
            txtOperatorName.KeyDown += txtOperatorName_KeyDown;
            // 
            // lblAdvisory
            // 
            lblAdvisory.AutoSize = true;
            lblAdvisory.BackColor = Color.Transparent;
            lblAdvisory.Font = new Font("IM FELL DW Pica", 9.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblAdvisory.ForeColor = Color.FromArgb(65, 45, 28);
            lblAdvisory.Location = new Point(68, 255);
            lblAdvisory.Name = "lblAdvisory";
            lblAdvisory.Size = new Size(339, 20);
            lblAdvisory.TabIndex = 8;
            lblAdvisory.Text = "“There are eight calls on tonight’s list. Do not disconnect before dawn.”";
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(232, 222, 200);
            btnCancel.BorderStyle = BorderStyle.FixedSingle;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.FromArgb(35, 22, 12);
            btnCancel.Location = new Point(270, 302);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(155, 46);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "CANCEL";
            btnCancel.TextAlign = ContentAlignment.MiddleCenter;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnStartShift
            // 
            btnStartShift.BackColor = Color.FromArgb(218, 208, 188);
            btnStartShift.BorderStyle = BorderStyle.FixedSingle;
            btnStartShift.Font = new Font("IM FELL DW Pica SC", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnStartShift.ForeColor = Color.FromArgb(125, 110, 92);
            btnStartShift.Location = new Point(435, 302);
            btnStartShift.Name = "btnStartShift";
            btnStartShift.Size = new Size(180, 46);
            btnStartShift.TabIndex = 7;
            btnStartShift.Text = "START SHIFT";
            btnStartShift.TextAlign = ContentAlignment.MiddleCenter;
            btnStartShift.Click += btnStartShift_Click;
            // 
            // CreateShiftDialog
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(18, 18, 18);
            ClientSize = new Size(680, 440);
            Controls.Add(pnlParchmentContainer);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Name = "CreateShiftDialog";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Give The Voice Your Name";
            Load += CreateShiftDialog_Load;
            pnlParchmentContainer.ResumeLayout(false);
            pnlParchmentContainer.PerformLayout();
            pnlInputBox.ResumeLayout(false);
            pnlInputBox.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlParchmentContainer;
        private Label lblFileHeader;
        private Label lblClose;
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblInputLabel;
        private Panel pnlInputBox;
        private TextBox txtOperatorName;
        private Label lblAdvisory;
        private Label btnCancel;
        private Label btnStartShift;
    }
}
