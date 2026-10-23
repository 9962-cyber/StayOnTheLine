namespace StayOnTheLine.Forms
{
    partial class SettingsForm
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
            lblHeaderTitle = new Label();
            lblHeaderSubtitle = new Label();
            lblClose = new Label();
            lblSectionKicker = new Label();
            lblSectionTitle = new Label();
            lblTagline = new Label();
            pnlRowsContainer = new Panel();
            pnlRow1 = new Panel();
            pnlRow1Header = new Panel();
            lblRow1Num = new Label();
            lblRow1Title = new Label();
            lblRow1Summary = new Label();
            lblRow1Toggle = new Label();
            pnlRow1Body = new Panel();
            lblAudioDesc = new Label();
            chkMuteAll = new Label();
            lblAudioHint = new Label();
            lblMaster = new Label();
            lblMasterVal = new Label();
            tbMasterVolume = new StayOnTheLine.Controls.VintageTrackBar();
            lblAmbient = new Label();
            lblAmbientVal = new Label();
            tbAmbientVolume = new StayOnTheLine.Controls.VintageTrackBar();
            lblSfx = new Label();
            lblSfxVal = new Label();
            tbSfxVolume = new StayOnTheLine.Controls.VintageTrackBar();
            pnlRow2 = new Panel();
            pnlRow2Header = new Panel();
            lblRow2Num = new Label();
            lblRow2Title = new Label();
            lblRow2Summary = new Label();
            lblRow2Toggle = new Label();
            pnlRow2Body = new Panel();
            lblTeletypeDesc = new Label();
            lblRow2Status = new Label();
            btnSpeedSlow = new Label();
            btnSpeedStandard = new Label();
            btnSpeedInstant = new Label();
            pnlRow3 = new Panel();
            pnlRow3Header = new Panel();
            lblRow3Num = new Label();
            lblRow3Title = new Label();
            lblRow3Summary = new Label();
            lblRow3Toggle = new Label();
            pnlRow3Body = new Panel();
            pnlPurgeAdvisoryCard = new Panel();
            lblPurgeHeader = new Label();
            lblPurgeDetails = new Label();
            lblPersistentInfo = new Label();
            btnPurgeAll = new Label();
            lblFooterHelp = new Label();
            btnCancel = new Label();
            btnApply = new Label();
            pnlParchmentContainer.SuspendLayout();
            pnlRowsContainer.SuspendLayout();
            pnlRow1.SuspendLayout();
            pnlRow1Header.SuspendLayout();
            pnlRow1Body.SuspendLayout();
            pnlRow2.SuspendLayout();
            pnlRow2Header.SuspendLayout();
            pnlRow2Body.SuspendLayout();
            pnlRow3.SuspendLayout();
            pnlRow3Header.SuspendLayout();
            pnlRow3Body.SuspendLayout();
            pnlPurgeAdvisoryCard.SuspendLayout();
            SuspendLayout();
            // 
            // pnlParchmentContainer
            // 
            pnlParchmentContainer.BackColor = Color.FromArgb(218, 208, 185);
            pnlParchmentContainer.BackgroundImageLayout = ImageLayout.Stretch;
            pnlParchmentContainer.Controls.Add(lblHeaderTitle);
            pnlParchmentContainer.Controls.Add(lblHeaderSubtitle);
            pnlParchmentContainer.Controls.Add(lblClose);
            pnlParchmentContainer.Controls.Add(lblSectionKicker);
            pnlParchmentContainer.Controls.Add(lblSectionTitle);
            pnlParchmentContainer.Controls.Add(lblTagline);
            pnlParchmentContainer.Controls.Add(pnlRowsContainer);
            pnlParchmentContainer.Controls.Add(lblFooterHelp);
            pnlParchmentContainer.Controls.Add(btnCancel);
            pnlParchmentContainer.Controls.Add(btnApply);
            pnlParchmentContainer.Location = new Point(380, 150);
            pnlParchmentContainer.Name = "pnlParchmentContainer";
            pnlParchmentContainer.Size = new Size(1160, 760);
            pnlParchmentContainer.TabIndex = 0;
            // 
            // lblHeaderTitle
            // 
            lblHeaderTitle.BackColor = Color.Transparent;
            lblHeaderTitle.Font = new Font("IM FELL DW Pica SC", 30F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeaderTitle.ForeColor = Color.FromArgb(16, 10, 4);
            lblHeaderTitle.Location = new Point(180, 62);
            lblHeaderTitle.Name = "lblHeaderTitle";
            lblHeaderTitle.Size = new Size(800, 48);
            lblHeaderTitle.TabIndex = 1;
            lblHeaderTitle.Text = "STATION SETTINGS";
            lblHeaderTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblHeaderTitle.UseMnemonic = false;
            // 
            // lblHeaderSubtitle
            // 
            lblHeaderSubtitle.BackColor = Color.Transparent;
            lblHeaderSubtitle.Font = new Font("IM FELL DW Pica", 12.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblHeaderSubtitle.ForeColor = Color.FromArgb(48, 32, 18);
            lblHeaderSubtitle.Location = new Point(180, 112);
            lblHeaderSubtitle.Name = "lblHeaderSubtitle";
            lblHeaderSubtitle.Size = new Size(800, 26);
            lblHeaderSubtitle.TabIndex = 2;
            lblHeaderSubtitle.Text = "Calibrate frequency channels, teletype transcript speed, and archive logs.";
            lblHeaderSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            lblHeaderSubtitle.UseMnemonic = false;
            // 
            // lblClose
            // 
            lblClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblClose.BackColor = Color.Transparent;
            lblClose.Cursor = Cursors.Hand;
            lblClose.Font = new Font("Georgia", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblClose.ForeColor = Color.FromArgb(40, 25, 12);
            lblClose.Location = new Point(1045, 68);
            lblClose.Name = "lblClose";
            lblClose.Size = new Size(36, 36);
            lblClose.TabIndex = 3;
            lblClose.Text = "✕";
            lblClose.TextAlign = ContentAlignment.MiddleCenter;
            lblClose.Click += lblClose_Click;
            // 
            // lblSectionKicker
            // 
            lblSectionKicker.AutoSize = true;
            lblSectionKicker.BackColor = Color.Transparent;
            lblSectionKicker.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSectionKicker.ForeColor = Color.FromArgb(165, 45, 28);
            lblSectionKicker.Location = new Point(70, 148);
            lblSectionKicker.Name = "lblSectionKicker";
            lblSectionKicker.Size = new Size(340, 22);
            lblSectionKicker.TabIndex = 4;
            lblSectionKicker.Text = "CIVIL DEFENSE TERMINAL PROTOCOL · CONF-1996";
            lblSectionKicker.UseMnemonic = false;
            // 
            // lblSectionTitle
            // 
            lblSectionTitle.AutoSize = true;
            lblSectionTitle.BackColor = Color.Transparent;
            lblSectionTitle.Font = new Font("IM FELL DW Pica SC", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSectionTitle.ForeColor = Color.FromArgb(20, 12, 6);
            lblSectionTitle.Location = new Point(68, 168);
            lblSectionTitle.Name = "lblSectionTitle";
            lblSectionTitle.Size = new Size(395, 41);
            lblSectionTitle.TabIndex = 5;
            lblSectionTitle.Text = "SYSTEM CONFIGURATION";
            lblSectionTitle.UseMnemonic = false;
            // 
            // lblTagline
            // 
            lblTagline.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTagline.BackColor = Color.Transparent;
            lblTagline.Font = new Font("IM FELL DW Pica", 11.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblTagline.ForeColor = Color.FromArgb(50, 35, 20);
            lblTagline.Location = new Point(600, 176);
            lblTagline.Name = "lblTagline";
            lblTagline.Size = new Size(490, 26);
            lblTagline.TabIndex = 6;
            lblTagline.Text = "“Click any file ledger below to expand and calibrate.”";
            lblTagline.TextAlign = ContentAlignment.TopRight;
            lblTagline.UseMnemonic = false;
            // 
            // pnlRowsContainer
            // 
            pnlRowsContainer.BackColor = Color.Transparent;
            pnlRowsContainer.Controls.Add(pnlRow1);
            pnlRowsContainer.Controls.Add(pnlRow2);
            pnlRowsContainer.Controls.Add(pnlRow3);
            pnlRowsContainer.Location = new Point(70, 212);
            pnlRowsContainer.Name = "pnlRowsContainer";
            pnlRowsContainer.Size = new Size(1020, 455);
            pnlRowsContainer.TabIndex = 7;
            // 
            // pnlRow1
            // 
            pnlRow1.BackColor = Color.Transparent;
            pnlRow1.Controls.Add(pnlRow1Header);
            pnlRow1.Controls.Add(pnlRow1Body);
            pnlRow1.Cursor = Cursors.Hand;
            pnlRow1.Location = new Point(0, 0);
            pnlRow1.Name = "pnlRow1";
            pnlRow1.Size = new Size(1020, 326);
            pnlRow1.TabIndex = 0;
            // 
            // pnlRow1Header
            // 
            pnlRow1Header.BackColor = Color.Transparent;
            pnlRow1Header.Controls.Add(lblRow1Num);
            pnlRow1Header.Controls.Add(lblRow1Title);
            pnlRow1Header.Controls.Add(lblRow1Summary);
            pnlRow1Header.Controls.Add(lblRow1Toggle);
            pnlRow1Header.Cursor = Cursors.Hand;
            pnlRow1Header.Location = new Point(0, 0);
            pnlRow1Header.Name = "pnlRow1Header";
            pnlRow1Header.Size = new Size(1020, 52);
            pnlRow1Header.TabIndex = 0;
            // 
            // lblRow1Num
            // 
            lblRow1Num.AutoSize = true;
            lblRow1Num.BackColor = Color.Transparent;
            lblRow1Num.Font = new Font("IM FELL DW Pica SC", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow1Num.ForeColor = Color.FromArgb(145, 65, 45);
            lblRow1Num.Location = new Point(18, 6);
            lblRow1Num.Name = "lblRow1Num";
            lblRow1Num.Size = new Size(41, 41);
            lblRow1Num.TabIndex = 0;
            lblRow1Num.Text = "01";
            lblRow1Num.UseMnemonic = false;
            // 
            // lblRow1Title
            // 
            lblRow1Title.AutoSize = true;
            lblRow1Title.BackColor = Color.Transparent;
            lblRow1Title.Font = new Font("IM FELL DW Pica SC", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow1Title.ForeColor = Color.FromArgb(20, 12, 6);
            lblRow1Title.Location = new Point(70, 9);
            lblRow1Title.Name = "lblRow1Title";
            lblRow1Title.Size = new Size(240, 33);
            lblRow1Title.TabIndex = 1;
            lblRow1Title.Text = "AUDIO CALIBRATION";
            lblRow1Title.UseMnemonic = false;
            // 
            // lblRow1Summary
            // 
            lblRow1Summary.BackColor = Color.Transparent;
            lblRow1Summary.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow1Summary.ForeColor = Color.FromArgb(85, 65, 45);
            lblRow1Summary.Location = new Point(350, 14);
            lblRow1Summary.Name = "lblRow1Summary";
            lblRow1Summary.Size = new Size(495, 24);
            lblRow1Summary.TabIndex = 2;
            lblRow1Summary.Text = "Master 80% · Ambience 75% · SFX 85%";
            lblRow1Summary.TextAlign = ContentAlignment.MiddleLeft;
            lblRow1Summary.UseMnemonic = false;
            // 
            // lblRow1Toggle
            // 
            lblRow1Toggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblRow1Toggle.BackColor = Color.Transparent;
            lblRow1Toggle.Cursor = Cursors.Hand;
            lblRow1Toggle.Font = new Font("IM FELL DW Pica SC", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow1Toggle.ForeColor = Color.FromArgb(145, 55, 30);
            lblRow1Toggle.Location = new Point(860, 11);
            lblRow1Toggle.Name = "lblRow1Toggle";
            lblRow1Toggle.Size = new Size(145, 30);
            lblRow1Toggle.TabIndex = 3;
            lblRow1Toggle.Text = "▲ ACTIVE FILE";
            lblRow1Toggle.TextAlign = ContentAlignment.MiddleCenter;
            lblRow1Toggle.UseMnemonic = false;
            // 
            // pnlRow1Body
            // 
            pnlRow1Body.BackColor = Color.Transparent;
            pnlRow1Body.Controls.Add(lblAudioDesc);
            pnlRow1Body.Controls.Add(chkMuteAll);
            pnlRow1Body.Controls.Add(lblAudioHint);
            pnlRow1Body.Controls.Add(lblMaster);
            pnlRow1Body.Controls.Add(lblMasterVal);
            pnlRow1Body.Controls.Add(tbMasterVolume);
            pnlRow1Body.Controls.Add(lblAmbient);
            pnlRow1Body.Controls.Add(lblAmbientVal);
            pnlRow1Body.Controls.Add(tbAmbientVolume);
            pnlRow1Body.Controls.Add(lblSfx);
            pnlRow1Body.Controls.Add(lblSfxVal);
            pnlRow1Body.Controls.Add(tbSfxVolume);
            pnlRow1Body.Location = new Point(0, 52);
            pnlRow1Body.Name = "pnlRow1Body";
            pnlRow1Body.Size = new Size(1020, 274);
            pnlRow1Body.TabIndex = 1;
            // 
            // lblAudioDesc
            // 
            lblAudioDesc.BackColor = Color.Transparent;
            lblAudioDesc.Font = new Font("IM FELL DW Pica", 10F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblAudioDesc.ForeColor = Color.FromArgb(70, 50, 32);
            lblAudioDesc.Location = new Point(60, 16);
            lblAudioDesc.Name = "lblAudioDesc";
            lblAudioDesc.Size = new Size(300, 50);
            lblAudioDesc.TabIndex = 0;
            lblAudioDesc.Text = "Calibrate emergency radio audio levels, analog tape hiss, and mechanical typewriter clicks.";
            lblAudioDesc.UseMnemonic = false;
            // 
            // chkMuteAll
            // 
            chkMuteAll.BackColor = Color.Transparent;
            chkMuteAll.Cursor = Cursors.Hand;
            chkMuteAll.Font = new Font("IM FELL DW Pica SC", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            chkMuteAll.ForeColor = Color.FromArgb(40, 25, 12);
            chkMuteAll.Location = new Point(60, 78);
            chkMuteAll.Name = "chkMuteAll";
            chkMuteAll.Size = new Size(300, 44);
            chkMuteAll.TabIndex = 1;
            chkMuteAll.Text = "[   ] MUTE ALL FREQUENCIES";
            chkMuteAll.TextAlign = ContentAlignment.MiddleCenter;
            chkMuteAll.UseMnemonic = false;
            chkMuteAll.Click += chkMuteAll_Click;
            // 
            // lblAudioHint
            // 
            lblAudioHint.BackColor = Color.Transparent;
            lblAudioHint.Font = new Font("IM FELL DW Pica", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblAudioHint.ForeColor = Color.FromArgb(90, 70, 50);
            lblAudioHint.Location = new Point(60, 138);
            lblAudioHint.Name = "lblAudioHint";
            lblAudioHint.Size = new Size(300, 115);
            lblAudioHint.TabIndex = 2;
            lblAudioHint.Text = "Audio calibrations apply immediately to active hardware.\r\n\r\nPrimary background music and analog tape hiss loop continuously in the dark dispatch office.";
            lblAudioHint.UseMnemonic = false;
            // 
            // lblMaster
            // 
            lblMaster.BackColor = Color.Transparent;
            lblMaster.Font = new Font("IM FELL DW Pica SC", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMaster.ForeColor = Color.FromArgb(40, 25, 12);
            lblMaster.Location = new Point(390, 14);
            lblMaster.Name = "lblMaster";
            lblMaster.Size = new Size(300, 22);
            lblMaster.TabIndex = 3;
            lblMaster.Text = "MASTER FREQUENCY GAIN";
            lblMaster.UseMnemonic = false;
            // 
            // lblMasterVal
            // 
            lblMasterVal.BackColor = Color.Transparent;
            lblMasterVal.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMasterVal.ForeColor = Color.FromArgb(145, 55, 25);
            lblMasterVal.Location = new Point(880, 14);
            lblMasterVal.Name = "lblMasterVal";
            lblMasterVal.Size = new Size(100, 22);
            lblMasterVal.TabIndex = 4;
            lblMasterVal.Text = "80%";
            lblMasterVal.TextAlign = ContentAlignment.TopRight;
            lblMasterVal.UseMnemonic = false;
            // 
            // tbMasterVolume
            // 
            tbMasterVolume.BackColor = Color.Transparent;
            tbMasterVolume.Cursor = Cursors.Hand;
            tbMasterVolume.Location = new Point(390, 38);
            tbMasterVolume.Maximum = 100;
            tbMasterVolume.Minimum = 0;
            tbMasterVolume.Name = "tbMasterVolume";
            tbMasterVolume.Size = new Size(590, 36);
            tbMasterVolume.TabIndex = 5;
            tbMasterVolume.Value = 80;
            // 
            // lblAmbient
            // 
            lblAmbient.BackColor = Color.Transparent;
            lblAmbient.Font = new Font("IM FELL DW Pica SC", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAmbient.ForeColor = Color.FromArgb(40, 25, 12);
            lblAmbient.Location = new Point(390, 92);
            lblAmbient.Name = "lblAmbient";
            lblAmbient.Size = new Size(360, 22);
            lblAmbient.TabIndex = 6;
            lblAmbient.Text = "RADIO AMBIENCE / ANALOG TAPE HISS";
            lblAmbient.UseMnemonic = false;
            // 
            // lblAmbientVal
            // 
            lblAmbientVal.BackColor = Color.Transparent;
            lblAmbientVal.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAmbientVal.ForeColor = Color.FromArgb(145, 55, 25);
            lblAmbientVal.Location = new Point(880, 92);
            lblAmbientVal.Name = "lblAmbientVal";
            lblAmbientVal.Size = new Size(100, 22);
            lblAmbientVal.TabIndex = 7;
            lblAmbientVal.Text = "75%";
            lblAmbientVal.TextAlign = ContentAlignment.TopRight;
            lblAmbientVal.UseMnemonic = false;
            // 
            // tbAmbientVolume
            // 
            tbAmbientVolume.BackColor = Color.Transparent;
            tbAmbientVolume.Cursor = Cursors.Hand;
            tbAmbientVolume.Location = new Point(390, 116);
            tbAmbientVolume.Maximum = 100;
            tbAmbientVolume.Minimum = 0;
            tbAmbientVolume.Name = "tbAmbientVolume";
            tbAmbientVolume.Size = new Size(590, 36);
            tbAmbientVolume.TabIndex = 8;
            tbAmbientVolume.Value = 75;
            // 
            // lblSfx
            // 
            lblSfx.BackColor = Color.Transparent;
            lblSfx.Font = new Font("IM FELL DW Pica SC", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSfx.ForeColor = Color.FromArgb(40, 25, 12);
            lblSfx.Location = new Point(390, 170);
            lblSfx.Name = "lblSfx";
            lblSfx.Size = new Size(360, 22);
            lblSfx.TabIndex = 9;
            lblSfx.Text = "DISPATCH SFX / TYPEWRITER CLICKS";
            lblSfx.UseMnemonic = false;
            // 
            // lblSfxVal
            // 
            lblSfxVal.BackColor = Color.Transparent;
            lblSfxVal.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSfxVal.ForeColor = Color.FromArgb(145, 55, 25);
            lblSfxVal.Location = new Point(880, 170);
            lblSfxVal.Name = "lblSfxVal";
            lblSfxVal.Size = new Size(100, 22);
            lblSfxVal.TabIndex = 10;
            lblSfxVal.Text = "85%";
            lblSfxVal.TextAlign = ContentAlignment.TopRight;
            lblSfxVal.UseMnemonic = false;
            // 
            // tbSfxVolume
            // 
            tbSfxVolume.BackColor = Color.Transparent;
            tbSfxVolume.Cursor = Cursors.Hand;
            tbSfxVolume.Location = new Point(390, 194);
            tbSfxVolume.Maximum = 100;
            tbSfxVolume.Minimum = 0;
            tbSfxVolume.Name = "tbSfxVolume";
            tbSfxVolume.Size = new Size(590, 36);
            tbSfxVolume.TabIndex = 11;
            tbSfxVolume.Value = 85;
            // 
            // pnlRow2
            // 
            pnlRow2.BackColor = Color.Transparent;
            pnlRow2.Controls.Add(pnlRow2Header);
            pnlRow2.Controls.Add(pnlRow2Body);
            pnlRow2.Cursor = Cursors.Hand;
            pnlRow2.Location = new Point(0, 336);
            pnlRow2.Name = "pnlRow2";
            pnlRow2.Size = new Size(1020, 62);
            pnlRow2.TabIndex = 1;
            // 
            // pnlRow2Header
            // 
            pnlRow2Header.BackColor = Color.Transparent;
            pnlRow2Header.Controls.Add(lblRow2Num);
            pnlRow2Header.Controls.Add(lblRow2Title);
            pnlRow2Header.Controls.Add(lblRow2Summary);
            pnlRow2Header.Controls.Add(lblRow2Toggle);
            pnlRow2Header.Cursor = Cursors.Hand;
            pnlRow2Header.Location = new Point(0, 0);
            pnlRow2Header.Name = "pnlRow2Header";
            pnlRow2Header.Size = new Size(1020, 52);
            pnlRow2Header.TabIndex = 0;
            // 
            // lblRow2Num
            // 
            lblRow2Num.AutoSize = true;
            lblRow2Num.BackColor = Color.Transparent;
            lblRow2Num.Font = new Font("IM FELL DW Pica SC", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow2Num.ForeColor = Color.FromArgb(145, 65, 45);
            lblRow2Num.Location = new Point(18, 6);
            lblRow2Num.Name = "lblRow2Num";
            lblRow2Num.Size = new Size(41, 41);
            lblRow2Num.TabIndex = 0;
            lblRow2Num.Text = "02";
            lblRow2Num.UseMnemonic = false;
            // 
            // lblRow2Title
            // 
            lblRow2Title.AutoSize = true;
            lblRow2Title.BackColor = Color.Transparent;
            lblRow2Title.Font = new Font("IM FELL DW Pica SC", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow2Title.ForeColor = Color.FromArgb(20, 12, 6);
            lblRow2Title.Location = new Point(70, 9);
            lblRow2Title.Name = "lblRow2Title";
            lblRow2Title.Size = new Size(310, 33);
            lblRow2Title.TabIndex = 1;
            lblRow2Title.Text = "TELETYPE PRINT SPEED";
            lblRow2Title.UseMnemonic = false;
            // 
            // lblRow2Summary
            // 
            lblRow2Summary.BackColor = Color.Transparent;
            lblRow2Summary.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow2Summary.ForeColor = Color.FromArgb(85, 65, 45);
            lblRow2Summary.Location = new Point(400, 14);
            lblRow2Summary.Name = "lblRow2Summary";
            lblRow2Summary.Size = new Size(445, 24);
            lblRow2Summary.TabIndex = 2;
            lblRow2Summary.Text = "Cadence: Standard (30ms / char)";
            lblRow2Summary.TextAlign = ContentAlignment.MiddleLeft;
            lblRow2Summary.UseMnemonic = false;
            // 
            // lblRow2Toggle
            // 
            lblRow2Toggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblRow2Toggle.BackColor = Color.Transparent;
            lblRow2Toggle.Cursor = Cursors.Hand;
            lblRow2Toggle.Font = new Font("IM FELL DW Pica SC", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow2Toggle.ForeColor = Color.FromArgb(40, 25, 12);
            lblRow2Toggle.Location = new Point(860, 11);
            lblRow2Toggle.Name = "lblRow2Toggle";
            lblRow2Toggle.Size = new Size(145, 30);
            lblRow2Toggle.TabIndex = 3;
            lblRow2Toggle.Text = "▼ OPEN FILE";
            lblRow2Toggle.TextAlign = ContentAlignment.MiddleCenter;
            lblRow2Toggle.UseMnemonic = false;
            // 
            // pnlRow2Body
            // 
            pnlRow2Body.BackColor = Color.Transparent;
            pnlRow2Body.Controls.Add(lblTeletypeDesc);
            pnlRow2Body.Controls.Add(lblRow2Status);
            pnlRow2Body.Controls.Add(btnSpeedSlow);
            pnlRow2Body.Controls.Add(btnSpeedStandard);
            pnlRow2Body.Controls.Add(btnSpeedInstant);
            pnlRow2Body.Location = new Point(0, 52);
            pnlRow2Body.Name = "pnlRow2Body";
            pnlRow2Body.Size = new Size(1020, 274);
            pnlRow2Body.TabIndex = 1;
            pnlRow2Body.Visible = false;
            // 
            // lblTeletypeDesc
            // 
            lblTeletypeDesc.BackColor = Color.Transparent;
            lblTeletypeDesc.Font = new Font("IM FELL DW Pica", 11F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblTeletypeDesc.ForeColor = Color.FromArgb(60, 42, 26);
            lblTeletypeDesc.Location = new Point(60, 14);
            lblTeletypeDesc.Name = "lblTeletypeDesc";
            lblTeletypeDesc.Size = new Size(900, 26);
            lblTeletypeDesc.TabIndex = 0;
            lblTeletypeDesc.Text = "Gaano kabilis lumalabas ang text ng caller dialogue o protocol briefing sa screen.";
            lblTeletypeDesc.UseMnemonic = false;
            // 
            // lblRow2Status
            // 
            lblRow2Status.BackColor = Color.Transparent;
            lblRow2Status.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow2Status.ForeColor = Color.FromArgb(160, 45, 25);
            lblRow2Status.Location = new Point(60, 44);
            lblRow2Status.Name = "lblRow2Status";
            lblRow2Status.Size = new Size(900, 24);
            lblRow2Status.TabIndex = 1;
            lblRow2Status.Text = "[ ACTIVE CADENCE: STANDARD · 30 MS / CHAR ]";
            lblRow2Status.UseMnemonic = false;
            // 
            // btnSpeedSlow
            // 
            btnSpeedSlow.BackColor = Color.Transparent;
            btnSpeedSlow.Cursor = Cursors.Hand;
            btnSpeedSlow.Font = new Font("IM FELL DW Pica SC", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSpeedSlow.ForeColor = Color.FromArgb(40, 25, 12);
            btnSpeedSlow.Location = new Point(60, 76);
            btnSpeedSlow.Name = "btnSpeedSlow";
            btnSpeedSlow.Size = new Size(286, 176);
            btnSpeedSlow.TabIndex = 2;
            btnSpeedSlow.Text = "SLOW\r\n\r\n65 MS / CHAR\r\n\r\nAuthentic manual typewriter cadence.\r\nDeliberate character printing.";
            btnSpeedSlow.TextAlign = ContentAlignment.MiddleCenter;
            btnSpeedSlow.UseMnemonic = false;
            btnSpeedSlow.Click += btnSpeedSlow_Click;
            // 
            // btnSpeedStandard
            // 
            btnSpeedStandard.BackColor = Color.FromArgb(45, 32, 20);
            btnSpeedStandard.Cursor = Cursors.Hand;
            btnSpeedStandard.Font = new Font("IM FELL DW Pica SC", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSpeedStandard.ForeColor = Color.FromArgb(248, 244, 232);
            btnSpeedStandard.Location = new Point(366, 76);
            btnSpeedStandard.Name = "btnSpeedStandard";
            btnSpeedStandard.Size = new Size(286, 176);
            btnSpeedStandard.TabIndex = 3;
            btnSpeedStandard.Text = "STANDARD\r\n\r\n30 MS / CHAR\r\n\r\nRecommended dispatch pacing.\r\nBalanced reading flow.";
            btnSpeedStandard.TextAlign = ContentAlignment.MiddleCenter;
            btnSpeedStandard.UseMnemonic = false;
            btnSpeedStandard.Click += btnSpeedStandard_Click;
            // 
            // btnSpeedInstant
            // 
            btnSpeedInstant.BackColor = Color.Transparent;
            btnSpeedInstant.Cursor = Cursors.Hand;
            btnSpeedInstant.Font = new Font("IM FELL DW Pica SC", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSpeedInstant.ForeColor = Color.FromArgb(40, 25, 12);
            btnSpeedInstant.Location = new Point(672, 76);
            btnSpeedInstant.Name = "btnSpeedInstant";
            btnSpeedInstant.Size = new Size(286, 176);
            btnSpeedInstant.TabIndex = 4;
            btnSpeedInstant.Text = "INSTANT\r\n\r\n0 MS / CHAR\r\n\r\nImmediate line readout.\r\nBypasses typewriter delay.";
            btnSpeedInstant.TextAlign = ContentAlignment.MiddleCenter;
            btnSpeedInstant.UseMnemonic = false;
            btnSpeedInstant.Click += btnSpeedInstant_Click;
            // 
            // pnlRow3
            // 
            pnlRow3.BackColor = Color.Transparent;
            pnlRow3.Controls.Add(pnlRow3Header);
            pnlRow3.Controls.Add(pnlRow3Body);
            pnlRow3.Cursor = Cursors.Hand;
            pnlRow3.Location = new Point(0, 408);
            pnlRow3.Name = "pnlRow3";
            pnlRow3.Size = new Size(1020, 62);
            pnlRow3.TabIndex = 2;
            // 
            // pnlRow3Header
            // 
            pnlRow3Header.BackColor = Color.Transparent;
            pnlRow3Header.Controls.Add(lblRow3Num);
            pnlRow3Header.Controls.Add(lblRow3Title);
            pnlRow3Header.Controls.Add(lblRow3Summary);
            pnlRow3Header.Controls.Add(lblRow3Toggle);
            pnlRow3Header.Cursor = Cursors.Hand;
            pnlRow3Header.Location = new Point(0, 0);
            pnlRow3Header.Name = "pnlRow3Header";
            pnlRow3Header.Size = new Size(1020, 52);
            pnlRow3Header.TabIndex = 0;
            // 
            // lblRow3Num
            // 
            lblRow3Num.AutoSize = true;
            lblRow3Num.BackColor = Color.Transparent;
            lblRow3Num.Font = new Font("IM FELL DW Pica SC", 20F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow3Num.ForeColor = Color.FromArgb(145, 65, 45);
            lblRow3Num.Location = new Point(18, 6);
            lblRow3Num.Name = "lblRow3Num";
            lblRow3Num.Size = new Size(41, 41);
            lblRow3Num.TabIndex = 0;
            lblRow3Num.Text = "03";
            lblRow3Num.UseMnemonic = false;
            // 
            // lblRow3Title
            // 
            lblRow3Title.AutoSize = true;
            lblRow3Title.BackColor = Color.Transparent;
            lblRow3Title.Font = new Font("IM FELL DW Pica SC", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow3Title.ForeColor = Color.FromArgb(20, 12, 6);
            lblRow3Title.Location = new Point(70, 9);
            lblRow3Title.Name = "lblRow3Title";
            lblRow3Title.Size = new Size(330, 33);
            lblRow3Title.TabIndex = 1;
            lblRow3Title.Text = "STATION ARCHIVE PURGE";
            lblRow3Title.UseMnemonic = false;
            // 
            // lblRow3Summary
            // 
            lblRow3Summary.BackColor = Color.Transparent;
            lblRow3Summary.Font = new Font("IM FELL DW Pica SC", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow3Summary.ForeColor = Color.FromArgb(85, 65, 45);
            lblRow3Summary.Location = new Point(420, 14);
            lblRow3Summary.Name = "lblRow3Summary";
            lblRow3Summary.Size = new Size(425, 24);
            lblRow3Summary.TabIndex = 2;
            lblRow3Summary.Text = "3 Slots Managed · Persistent JSON";
            lblRow3Summary.TextAlign = ContentAlignment.MiddleLeft;
            lblRow3Summary.UseMnemonic = false;
            // 
            // lblRow3Toggle
            // 
            lblRow3Toggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblRow3Toggle.BackColor = Color.Transparent;
            lblRow3Toggle.Cursor = Cursors.Hand;
            lblRow3Toggle.Font = new Font("IM FELL DW Pica SC", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRow3Toggle.ForeColor = Color.FromArgb(40, 25, 12);
            lblRow3Toggle.Location = new Point(860, 11);
            lblRow3Toggle.Name = "lblRow3Toggle";
            lblRow3Toggle.Size = new Size(145, 30);
            lblRow3Toggle.TabIndex = 3;
            lblRow3Toggle.Text = "▼ OPEN FILE";
            lblRow3Toggle.TextAlign = ContentAlignment.MiddleCenter;
            lblRow3Toggle.UseMnemonic = false;
            // 
            // pnlRow3Body
            // 
            pnlRow3Body.BackColor = Color.Transparent;
            pnlRow3Body.Controls.Add(pnlPurgeAdvisoryCard);
            pnlRow3Body.Controls.Add(btnPurgeAll);
            pnlRow3Body.Location = new Point(0, 52);
            pnlRow3Body.Name = "pnlRow3Body";
            pnlRow3Body.Size = new Size(1020, 274);
            pnlRow3Body.TabIndex = 1;
            pnlRow3Body.Visible = false;
            // 
            // pnlPurgeAdvisoryCard
            // 
            pnlPurgeAdvisoryCard.BackColor = Color.Transparent;
            pnlPurgeAdvisoryCard.Controls.Add(lblPurgeHeader);
            pnlPurgeAdvisoryCard.Controls.Add(lblPurgeDetails);
            pnlPurgeAdvisoryCard.Controls.Add(lblPersistentInfo);
            pnlPurgeAdvisoryCard.Location = new Point(60, 14);
            pnlPurgeAdvisoryCard.Name = "pnlPurgeAdvisoryCard";
            pnlPurgeAdvisoryCard.Size = new Size(900, 136);
            pnlPurgeAdvisoryCard.TabIndex = 0;
            // 
            // lblPurgeHeader
            // 
            lblPurgeHeader.AutoSize = true;
            lblPurgeHeader.BackColor = Color.Transparent;
            lblPurgeHeader.Font = new Font("IM FELL DW Pica SC", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPurgeHeader.ForeColor = Color.FromArgb(160, 40, 22);
            lblPurgeHeader.Location = new Point(20, 12);
            lblPurgeHeader.Name = "lblPurgeHeader";
            lblPurgeHeader.Size = new Size(475, 25);
            lblPurgeHeader.TabIndex = 0;
            lblPurgeHeader.Text = "⚠ CAUTION: EMERGENCY STATION FACTORY RESET";
            lblPurgeHeader.UseMnemonic = false;
            // 
            // lblPurgeDetails
            // 
            lblPurgeDetails.BackColor = Color.Transparent;
            lblPurgeDetails.Font = new Font("IM FELL DW Pica", 10F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblPurgeDetails.ForeColor = Color.FromArgb(60, 42, 26);
            lblPurgeDetails.Location = new Point(20, 42);
            lblPurgeDetails.Name = "lblPurgeDetails";
            lblPurgeDetails.Size = new Size(860, 52);
            lblPurgeDetails.TabIndex = 1;
            lblPurgeDetails.Text = "Executing a purge permanently incinerates and deletes all shift records in Slots 1, 2, and 3 from disk. All logged call transcripts, operator statistics, and frequency progress will be permanently lost.";
            lblPurgeDetails.UseMnemonic = false;
            // 
            // lblPersistentInfo
            // 
            lblPersistentInfo.BackColor = Color.Transparent;
            lblPersistentInfo.Font = new Font("IM FELL DW Pica", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPersistentInfo.ForeColor = Color.FromArgb(85, 70, 50);
            lblPersistentInfo.Location = new Point(20, 100);
            lblPersistentInfo.Name = "lblPersistentInfo";
            lblPersistentInfo.Size = new Size(860, 24);
            lblPersistentInfo.TabIndex = 2;
            lblPersistentInfo.Text = "PERSISTENT PROTOCOL: Saves/settings.json · ACTIVE AUDIO HARDWARE: WIN32 MCI DIRECT";
            lblPersistentInfo.UseMnemonic = false;
            // 
            // btnPurgeAll
            // 
            btnPurgeAll.BackColor = Color.Transparent;
            btnPurgeAll.Cursor = Cursors.Hand;
            btnPurgeAll.Font = new Font("IM FELL DW Pica SC", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnPurgeAll.ForeColor = Color.FromArgb(160, 35, 20);
            btnPurgeAll.Location = new Point(220, 172);
            btnPurgeAll.Name = "btnPurgeAll";
            btnPurgeAll.Size = new Size(580, 52);
            btnPurgeAll.TabIndex = 1;
            btnPurgeAll.Text = "PURGE ALL OPERATOR SHIFTS (FACTORY RESET)";
            btnPurgeAll.TextAlign = ContentAlignment.MiddleCenter;
            btnPurgeAll.UseMnemonic = false;
            btnPurgeAll.Click += btnPurgeAll_Click;
            // 
            // lblFooterHelp
            // 
            lblFooterHelp.AutoSize = true;
            lblFooterHelp.BackColor = Color.Transparent;
            lblFooterHelp.Font = new Font("IM FELL DW Pica", 9.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblFooterHelp.ForeColor = Color.FromArgb(65, 48, 32);
            lblFooterHelp.Location = new Point(70, 688);
            lblFooterHelp.Name = "lblFooterHelp";
            lblFooterHelp.Size = new Size(420, 22);
            lblFooterHelp.TabIndex = 8;
            lblFooterHelp.Text = "Press ENTER to Apply && Save · Press ESC or ✕ to Discard";
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(214, 202, 178);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Font = new Font("IM FELL DW Pica SC", 11.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCancel.ForeColor = Color.FromArgb(40, 25, 12);
            btnCancel.Location = new Point(740, 676);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(160, 48);
            btnCancel.TabIndex = 9;
            btnCancel.Text = "DISCARD";
            btnCancel.TextAlign = ContentAlignment.MiddleCenter;
            btnCancel.UseMnemonic = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnApply
            // 
            btnApply.BackColor = Color.FromArgb(40, 28, 18);
            btnApply.Cursor = Cursors.Hand;
            btnApply.Font = new Font("IM FELL DW Pica SC", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnApply.ForeColor = Color.FromArgb(248, 244, 232);
            btnApply.Location = new Point(915, 676);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(175, 48);
            btnApply.TabIndex = 10;
            btnApply.Text = "APPLY & SAVE";
            btnApply.TextAlign = ContentAlignment.MiddleCenter;
            btnApply.UseMnemonic = false;
            btnApply.Click += btnApply_Click;
            // 
            // SettingsForm
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            ClientSize = new Size(1920, 1080);
            Controls.Add(pnlParchmentContainer);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.Manual;
            Text = "Civil Defense Station Settings";
            WindowState = FormWindowState.Maximized;
            Load += SettingsForm_Load;
            Resize += SettingsForm_Resize;
            KeyDown += SettingsForm_KeyDown;
            pnlParchmentContainer.ResumeLayout(false);
            pnlParchmentContainer.PerformLayout();
            pnlRowsContainer.ResumeLayout(false);
            pnlRow1.ResumeLayout(false);
            pnlRow1Header.ResumeLayout(false);
            pnlRow1Header.PerformLayout();
            pnlRow1Body.ResumeLayout(false);
            pnlRow2.ResumeLayout(false);
            pnlRow2Header.ResumeLayout(false);
            pnlRow2Header.PerformLayout();
            pnlRow2Body.ResumeLayout(false);
            pnlRow3.ResumeLayout(false);
            pnlRow3Header.ResumeLayout(false);
            pnlRow3Header.PerformLayout();
            pnlRow3Body.ResumeLayout(false);
            pnlPurgeAdvisoryCard.ResumeLayout(false);
            pnlPurgeAdvisoryCard.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlParchmentContainer;
        private Label lblHeaderTitle;
        private Label lblHeaderSubtitle;
        private Label lblClose;
        private Label lblSectionKicker;
        private Label lblSectionTitle;
        private Label lblTagline;
        private Panel pnlRowsContainer;
        private Panel pnlRow1;
        private Panel pnlRow1Header;
        private Label lblRow1Num;
        private Label lblRow1Title;
        private Label lblRow1Summary;
        private Label lblRow1Toggle;
        private Panel pnlRow1Body;
        private Label lblAudioDesc;
        private Label chkMuteAll;
        private Label lblAudioHint;
        private Label lblMaster;
        private Label lblMasterVal;
        private StayOnTheLine.Controls.VintageTrackBar tbMasterVolume;
        private Label lblAmbient;
        private Label lblAmbientVal;
        private StayOnTheLine.Controls.VintageTrackBar tbAmbientVolume;
        private Label lblSfx;
        private Label lblSfxVal;
        private StayOnTheLine.Controls.VintageTrackBar tbSfxVolume;
        private Panel pnlRow2;
        private Panel pnlRow2Header;
        private Label lblRow2Num;
        private Label lblRow2Title;
        private Label lblRow2Summary;
        private Label lblRow2Toggle;
        private Panel pnlRow2Body;
        private Label lblTeletypeDesc;
        private Label lblRow2Status;
        private Label btnSpeedSlow;
        private Label btnSpeedStandard;
        private Label btnSpeedInstant;
        private Panel pnlRow3;
        private Panel pnlRow3Header;
        private Label lblRow3Num;
        private Label lblRow3Title;
        private Label lblRow3Summary;
        private Label lblRow3Toggle;
        private Panel pnlRow3Body;
        private Panel pnlPurgeAdvisoryCard;
        private Label lblPurgeHeader;
        private Label lblPurgeDetails;
        private Label lblPersistentInfo;
        private Label btnPurgeAll;
        private Label lblFooterHelp;
        private Label btnCancel;
        private Label btnApply;
    }
}
