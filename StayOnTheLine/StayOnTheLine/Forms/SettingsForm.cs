using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using StayOnTheLine.Models;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION & POLYMORPHISM
    /// Encapsulates player preferences modal interface, real-time audio calibration,
    /// teletype print speed adjustments, persistent JSON storage, and station archive purge operations.
    /// Features an archival collapsible accordion ledger, transparent vintage control styling,
    /// non-boxy contrasting buttons, and a deeply darkened CRT room backdrop matching New Shift (OperatorArchiveForm).
    /// </summary>
    public partial class SettingsForm : Form
    {
        private GameSettings _pendingSettings;
        private readonly GameSettings _initialSettings;
        private bool _isInitializing = true;
        private int _expandedSection = 0; // 0 = Audio, 1 = Teletype, 2 = Archive, -1 = All Collapsed
        private int _hoveredRowIndex = -1;
        private readonly Panel[] _rowPanels;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth hardware/OS-level double buffering
                return cp;
            }
        }

        public SettingsForm()
        {
            InitializeComponent();

            _initialSettings = SettingsManager.Instance.Current;
            _pendingSettings = new GameSettings(
                _initialSettings.MasterVolume,
                _initialSettings.AmbientVolume,
                _initialSettings.SfxVolume,
                _initialSettings.IsMuted,
                _initialSettings.PrintSpeed);

            _rowPanels = new[] { pnlRow1, pnlRow2, pnlRow3 };

            EnableDoubleBuffering(pnlParchmentContainer);
            EnableDoubleBuffering(pnlRowsContainer);
            foreach (var row in _rowPanels)
            {
                EnableDoubleBuffering(row);
            }
            EnableDoubleBuffering(pnlRow1Body);
            EnableDoubleBuffering(pnlRow2Body);
            EnableDoubleBuffering(pnlRow3Body);
            EnableDoubleBuffering(pnlPurgeAdvisoryCard);

            ApplyCustomFonts();
            LoadBackgroundImages();
            SetupCustomRowPainting();
            SetupCustomCardPainting();
            WireSliderEvents();
            WireButtonEvents();
            WireAccordionEvents();

            LoadSettingsIntoUi();
            SetExpandedSection(0, playSound: false);

            _isInitializing = false;
        }

        private static void EnableDoubleBuffering(Control control)
        {
            try
            {
                typeof(Control).InvokeMember(
                    "DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic,
                    null, control, new object[] { true });
            }
            catch { }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            CenterLedger();
            AudioManager.Instance.PlayPaperRustle();
        }

        private void SettingsForm_Resize(object sender, EventArgs e)
        {
            CenterLedger();
            Invalidate();
        }

        private void CenterLedger()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            pnlParchmentContainer.Left = (ClientSize.Width - pnlParchmentContainer.Width) / 2;
            pnlParchmentContainer.Top = (ClientSize.Height - pnlParchmentContainer.Height) / 2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawScreenVignette(e.Graphics);
        }

        private void DrawScreenVignette(Graphics g)
        {
            var rect = ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            // 1. Extra dark backdrop overlay (84% opacity) to deeply darken the room behind the settings ledger
            using (var darkOverlay = new SolidBrush(Color.FromArgb(215, 0, 0, 0)))
            {
                g.FillRectangle(darkOverlay, rect);
            }

            // 2. Analog CRT Vignette (shadowing the 4 corners of the dispatcher office)
            using (var path = new GraphicsPath())
            {
                path.AddRectangle(rect);
                using var pgb = new PathGradientBrush(path);
                pgb.CenterColor = Color.FromArgb(0, 0, 0, 0);
                pgb.SurroundColors = new[] { Color.FromArgb(245, 0, 0, 0) };
                pgb.FocusScales = new PointF(0.44f, 0.34f);
                g.FillRectangle(pgb, rect);
            }

            // 3. Analog CRT Scanlines
            using var scanlinePen = new Pen(Color.FromArgb(18, 0, 0, 0), 1f);
            for (int y = 0; y < rect.Height; y += 4)
            {
                g.DrawLine(scanlinePen, 0, y, rect.Width, y);
            }
        }

        private void LoadBackgroundImages()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string croppedParchment = Path.Combine(basePath, "Assets", "parchment_modal_bg.png");
                if (!File.Exists(croppedParchment))
                    croppedParchment = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "parchment_modal_bg.png");

                if (File.Exists(croppedParchment))
                {
                    pnlParchmentContainer.BackgroundImage = Image.FromFile(croppedParchment);
                    pnlParchmentContainer.BackgroundImageLayout = ImageLayout.Stretch;
                }
                else
                {
                    string ledgerBg = Path.Combine(basePath, "Assets", "creategame_bg.png");
                    if (!File.Exists(ledgerBg))
                        ledgerBg = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "creategame_bg.png");

                    if (File.Exists(ledgerBg))
                    {
                        pnlParchmentContainer.BackgroundImage = Image.FromFile(ledgerBg);
                        pnlParchmentContainer.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                }

                // Load dark room background
                string roomBg = Path.Combine(basePath, "Assets", "loadingandmainscreen_bg.png");
                if (!File.Exists(roomBg))
                    roomBg = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "loadingandmainscreen_bg.png");

                if (File.Exists(roomBg))
                {
                    BackgroundImage = Image.FromFile(roomBg);
                    BackgroundImageLayout = ImageLayout.Zoom;
                }
            }
            catch
            {
                pnlParchmentContainer.BackColor = Color.FromArgb(218, 208, 185);
            }
        }

        private void ApplyCustomFonts()
        {
            lblHeaderTitle.Font = FontManager.GetSmallCapsFont(30F, FontStyle.Bold);
            lblHeaderSubtitle.Font = FontManager.GetItalicFont(12.5F);
            lblSectionKicker.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            lblSectionTitle.Font = FontManager.GetSmallCapsFont(20F, FontStyle.Bold);
            lblTagline.Font = FontManager.GetItalicFont(11.5F);

            // Row 01
            lblRow1Num.Font = FontManager.GetSmallCapsFont(20F, FontStyle.Bold);
            lblRow1Title.Font = FontManager.GetSmallCapsFont(16F, FontStyle.Bold);
            lblRow1Summary.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            lblRow1Toggle.Font = FontManager.GetSmallCapsFont(9.5F, FontStyle.Bold);
            lblAudioDesc.Font = FontManager.GetItalicFont(10F);
            chkMuteAll.Font = FontManager.GetSmallCapsFont(10F, FontStyle.Bold);
            lblAudioHint.Font = FontManager.GetItalicFont(8.5F);

            lblMaster.Font = FontManager.GetSmallCapsFont(10F, FontStyle.Bold);
            lblMasterVal.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            lblAmbient.Font = FontManager.GetSmallCapsFont(10F, FontStyle.Bold);
            lblAmbientVal.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            lblSfx.Font = FontManager.GetSmallCapsFont(10F, FontStyle.Bold);
            lblSfxVal.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);

            // Row 02
            lblRow2Num.Font = FontManager.GetSmallCapsFont(20F, FontStyle.Bold);
            lblRow2Title.Font = FontManager.GetSmallCapsFont(16F, FontStyle.Bold);
            lblRow2Summary.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            lblRow2Toggle.Font = FontManager.GetSmallCapsFont(9.5F, FontStyle.Bold);
            lblTeletypeDesc.Font = FontManager.GetItalicFont(11F);
            lblRow2Status.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            btnSpeedSlow.Font = FontManager.GetSmallCapsFont(9.5F, FontStyle.Bold);
            btnSpeedStandard.Font = FontManager.GetSmallCapsFont(9.5F, FontStyle.Bold);
            btnSpeedInstant.Font = FontManager.GetSmallCapsFont(9.5F, FontStyle.Bold);

            // Row 03
            lblRow3Num.Font = FontManager.GetSmallCapsFont(20F, FontStyle.Bold);
            lblRow3Title.Font = FontManager.GetSmallCapsFont(16F, FontStyle.Bold);
            lblRow3Summary.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            lblRow3Toggle.Font = FontManager.GetSmallCapsFont(9.5F, FontStyle.Bold);
            lblPurgeHeader.Font = FontManager.GetSmallCapsFont(12F, FontStyle.Bold);
            lblPurgeDetails.Font = FontManager.GetItalicFont(10F);
            lblPersistentInfo.Font = FontManager.GetItalicFont(9F);
            btnPurgeAll.Font = FontManager.GetSmallCapsFont(12F, FontStyle.Bold);

            // Footer
            lblFooterHelp.Font = FontManager.GetItalicFont(9.5F);
            btnCancel.Font = FontManager.GetSmallCapsFont(11.5F, FontStyle.Bold);
            btnApply.Font = FontManager.GetSmallCapsFont(12F, FontStyle.Bold);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Toggles section state: if already open, collapses it; otherwise expands it.
        /// </summary>
        public void ToggleSection(int index, bool playSound = true)
        {
            if (_expandedSection == index)
            {
                // Uncollapse / close currently open section
                SetExpandedSection(-1, playSound);
            }
            else
            {
                // Expand clicked section
                SetExpandedSection(index, playSound);
            }
        }

        /// <summary>
        /// Sets the expanded accordion row (0, 1, 2) or collapses all (-1).
        /// Spacing is dynamically distributed inside pnlRowsContainer.
        /// </summary>
        public void SetExpandedSection(int index, bool playSound = true)
        {
            if (index < -1 || index >= 3) return;

            bool changed = (_expandedSection != index);
            _expandedSection = index;

            int expandedHeight = 326;
            int collapsedHeight = 52;
            int gap = 8;
            int currentTop = 0;

            for (int i = 0; i < 3; i++)
            {
                bool isExpanded = (i == _expandedSection);
                _rowPanels[i].Top = currentTop;
                _rowPanels[i].Height = isExpanded ? expandedHeight : collapsedHeight;
                currentTop += _rowPanels[i].Height + gap;
            }

            pnlRow1Body.Visible = (_expandedSection == 0);
            pnlRow2Body.Visible = (_expandedSection == 1);
            pnlRow3Body.Visible = (_expandedSection == 2);

            lblRow1Toggle.Text = (_expandedSection == 0) ? "▲ ACTIVE FILE" : "▼ OPEN FILE";
            lblRow2Toggle.Text = (_expandedSection == 1) ? "▲ ACTIVE FILE" : "▼ OPEN FILE";
            lblRow3Toggle.Text = (_expandedSection == 2) ? "▲ ACTIVE FILE" : "▼ OPEN FILE";

            lblRow1Toggle.ForeColor = (_expandedSection == 0) ? Color.FromArgb(170, 45, 25) : Color.FromArgb(70, 50, 30);
            lblRow2Toggle.ForeColor = (_expandedSection == 1) ? Color.FromArgb(170, 45, 25) : Color.FromArgb(70, 50, 30);
            lblRow3Toggle.ForeColor = (_expandedSection == 2) ? Color.FromArgb(170, 45, 25) : Color.FromArgb(70, 50, 30);

            // Clean header when expanded - hide summary while active file is open
            lblRow1Summary.Visible = (_expandedSection != 0);
            lblRow2Summary.Visible = (_expandedSection != 1);
            lblRow3Summary.Visible = (_expandedSection != 2);

            UpdateSummaryTexts();

            foreach (var row in _rowPanels)
            {
                row.Invalidate();
            }
            lblRow1Toggle.Invalidate();
            lblRow2Toggle.Invalidate();
            lblRow3Toggle.Invalidate();

            if (changed && playSound && !_isInitializing)
            {
                AudioManager.Instance.PlayPaperRustle();
            }
        }

        private void UpdateSummaryTexts()
        {
            // Auto-position summary labels right after their titles to prevent any overlap
            lblRow1Summary.Left = lblRow1Title.Right + 18;
            lblRow1Summary.Width = Math.Max(200, lblRow1Toggle.Left - lblRow1Summary.Left - 10);

            lblRow2Summary.Left = lblRow2Title.Right + 18;
            lblRow2Summary.Width = Math.Max(200, lblRow2Toggle.Left - lblRow2Summary.Left - 10);

            lblRow3Summary.Left = lblRow3Title.Right + 18;
            lblRow3Summary.Width = Math.Max(200, lblRow3Toggle.Left - lblRow3Summary.Left - 10);

            lblRow1Summary.BringToFront();
            lblRow2Summary.BringToFront();
            lblRow3Summary.BringToFront();

            // Row 1 Summary
            if (_pendingSettings.IsMuted)
            {
                lblRow1Summary.Text = "[ ALL FREQUENCIES MUTED ]";
                lblRow1Summary.ForeColor = Color.FromArgb(160, 45, 25);
            }
            else
            {
                lblRow1Summary.Text = $"Master: {_pendingSettings.MasterVolume}%  ·  Ambience: {_pendingSettings.AmbientVolume}%  ·  SFX: {_pendingSettings.SfxVolume}%";
                lblRow1Summary.ForeColor = Color.FromArgb(85, 65, 45);
            }

            // Row 2 Summary
            string cadenceName = _pendingSettings.PrintSpeed switch
            {
                TextSpeed.Slow => "Slow (65ms / char)",
                TextSpeed.Instant => "Instant (0ms / char)",
                _ => "Standard (30ms / char)"
            };
            lblRow2Summary.Text = $"Active Cadence: {cadenceName}";

            // Row 3 Summary
            lblRow3Summary.Text = "3 Shift Slots  ·  Persistent Protocol Saves/settings.json";
        }

        private void WireAccordionEvents()
        {
            WireHeaderClick(pnlRow1Header, 0, lblRow1Num, lblRow1Title, lblRow1Summary, lblRow1Toggle);
            WireHeaderClick(pnlRow2Header, 1, lblRow2Num, lblRow2Title, lblRow2Summary, lblRow2Toggle);
            WireHeaderClick(pnlRow3Header, 2, lblRow3Num, lblRow3Title, lblRow3Summary, lblRow3Toggle);
        }

        private void WireHeaderClick(Control header, int sectionIndex, params Control[] children)
        {
            header.Cursor = Cursors.Hand;
            header.Click += (s, e) => ToggleSection(sectionIndex);

            foreach (var child in children)
            {
                child.Cursor = Cursors.Hand;
                child.Click += (s, e) => ToggleSection(sectionIndex);
            }
        }

        private void SetupCustomRowPainting()
        {
            for (int i = 0; i < 3; i++)
            {
                int capturedIndex = i;
                _rowPanels[i].Paint += (s, e) =>
                {
                    bool isExpanded = (_expandedSection == capturedIndex);
                    bool isHovered = (_hoveredRowIndex == capturedIndex);
                    var rect = _rowPanels[capturedIndex].ClientRectangle;

                    // Subtle translucent ledger wash (allows parchment texture and watermark to shine through, matching New Shift)
                    Color washColor;
                    if (isExpanded)
                    {
                        washColor = isHovered
                            ? Color.FromArgb(50, 255, 252, 240)
                            : Color.FromArgb(28, 255, 255, 248);
                    }
                    else
                    {
                        washColor = isHovered
                            ? Color.FromArgb(42, 255, 250, 235)
                            : Color.FromArgb(10, 0, 0, 0);
                    }

                    using (var washBrush = new SolidBrush(washColor))
                    {
                        e.Graphics.FillRectangle(washBrush, rect);
                    }

                    // Archival ledger horizontal divider rules
                    using (var linePen = new Pen(isExpanded
                        ? Color.FromArgb(170, 70, 48, 28)
                        : Color.FromArgb(90, 125, 100, 75), isExpanded ? 1.5f : 1f))
                    {
                        e.Graphics.DrawLine(linePen, 0, 0, rect.Width, 0);
                        e.Graphics.DrawLine(linePen, 0, rect.Height - 1, rect.Width, rect.Height - 1);
                    }

                    // Left red archival spine accent for expanded or hovered row
                    if (isExpanded || isHovered)
                    {
                        using var stripBrush = new SolidBrush(isExpanded
                            ? Color.FromArgb(170, 45, 30)
                            : Color.FromArgb(120, 80, 60, 40));
                        e.Graphics.FillRectangle(stripBrush, 0, 0, isExpanded ? 5 : 3, rect.Height);
                    }

                    // Subtle perimeter border if hovered
                    if (isHovered && !isExpanded)
                    {
                        using var hoverPen = new Pen(Color.FromArgb(130, 95, 70, 45), 1f);
                        e.Graphics.DrawRectangle(hoverPen, 0, 0, rect.Width - 1, rect.Height - 1);
                    }
                };
            }
        }

        private void SetupCustomCardPainting()
        {
            // Clear default WinForms Label text on all custom-painted controls to prevent duplicate/ghosted text
            lblRow1Toggle.Text = string.Empty;
            lblRow2Toggle.Text = string.Empty;
            lblRow3Toggle.Text = string.Empty;
            chkMuteAll.Text = string.Empty;
            btnSpeedSlow.Text = string.Empty;
            btnSpeedStandard.Text = string.Empty;
            btnSpeedInstant.Text = string.Empty;
            btnPurgeAll.Text = string.Empty;
            btnCancel.Text = string.Empty;
            btnApply.Text = string.Empty;

            // Row Toggles - Soft rounded pill badge
            Label[] toggles = { lblRow1Toggle, lblRow2Toggle, lblRow3Toggle };
            for (int i = 0; i < toggles.Length; i++)
            {
                int idx = i;
                var toggle = toggles[i];
                toggle.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    bool isExpanded = (_expandedSection == idx);
                    bool isHovered = toggle.ClientRectangle.Contains(toggle.PointToClient(Cursor.Position));
                    var r = new Rectangle(0, 0, toggle.Width - 1, toggle.Height - 1);
                    using var path = CreateRoundedPath(r, 4);

                    Color bgColor;
                    if (isExpanded)
                    {
                        bgColor = isHovered
                            ? Color.FromArgb(48, 175, 45, 25)
                            : Color.FromArgb(28, 175, 45, 25);
                    }
                    else
                    {
                        bgColor = isHovered
                            ? Color.FromArgb(32, 70, 50, 30)
                            : Color.FromArgb(12, 0, 0, 0);
                    }

                    using (var bgBrush = new SolidBrush(bgColor))
                    {
                        e.Graphics.FillPath(bgBrush, path);
                    }

                    using var pen = new Pen(isExpanded
                        ? Color.FromArgb(180, 50, 30)
                        : (isHovered ? Color.FromArgb(150, 110, 80) : Color.FromArgb(120, 95, 70)), isExpanded ? 1.5f : 1f);
                    e.Graphics.DrawPath(pen, path);

                    // Single-pass clean text rendering
                    string toggleText = isExpanded ? "▲ ACTIVE FILE" : "▼ OPEN FILE";
                    Color textColor = isExpanded
                        ? Color.FromArgb(170, 45, 25)
                        : (isHovered ? Color.FromArgb(145, 55, 30) : Color.FromArgb(70, 50, 30));

                    TextRenderer.DrawText(
                        e.Graphics,
                        toggleText,
                        toggle.Font,
                        r,
                        textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                };

                toggle.MouseEnter += (s, e) => toggle.Invalidate();
                toggle.MouseLeave += (s, e) => toggle.Invalidate();
            }

            // Mute checkbox button - Soft rounded corners
            chkMuteAll.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, chkMuteAll.Width - 1, chkMuteAll.Height - 1);
                using var path = CreateRoundedPath(r, 5);

                bool isHovered = chkMuteAll.ClientRectangle.Contains(chkMuteAll.PointToClient(Cursor.Position));
                Color bgColor;
                if (_pendingSettings.IsMuted)
                {
                    bgColor = isHovered
                        ? Color.FromArgb(105, 50, 38)
                        : Color.FromArgb(85, 45, 35);
                }
                else
                {
                    bgColor = isHovered
                        ? Color.FromArgb(30, 48, 34, 22)
                        : Color.Transparent;
                }

                if (bgColor != Color.Transparent)
                {
                    using var bgBrush = new SolidBrush(bgColor);
                    e.Graphics.FillPath(bgBrush, path);
                }

                using var pen = new Pen(_pendingSettings.IsMuted
                    ? Color.FromArgb(200, 180, 50, 40)
                    : (isHovered ? Color.FromArgb(160, 70, 45) : Color.FromArgb(125, 95, 70)), 1.5f);
                e.Graphics.DrawPath(pen, path);

                // Single-pass clean text rendering
                string muteText = _pendingSettings.IsMuted ? "[ ✕ ] ALL FREQUENCIES MUTED" : "[   ] MUTE ALL FREQUENCIES";
                Color textColor = _pendingSettings.IsMuted
                    ? Color.FromArgb(250, 235, 220)
                    : (isHovered ? Color.FromArgb(160, 70, 45) : Color.FromArgb(40, 25, 12));

                TextRenderer.DrawText(
                    e.Graphics,
                    muteText,
                    chkMuteAll.Font,
                    r,
                    textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };

            // Speed selection cards - Soft rounded corners & gold frame
            Label[] speedCards = { btnSpeedSlow, btnSpeedStandard, btnSpeedInstant };
            string[] speedCardTexts = {
                "SLOW\r\n\r\n65 MS / CHAR\r\n\r\nAuthentic manual typewriter cadence.\r\nDeliberate character printing.",
                "STANDARD\r\n\r\n30 MS / CHAR\r\n\r\nRecommended dispatch pacing.\r\nBalanced reading flow.",
                "INSTANT\r\n\r\n0 MS / CHAR\r\n\r\nImmediate line readout.\r\nBypasses typewriter delay."
            };

            for (int i = 0; i < speedCards.Length; i++)
            {
                int cardIdx = i;
                var card = speedCards[i];
                card.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    bool isActive = (cardIdx == 0 && _pendingSettings.PrintSpeed == TextSpeed.Slow) ||
                                    (cardIdx == 1 && _pendingSettings.PrintSpeed == TextSpeed.Standard) ||
                                    (cardIdx == 2 && _pendingSettings.PrintSpeed == TextSpeed.Instant);
                    bool isHovered = card.ClientRectangle.Contains(card.PointToClient(Cursor.Position));

                    var r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                    using var path = CreateRoundedPath(r, 6);

                    Color bgColor;
                    if (isActive)
                    {
                        bgColor = isHovered ? Color.FromArgb(60, 42, 28) : Color.FromArgb(45, 32, 20);
                    }
                    else
                    {
                        bgColor = isHovered ? Color.FromArgb(30, 48, 34, 22) : Color.Transparent;
                    }

                    if (bgColor != Color.Transparent)
                    {
                        using var bgBrush = new SolidBrush(bgColor);
                        e.Graphics.FillPath(bgBrush, path);
                    }

                    using var borderPen = new Pen(isActive
                        ? Color.FromArgb(200, 160, 110)
                        : (isHovered ? Color.FromArgb(160, 120, 85) : Color.FromArgb(120, 95, 70)), isActive ? 1.75f : 1f);
                    e.Graphics.DrawPath(borderPen, path);

                    if (isActive)
                    {
                        var innerR = new Rectangle(3, 3, card.Width - 7, card.Height - 7);
                        using var innerPath = CreateRoundedPath(innerR, 4);
                        using var innerPen = new Pen(Color.FromArgb(90, 200, 160, 110), 1f);
                        e.Graphics.DrawPath(innerPen, innerPath);
                    }

                    // Single-pass clean text rendering
                    Color textColor = isActive
                        ? Color.FromArgb(248, 244, 232)
                        : (isHovered ? Color.FromArgb(140, 50, 30) : Color.FromArgb(45, 30, 15));

                    TextRenderer.DrawText(
                        e.Graphics,
                        speedCardTexts[cardIdx],
                        card.Font,
                        r,
                        textColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak);
                };
            }

            // Advisory Card - Rounded dashed hazard border
            pnlPurgeAdvisoryCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, pnlPurgeAdvisoryCard.Width - 1, pnlPurgeAdvisoryCard.Height - 1);
                using var path = CreateRoundedPath(r, 6);

                using (var wash = new SolidBrush(Color.FromArgb(22, 160, 40, 25)))
                {
                    e.Graphics.FillPath(wash, path);
                }

                using var pen = new Pen(Color.FromArgb(140, 150, 45, 30), 1f);
                pen.DashStyle = DashStyle.Dash;
                e.Graphics.DrawPath(pen, path);
            };

            // Purge All Button - Rounded hazard button
            btnPurgeAll.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, btnPurgeAll.Width - 1, btnPurgeAll.Height - 1);
                using var path = CreateRoundedPath(r, 6);

                bool isHovered = btnPurgeAll.ClientRectangle.Contains(btnPurgeAll.PointToClient(Cursor.Position));
                Color bgColor = isHovered
                    ? Color.FromArgb(150, 42, 28)
                    : Color.FromArgb(32, 175, 45, 30);

                using (var bgBrush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                using var pen = new Pen(isHovered
                    ? Color.FromArgb(210, 65, 45)
                    : Color.FromArgb(170, 50, 35), 1.5f);
                e.Graphics.DrawPath(pen, path);

                var innerR = new Rectangle(2, 2, btnPurgeAll.Width - 5, btnPurgeAll.Height - 5);
                using var innerPath = CreateRoundedPath(innerR, 4);
                using (var innerPen = new Pen(isHovered
                    ? Color.FromArgb(90, 255, 200, 180)
                    : Color.FromArgb(40, 180, 50, 35), 1f))
                {
                    e.Graphics.DrawPath(innerPen, innerPath);
                }

                // Single-pass clean text rendering
                Color textColor = isHovered
                    ? Color.FromArgb(252, 244, 238)
                    : Color.FromArgb(165, 38, 22);

                TextRenderer.DrawText(
                    e.Graphics,
                    "PURGE ALL OPERATOR SHIFTS (FACTORY RESET)",
                    btnPurgeAll.Font,
                    r,
                    textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };

            // Discard (Cancel) Button - Warm contrasting aged parchment plaque with soft rounded edges
            btnCancel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, btnCancel.Width - 1, btnCancel.Height - 1);
                using var path = CreateRoundedPath(rect, 6);

                bool isHovered = btnCancel.ClientRectangle.Contains(btnCancel.PointToClient(Cursor.Position));
                Color bgColor = isHovered
                    ? Color.FromArgb(228, 218, 196)
                    : Color.FromArgb(214, 202, 178);

                using (var bgBrush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                // Outer antique sepia border
                using (var borderPen = new Pen(isHovered
                    ? Color.FromArgb(160, 65, 45)
                    : Color.FromArgb(135, 105, 75), 1.5f))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }

                // Inner subtle debossed highlight frame
                var innerRect = new Rectangle(2, 2, btnCancel.Width - 5, btnCancel.Height - 5);
                using (var innerPath = CreateRoundedPath(innerRect, 4))
                using (var innerPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f))
                {
                    e.Graphics.DrawPath(innerPen, innerPath);
                }

                // Single-pass clean text rendering
                Color textColor = isHovered ? Color.FromArgb(160, 45, 30) : Color.FromArgb(42, 28, 16);
                TextRenderer.DrawText(
                    e.Graphics,
                    "DISCARD",
                    btnCancel.Font,
                    rect,
                    textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };

            // Apply & Save Button - Dark chocolate archival ink seal with gold rounded frame
            btnApply.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, btnApply.Width - 1, btnApply.Height - 1);
                using var path = CreateRoundedPath(rect, 6);

                bool isHovered = btnApply.ClientRectangle.Contains(btnApply.PointToClient(Cursor.Position));
                Color bgColor = isHovered
                    ? Color.FromArgb(62, 46, 32)
                    : Color.FromArgb(42, 30, 20);

                using (var bgBrush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillPath(bgBrush, path);
                }

                // Outer antique gold border
                using (var borderPen = new Pen(isHovered
                    ? Color.FromArgb(215, 175, 115)
                    : Color.FromArgb(185, 145, 95), 1.5f))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }

                // Inner delicate gold highlight
                var innerRect = new Rectangle(2, 2, btnApply.Width - 5, btnApply.Height - 5);
                using (var innerPath = CreateRoundedPath(innerRect, 4))
                using (var innerPen = new Pen(Color.FromArgb(70, 220, 180, 130), 1f))
                {
                    e.Graphics.DrawPath(innerPen, innerPath);
                }

                // Single-pass clean text rendering
                Color textColor = isHovered ? Color.FromArgb(255, 230, 180) : Color.FromArgb(248, 244, 232);
                TextRenderer.DrawText(
                    e.Graphics,
                    "APPLY & SAVE",
                    btnApply.Font,
                    rect,
                    textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
        }

        private void CheckRowHover(int index)
        {
            if (_hoveredRowIndex != index)
            {
                _hoveredRowIndex = index;
                foreach (var row in _rowPanels)
                {
                    row.Invalidate();
                }
                if (index >= 0)
                {
                    AudioManager.Instance.PlayFrequencyBlip();
                }
            }
        }

        private void WireSliderEvents()
        {
            tbMasterVolume.ValueChanged += (s, e) =>
            {
                if (_isInitializing) return;
                _pendingSettings.MasterVolume = tbMasterVolume.Value;
                lblMasterVal.Text = $"{tbMasterVolume.Value}%";
                UpdateSummaryTexts();
                AudioManager.Instance.ApplySettings(_pendingSettings);
            };

            tbAmbientVolume.ValueChanged += (s, e) =>
            {
                if (_isInitializing) return;
                _pendingSettings.AmbientVolume = tbAmbientVolume.Value;
                lblAmbientVal.Text = $"{tbAmbientVolume.Value}%";
                UpdateSummaryTexts();
                AudioManager.Instance.ApplySettings(_pendingSettings);
            };

            tbSfxVolume.ValueChanged += (s, e) =>
            {
                if (_isInitializing) return;
                _pendingSettings.SfxVolume = tbSfxVolume.Value;
                lblSfxVal.Text = $"{tbSfxVolume.Value}%";
                UpdateSummaryTexts();
                AudioManager.Instance.ApplySettings(_pendingSettings);
            };

            tbSfxVolume.Scroll += (s, e) =>
            {
                if (!_isInitializing) AudioManager.Instance.PlayFrequencyBlip();
            };
        }

        private void WireButtonEvents()
        {
            // Wire row hover tracking
            for (int i = 0; i < 3; i++)
            {
                int capturedIndex = i;
                Panel panel = _rowPanels[i];
                panel.MouseEnter += (s, e) => CheckRowHover(capturedIndex);
                panel.MouseLeave += (s, e) =>
                {
                    if (!panel.ClientRectangle.Contains(panel.PointToClient(Cursor.Position)))
                        CheckRowHover(-1);
                };

                foreach (Control child in panel.Controls)
                {
                    if (child is not StayOnTheLine.Controls.VintageTrackBar)
                    {
                        child.MouseEnter += (s, e) => CheckRowHover(capturedIndex);
                    }
                }
            }

            // Close button
            lblClose.MouseEnter += (s, e) => lblClose.ForeColor = Color.FromArgb(160, 30, 20);
            lblClose.MouseLeave += (s, e) => lblClose.ForeColor = Color.FromArgb(40, 25, 12);

            // Mute checkbox hover
            chkMuteAll.MouseEnter += (s, e) =>
            {
                if (!_pendingSettings.IsMuted) chkMuteAll.ForeColor = Color.FromArgb(160, 70, 45);
                chkMuteAll.Invalidate();
            };
            chkMuteAll.MouseLeave += (s, e) =>
            {
                if (!_pendingSettings.IsMuted) chkMuteAll.ForeColor = Color.FromArgb(40, 25, 12);
                chkMuteAll.Invalidate();
            };

            // Speed cards hover
            WireSpeedHover(btnSpeedSlow, TextSpeed.Slow);
            WireSpeedHover(btnSpeedStandard, TextSpeed.Standard);
            WireSpeedHover(btnSpeedInstant, TextSpeed.Instant);

            // Cancel (Discard) button hover
            btnCancel.MouseEnter += (s, e) =>
            {
                AudioManager.Instance.PlayFrequencyBlip();
                btnCancel.ForeColor = Color.FromArgb(160, 45, 30);
                btnCancel.Invalidate();
            };
            btnCancel.MouseLeave += (s, e) =>
            {
                btnCancel.ForeColor = Color.FromArgb(42, 28, 16);
                btnCancel.Invalidate();
            };

            // Apply button hover
            btnApply.MouseEnter += (s, e) =>
            {
                AudioManager.Instance.PlayFrequencyBlip();
                btnApply.ForeColor = Color.FromArgb(255, 230, 180);
                btnApply.Invalidate();
            };
            btnApply.MouseLeave += (s, e) =>
            {
                btnApply.ForeColor = Color.FromArgb(248, 244, 232);
                btnApply.Invalidate();
            };

            // Purge All button hover
            btnPurgeAll.MouseEnter += (s, e) =>
            {
                AudioManager.Instance.PlayFrequencyBlip();
                btnPurgeAll.ForeColor = Color.FromArgb(252, 244, 238);
                btnPurgeAll.Invalidate();
            };
            btnPurgeAll.MouseLeave += (s, e) =>
            {
                btnPurgeAll.ForeColor = Color.FromArgb(165, 38, 22);
                btnPurgeAll.Invalidate();
            };
        }

        private void WireSpeedHover(Label card, TextSpeed speed)
        {
            card.MouseEnter += (s, e) =>
            {
                if (_pendingSettings.PrintSpeed != speed)
                {
                    AudioManager.Instance.PlayFrequencyBlip();
                    card.ForeColor = Color.FromArgb(140, 50, 30);
                }
                card.Invalidate();
            };
            card.MouseLeave += (s, e) =>
            {
                card.ForeColor = (_pendingSettings.PrintSpeed == speed) ? Color.FromArgb(248, 244, 232) : Color.FromArgb(45, 30, 15);
                card.Invalidate();
            };
        }

        private void LoadSettingsIntoUi()
        {
            tbMasterVolume.Value = _pendingSettings.MasterVolume;
            lblMasterVal.Text = $"{_pendingSettings.MasterVolume}%";

            tbAmbientVolume.Value = _pendingSettings.AmbientVolume;
            lblAmbientVal.Text = $"{_pendingSettings.AmbientVolume}%";

            tbSfxVolume.Value = _pendingSettings.SfxVolume;
            lblSfxVal.Text = $"{_pendingSettings.SfxVolume}%";

            UpdateMuteUi();
            UpdateSpeedUi();
            UpdateSummaryTexts();
        }

        private void UpdateMuteUi()
        {
            chkMuteAll.Text = string.Empty;
            chkMuteAll.Invalidate();
        }

        private void UpdateSpeedUi()
        {
            btnSpeedSlow.Text = string.Empty;
            btnSpeedStandard.Text = string.Empty;
            btnSpeedInstant.Text = string.Empty;

            btnSpeedSlow.Invalidate();
            btnSpeedStandard.Invalidate();
            btnSpeedInstant.Invalidate();

            lblRow2Status.Text = _pendingSettings.PrintSpeed switch
            {
                TextSpeed.Slow => "[ ACTIVE CADENCE: SLOW · 65 MS / CHAR ]",
                TextSpeed.Instant => "[ ACTIVE CADENCE: INSTANT · 0 MS / CHAR ]",
                _ => "[ ACTIVE CADENCE: STANDARD · 30 MS / CHAR ]"
            };

            UpdateSummaryTexts();
        }

        private void chkMuteAll_Click(object sender, EventArgs e)
        {
            _pendingSettings.IsMuted = !_pendingSettings.IsMuted;
            UpdateMuteUi();
            UpdateSummaryTexts();
            AudioManager.Instance.ApplySettings(_pendingSettings);
            if (!_pendingSettings.IsMuted) AudioManager.Instance.PlayFrequencyBlip();
        }

        private void btnSpeedSlow_Click(object sender, EventArgs e)
        {
            _pendingSettings.PrintSpeed = TextSpeed.Slow;
            UpdateSpeedUi();
            AudioManager.Instance.PlayTypewriterKeystroke();
        }

        private void btnSpeedStandard_Click(object sender, EventArgs e)
        {
            _pendingSettings.PrintSpeed = TextSpeed.Standard;
            UpdateSpeedUi();
            AudioManager.Instance.PlayTypewriterKeystroke();
        }

        private void btnSpeedInstant_Click(object sender, EventArgs e)
        {
            _pendingSettings.PrintSpeed = TextSpeed.Instant;
            UpdateSpeedUi();
            AudioManager.Instance.PlayTypewriterBell();
        }

        private void btnPurgeAll_Click(object sender, EventArgs e)
        {
            AudioManager.Instance.PlayFrequencyBlip();

            var result = MessageBox.Show(
                this,
                "CONFIRM PURGE OF ALL OPERATOR ARCHIVES?\n\nThis will permanently incinerate and delete shift records in Slots 1, 2, and 3 from disk.\nAll recorded frequency progress will be permanently lost.\n\nProceed with factory wipe?",
                "Emergency Station Purge Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                SettingsManager.Instance.PurgeAllShiftLogs();
                AudioManager.Instance.PlayBurnHiss();

                MessageBox.Show(
                    this,
                    "ALL OPERATOR ARCHIVES PURGED.\nArchive slots 1, 2, and 3 are now vacant and reset.",
                    "Station Archive Cleared",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SettingsManager.Instance.SaveSettings(_pendingSettings);
            AudioManager.Instance.PlayInkStamp();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // Revert audio settings to original before dialog opened
            AudioManager.Instance.ApplySettings(_initialSettings);
            AudioManager.Instance.PlayFrequencyBlip();
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void lblClose_Click(object sender, EventArgs e)
        {
            btnCancel_Click(sender, e);
        }

        private void SettingsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                btnCancel_Click(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter && !btnPurgeAll.Focused)
            {
                btnApply_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
