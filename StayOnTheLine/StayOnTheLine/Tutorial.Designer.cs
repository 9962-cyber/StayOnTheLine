namespace StayOnTheLine
{
    partial class Tutorial
    {
        private System.ComponentModel.IContainer components = null;

        // UI Controls
        private System.Windows.Forms.PictureBox picBackground;
        private System.Windows.Forms.PictureBox picCharacter;
        private DialoguePanel pnlDialogue;
        private System.Windows.Forms.Label lblSpeaker;
        private System.Windows.Forms.Label lblDialogue;
        private System.Windows.Forms.Label lblIncoming;
        private System.Windows.Forms.Label lblArrow;
        private System.Windows.Forms.Button btnSettings;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.picBackground = new System.Windows.Forms.PictureBox();
            this.picCharacter = new System.Windows.Forms.PictureBox();
            this.pnlDialogue = new StayOnTheLine.DialoguePanel();
            this.lblArrow = new System.Windows.Forms.Label();
            this.lblIncoming = new System.Windows.Forms.Label();
            this.lblDialogue = new System.Windows.Forms.Label();
            this.lblSpeaker = new System.Windows.Forms.Label();
            this.btnSettings = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picBackground)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picCharacter)).BeginInit();
            this.pnlDialogue.SuspendLayout();
            this.picBackground.SuspendLayout();
            this.SuspendLayout();

            // 
            // picBackground  (parent of everything else, so transparency blends with the art)
            // 
            this.picBackground.BackColor = System.Drawing.Color.Black;
            this.picBackground.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picBackground.Name = "picBackground";
            this.picBackground.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picBackground.TabIndex = 0;
            this.picBackground.TabStop = false;
            // picCharacter in back, pnlDialogue on top, btnSettings on very top
            this.picBackground.Controls.Add(this.picCharacter);
            this.picBackground.Controls.Add(this.pnlDialogue);
            this.picBackground.Controls.Add(this.btnSettings);

            // 
            // picCharacter  (position/size set in ApplyLayout)
            // 
            this.picCharacter.BackColor = System.Drawing.Color.Transparent;
            this.picCharacter.Name = "picCharacter";
            this.picCharacter.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picCharacter.TabIndex = 1;
            this.picCharacter.TabStop = false;

            // 
            // pnlDialogue  (position/size set in ApplyLayout)
            // 
            this.pnlDialogue.Controls.Add(this.lblArrow);
            this.pnlDialogue.Controls.Add(this.lblIncoming);
            this.pnlDialogue.Controls.Add(this.lblDialogue);
            this.pnlDialogue.Controls.Add(this.lblSpeaker);
            this.pnlDialogue.Name = "pnlDialogue";
            this.pnlDialogue.TabIndex = 2;

            // 
            // lblSpeaker
            // 
            this.lblSpeaker.AutoSize = true;
            this.lblSpeaker.BackColor = System.Drawing.Color.Transparent;
            this.lblSpeaker.ForeColor = System.Drawing.Color.White;
            this.lblSpeaker.Name = "lblSpeaker";
            this.lblSpeaker.TabIndex = 0;
            this.lblSpeaker.Text = "SPEAKER";

            // 
            // lblIncoming  (small gray tag, top-right of the box)
            // 
            this.lblIncoming.AutoSize = false;
            this.lblIncoming.BackColor = System.Drawing.Color.Transparent;
            this.lblIncoming.ForeColor = System.Drawing.Color.Gray;
            this.lblIncoming.Name = "lblIncoming";
            this.lblIncoming.TabIndex = 2;
            this.lblIncoming.Text = "INCOMING / VOICE";
            this.lblIncoming.TextAlign = System.Drawing.ContentAlignment.TopRight;

            // 
            // lblDialogue
            // 
            this.lblDialogue.AutoSize = false;
            this.lblDialogue.BackColor = System.Drawing.Color.Transparent;
            this.lblDialogue.ForeColor = System.Drawing.Color.White;
            this.lblDialogue.Name = "lblDialogue";
            this.lblDialogue.TabIndex = 1;
            this.lblDialogue.Text = "Dialogue text goes here...";

            // 
            // lblArrow  (the little ▼ at the bottom-right)
            // 
            this.lblArrow.AutoSize = true;
            this.lblArrow.BackColor = System.Drawing.Color.Transparent;
            this.lblArrow.ForeColor = System.Drawing.Color.White;
            this.lblArrow.Name = "lblArrow";
            this.lblArrow.TabIndex = 4;
            this.lblArrow.Text = "▼";

            // 
            // btnSettings  (gear is custom drawn)
            // 
            this.btnSettings.BackColor = System.Drawing.Color.Transparent;
            this.btnSettings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(50, 255, 255, 255);
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(50, 50);
            this.btnSettings.TabIndex = 3;
            this.btnSettings.TabStop = false;
            this.btnSettings.UseVisualStyleBackColor = false;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            this.btnSettings.Paint += new System.Windows.Forms.PaintEventHandler(this.btnSettings_Paint);

            // 
            // Tutorial
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1920, 1080);
            this.Controls.Add(this.picBackground);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Name = "Tutorial";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Stay On The Line - Tutorial";

            // BORDERLESS FULLSCREEN (1920x1080 design size)
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.TopMost = true;

            ((System.ComponentModel.ISupportInitialize)(this.picBackground)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picCharacter)).EndInit();
            this.pnlDialogue.ResumeLayout(false);
            this.pnlDialogue.PerformLayout();
            this.picBackground.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}