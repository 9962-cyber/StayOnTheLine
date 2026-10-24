using System.ComponentModel;
using StayOnTheLine.Models;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principles Demonstrated:
    /// 1. ENCAPSULATION: Operates against the ISaveService contract to manage save state.
    /// 2. POLYMORPHISM: Dynamically renders slot rows as active profile cards or
    ///    interactive "+ CREATE NEW SHIFT" action triggers based on slot occupancy.
    /// 3. INHERITANCE: Inherits from System.Windows.Forms.Form.
    /// </summary>
    public partial class OperatorArchiveForm : Form
    {
        private readonly ISaveService _saveService;
        private readonly Panel[] _slotPanels;
        private readonly Label[] _slotNums;
        private readonly Label[] _slotNames;
        private readonly Label[] _slotDetails;
        private readonly Label[] _slotTimes;
        private readonly Label[] _slotDeletes;

        private int _currentHoveredIndex = -1;
        private int? _mostRecentSlot;
        private Font? _activeStampFont;
        private DesignScaler? _scaler;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OperatorProfile? SelectedProfile { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsNewShiftMode { get; set; } = false;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth hardware/OS-level double-buffering for all child controls
                return cp;
            }
        }

        public OperatorArchiveForm(ISaveService? saveService = null, bool isNewShiftMode = false)
        {
            InitializeComponent();
            _saveService = saveService ?? SaveManager.Instance;
            IsNewShiftMode = isNewShiftMode;

            _slotPanels = new[] { pnlSlot1, pnlSlot2, pnlSlot3 };
            _slotNums = new[] { lblSlot1Num, lblSlot2Num, lblSlot3Num };
            _slotNames = new[] { lblSlot1Name, lblSlot2Name, lblSlot3Name };
            _slotDetails = new[] { lblSlot1Details, lblSlot2Details, lblSlot3Details };
            _slotTimes = new[] { lblSlot1Time, lblSlot2Time, lblSlot3Time };
            _slotDeletes = new[] { lblSlot1Delete, lblSlot2Delete, lblSlot3Delete };

            EnableDoubleBuffering(pnlParchmentLedger);
            EnableDoubleBuffering(pnlSlotsContainer);
            foreach (var panel in _slotPanels)
            {
                EnableDoubleBuffering(panel);
            }

            ApplyFonts();
            LoadBackgroundImages();
            SetupCustomSlotPainting();
            WireSlotEvents();
            _scaler = new DesignScaler(this);
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
                    pnlParchmentLedger.BackgroundImage = Image.FromFile(croppedParchment);
                    pnlParchmentLedger.BackgroundImageLayout = ImageLayout.Stretch;
                }
                else
                {
                    string ledgerBg = Path.Combine(basePath, "Assets", "creategame_bg.png");
                    if (!File.Exists(ledgerBg))
                        ledgerBg = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "creategame_bg.png");

                    if (File.Exists(ledgerBg))
                    {
                        pnlParchmentLedger.BackgroundImage = Image.FromFile(ledgerBg);
                        pnlParchmentLedger.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                }

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
                pnlParchmentLedger.BackColor = Color.FromArgb(215, 205, 180);
            }
        }

                private void ApplyFonts()
        {
            lblHeaderStartGame.Font = FontManager.GetSmallCapsFont(30F, FontStyle.Bold);
            lblHeaderSubtitle.Font = FontManager.GetItalicFont(12.5F);
            lblSectionArchive.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            lblSectionChoose.Font = FontManager.GetSmallCapsFont(22F, FontStyle.Bold);
            lblQuote.Font = FontManager.GetItalicFont(11.5F);

            for (int i = 0; i < 3; i++)
            {
                _slotNums[i].Font = FontManager.GetSmallCapsFont(22F, FontStyle.Bold);
                _slotNames[i].Font = FontManager.GetSmallCapsFont(20F, FontStyle.Bold);
                _slotDetails[i].Font = FontManager.GetSmallCapsFont(11.5F, FontStyle.Bold);
                _slotTimes[i].Font = FontManager.GetSmallCapsFont(11.5F, FontStyle.Bold);
                _slotDeletes[i].Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            }
        }

                        private void SetupCustomSlotPainting()
        {
            for (int i = 0; i < 3; i++)
            {
                int capturedIndex = i;
                _slotPanels[i].Paint += (s, e) =>
                {
                    bool isHovered = (_currentHoveredIndex == capturedIndex);
                    var rect = _slotPanels[capturedIndex].ClientRectangle;

                    // Subtle translucent ledger wash (allows parchment texture and watermark to shine through)
                    using (var washBrush = new SolidBrush(isHovered
                        ? Color.FromArgb(65, 255, 252, 240)
                        : Color.FromArgb(20, 255, 255, 255)))
                    {
                        e.Graphics.FillRectangle(washBrush, rect);
                    }

                    // Archival ledger horizontal divider rules
                    using (var linePen = new Pen(isHovered
                        ? Color.FromArgb(200, 60, 40, 25)
                        : Color.FromArgb(90, 125, 100, 75), isHovered ? 1.5f : 1f))
                    {
                        e.Graphics.DrawLine(linePen, 0, 0, rect.Width, 0);
                        e.Graphics.DrawLine(linePen, 0, rect.Height - 1, rect.Width, rect.Height - 1);
                    }

                    // Hover border and spine accent
                    if (isHovered)
                    {
                        using var borderPen = new Pen(Color.FromArgb(180, 50, 30, 15), 1.5f);
                        e.Graphics.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1);

                        using var stripBrush = new SolidBrush(Color.FromArgb(170, 45, 30));
                        e.Graphics.FillRectangle(stripBrush, 0, 0, 4, rect.Height);
                    }

                    // VINTAGE RED INK ARCHIVAL STAMP: [ ACTIVE FREQUENCY ] + Paperclip
                    int currentSlotNum = capturedIndex + 1;
                    if (_mostRecentSlot.HasValue && _mostRecentSlot.Value == currentSlotNum)
                    {
                        var prof = _saveService.GetProfile(currentSlotNum);
                        if (prof != null)
                        {
                            DrawActiveFrequencyStamp(e.Graphics, isHovered, prof.OperatorName);
                        }
                    }
                };
            }
        }

                private void DrawActiveFrequencyStamp(Graphics g, bool isHovered, string operatorName)
        {
            _activeStampFont ??= FontManager.GetSmallCapsFont(9.5F, FontStyle.Bold);

            float nameWidth = 100;
            try
            {
                nameWidth = g.MeasureString(operatorName, _slotNames[0].Font).Width / (_scaler?.Factor ?? 1f);
            }
            catch { }

            // Stamp geometry is in 1920x1080 design units; scale it with the ledger
            var outerState = g.Save();
            float factor = _scaler?.Factor ?? 1f;
            g.ScaleTransform(factor, factor);

            float stampWidth = 205;
            float stampHeight = 30;
            float stampX = Math.Max(95 + nameWidth + 18, 195);
            if (stampX + stampWidth > 645)
            {
                stampX = 645 - stampWidth;
            }
            float stampY = 25;

            var stampRect = new RectangleF(stampX, stampY, stampWidth, stampHeight);
            var innerRect = new RectangleF(stampX + 2, stampY + 2, stampWidth - 4, stampHeight - 4);

            var state = g.Save();
            float cx = stampX + stampWidth / 2f;
            float cy = stampY + stampHeight / 2f;
            g.TranslateTransform(cx, cy);
            g.RotateTransform(-2.0f);
            g.TranslateTransform(-cx, -cy);

            // Double border rubber stamp
            using (var outerPen = new Pen(Color.FromArgb(isHovered ? 235 : 185, 160, 35, 22), 1.5f))
            {
                g.DrawRectangle(outerPen, stampRect.X, stampRect.Y, stampRect.Width, stampRect.Height);
            }

            using (var innerPen = new Pen(Color.FromArgb(isHovered ? 135 : 90, 160, 35, 22), 0.8f))
            {
                g.DrawRectangle(innerPen, innerRect.X, innerRect.Y, innerRect.Width, innerRect.Height);
            }

            using (var washBrush = new SolidBrush(Color.FromArgb(isHovered ? 38 : 20, 180, 40, 25)))
            {
                g.FillRectangle(washBrush, stampRect);
            }

            using (var textBrush = new SolidBrush(Color.FromArgb(isHovered ? 245 : 205, 155, 30, 18)))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                g.DrawString("[ ACTIVE FREQUENCY ]", _activeStampFont, textBrush, stampRect, sf);
            }

            g.Restore(state);

            // Vintage Brass Paperclip on the left top edge
            DrawVintagePaperclip(g, isHovered);
            g.Restore(outerState);
        }

        private static void DrawVintagePaperclip(Graphics g, bool isHovered)
        {
            using var clipPen = new Pen(isHovered ? Color.FromArgb(215, 200, 175) : Color.FromArgb(165, 145, 120), 2.2f);
            clipPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            clipPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            g.DrawArc(clipPen, 18, -4, 14, 18, 180, 180);
            g.DrawLine(clipPen, 18, 5, 18, 28);
            g.DrawLine(clipPen, 32, 5, 32, 34);
            g.DrawArc(clipPen, 22, 26, 10, 12, 0, 180);
            g.DrawLine(clipPen, 22, 32, 22, 12);
        }

        private void WireSlotEvents()
        {
            for (int i = 0; i < 3; i++)
            {
                int slotIndex = i + 1;
                int capturedIndex = i;
                Panel panel = _slotPanels[i];
                Label deleteBtn = _slotDeletes[i];

                // Click handlers
                panel.Click += (s, e) => OnSlotClicked(slotIndex);
                foreach (Control child in panel.Controls)
                {
                    if (child != deleteBtn)
                    {
                        child.Click += (s, e) => OnSlotClicked(slotIndex);
                        child.MouseEnter += (s, e) => CheckSlotHover(capturedIndex);
                        child.MouseLeave += (s, e) => CheckSlotHover(capturedIndex);
                    }
                }

                // Panel mouse hover
                panel.MouseEnter += (s, e) => CheckSlotHover(capturedIndex);
                panel.MouseLeave += (s, e) => CheckSlotHover(capturedIndex);

                // Delete click & hover
                deleteBtn.Click += (s, e) => OnDeleteSlotClicked(slotIndex);
                deleteBtn.MouseEnter += (s, e) =>
                {
                    deleteBtn.ForeColor = Color.FromArgb(220, 20, 20);
                    CheckSlotHover(capturedIndex);
                };
                deleteBtn.MouseLeave += (s, e) =>
                {
                    deleteBtn.ForeColor = Color.FromArgb(170, 25, 25);
                    CheckSlotHover(capturedIndex);
                };
            }

            lblClose.MouseEnter += (s, e) => lblClose.ForeColor = Color.FromArgb(190, 20, 20);
            lblClose.MouseLeave += (s, e) => lblClose.ForeColor = Color.FromArgb(40, 25, 12);
        }

        /// <summary>
        /// Rock-solid screen-bounds hover check that completely eliminates child-control redraw strobing.
        /// </summary>
        private void CheckSlotHover(int index)
        {
            if (index < 0 || index >= 3) return;

            Rectangle screenBounds = _slotPanels[index].RectangleToScreen(_slotPanels[index].ClientRectangle);
            bool isInside = screenBounds.Contains(Cursor.Position);

            if (isInside)
            {
                if (_currentHoveredIndex != index)
                {
                    int prev = _currentHoveredIndex;
                    _currentHoveredIndex = index;
                    if (prev != -1) SetSlotVisualHoverState(prev, false);
                    SetSlotVisualHoverState(index, true);
                }
            }
            else
            {
                if (_currentHoveredIndex == index)
                {
                    _currentHoveredIndex = -1;
                    SetSlotVisualHoverState(index, false);
                }
            }
        }

                private void SetSlotVisualHoverState(int index, bool isHovered)
        {
            if (index < 0 || index >= 3) return;

            var profile = _saveService.GetProfile(index + 1);

            if (isHovered)
            {
                AudioManager.Instance.PlayFrequencyBlip();
                _slotNums[index].ForeColor = Color.FromArgb(175, 45, 30);

                if (profile != null)
                {
                    _slotNames[index].ForeColor = Color.FromArgb(0, 0, 0);
                    _slotDetails[index].ForeColor = Color.FromArgb(32, 20, 10);
                    _slotTimes[index].ForeColor = Color.FromArgb(30, 18, 10);
                    _slotDeletes[index].Visible = true;
                }
                else
                {
                    _slotNames[index].ForeColor = Color.FromArgb(0, 0, 0);
                    _slotDetails[index].ForeColor = Color.FromArgb(175, 55, 40);
                }
            }
            else
            {
                _slotNums[index].ForeColor = Color.FromArgb(145, 65, 45);

                if (profile != null)
                {
                    _slotNames[index].ForeColor = Color.FromArgb(20, 12, 6);
                    _slotDetails[index].ForeColor = Color.FromArgb(60, 42, 26);
                    _slotTimes[index].ForeColor = Color.FromArgb(60, 42, 26);
                    _slotDeletes[index].Visible = false;
                }
                else
                {
                    _slotNames[index].ForeColor = Color.FromArgb(25, 16, 8);
                    _slotDetails[index].ForeColor = Color.FromArgb(140, 70, 55);
                    _slotDeletes[index].Visible = false;
                }
            }

            _slotPanels[index].Invalidate();
        }

        private void OperatorArchiveForm_Load(object? sender, EventArgs e)
        {
            AudioManager.Instance.PlayPaperRustle();
            CenterLedger();
            RefreshSlots();
        }

        private void OperatorArchiveForm_Resize(object? sender, EventArgs e)
        {
            CenterLedger();
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

            // 1. Subtle Analog CRT Vignette (shadowing the 4 corners of the dispatcher room)
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddRectangle(rect);
                using var pgb = new System.Drawing.Drawing2D.PathGradientBrush(path);
                pgb.CenterColor = Color.FromArgb(0, 0, 0, 0);
                pgb.SurroundColors = new[] { Color.FromArgb(175, 0, 0, 0) };
                pgb.FocusScales = new PointF(0.55f, 0.45f);
                g.FillRectangle(pgb, rect);
            }

            // 2. Subtle Analog CRT Scanlines
            using var scanlinePen = new Pen(Color.FromArgb(12, 0, 0, 0), 1f);
            for (int y = 0; y < rect.Height; y += 4)
            {
                g.DrawLine(scanlinePen, 0, y, rect.Width, y);
            }
        }

        private void CenterLedger()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            _scaler?.Apply(DesignScaler.FactorFor(ClientSize));
            pnlParchmentLedger.Left = (ClientSize.Width - pnlParchmentLedger.Width) / 2;
            pnlParchmentLedger.Top = (ClientSize.Height - pnlParchmentLedger.Height) / 2;
        }

        /// <summary>
        /// Reads all slots from ISaveService and dynamically updates UI elements (Polymorphism).
        /// </summary>
                public void RefreshSlots()
        {
            _mostRecentSlot = _saveService.GetMostRecentSlot();
            var slots = _saveService.GetAllSlots();

            for (int i = 0; i < 3; i++)
            {
                int slotIndex = i + 1;
                var profile = slots.TryGetValue(slotIndex, out var p) ? p : null;

                _slotNums[i].Text = slotIndex.ToString("D2");
                _slotNums[i].ForeColor = Color.FromArgb(145, 65, 45);

                if (profile != null)
                {
                    // Occupied Slot state (High contrast dark ink)
                    _slotNames[i].Text = profile.OperatorName;
                    _slotNames[i].ForeColor = Color.FromArgb(20, 12, 6);

                    _slotDetails[i].Text = profile.FormattedDetails;
                    _slotDetails[i].ForeColor = Color.FromArgb(60, 42, 26);

                    _slotTimes[i].Text = profile.FormattedTimestamp;
                    _slotTimes[i].ForeColor = Color.FromArgb(60, 42, 26);
                    _slotTimes[i].Visible = true;
                    _slotDeletes[i].Visible = (_currentHoveredIndex == i);
                }
                else
                {
                    // Empty Slot state (+ OPEN NEW SHIFT DOSSIER)
                    _slotNames[i].Text = "+ CREATE NEW SHIFT";
                    _slotNames[i].ForeColor = Color.FromArgb(25, 16, 8);

                    _slotDetails[i].Text = "EMPTY OPERATOR ARCHIVE";
                    _slotDetails[i].ForeColor = Color.FromArgb(140, 70, 55);

                    _slotTimes[i].Text = string.Empty;
                    _slotTimes[i].Visible = false;
                    _slotDeletes[i].Visible = false;
                }

                _slotPanels[i].Invalidate();
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            AudioManager.Instance.PlayInkStamp();
            var profile = _saveService.GetProfile(slotIndex);

            if (profile != null)
            {
                // Resume existing shift
                SelectedProfile = profile;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                // Create new shift in this empty slot
                PromptCreateNewShift(slotIndex);
            }
        }

        private void PromptCreateNewShift(int slotIndex)
        {
            using var dialog = new CreateShiftDialog(slotIndex);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                var newProfile = _saveService.CreateNewProfile(dialog.OperatorName, slotIndex);
                SelectedProfile = newProfile;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                RefreshSlots();
            }
        }

        private void OnDeleteSlotClicked(int slotIndex)
        {
            var profile = _saveService.GetProfile(slotIndex);
            if (profile == null) return;

            using var deleteDialog = new DeleteShiftDialog(profile);
            if (deleteDialog.ShowDialog(this) == DialogResult.OK)
            {
                _saveService.DeleteProfile(slotIndex);
                RefreshSlots();
            }
        }

        private void lblClose_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void OperatorArchiveForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

                private void SetHoveredIndex(int index)
        {
            if (_currentHoveredIndex >= 0 && _currentHoveredIndex < 3)
            {
                SetSlotVisualHoverState(_currentHoveredIndex, false);
            }
            _currentHoveredIndex = index;
            if (_currentHoveredIndex >= 0 && _currentHoveredIndex < 3)
            {
                SetSlotVisualHoverState(_currentHoveredIndex, true);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == Keys.D1 || keyData == Keys.NumPad1)
            {
                OnSlotClicked(1);
                return true;
            }
            if (keyData == Keys.D2 || keyData == Keys.NumPad2)
            {
                OnSlotClicked(2);
                return true;
            }
            if (keyData == Keys.D3 || keyData == Keys.NumPad3)
            {
                OnSlotClicked(3);
                return true;
            }
            if (keyData == Keys.Down)
            {
                int next = (_currentHoveredIndex + 1) % 3;
                SetHoveredIndex(next);
                return true;
            }
            if (keyData == Keys.Up)
            {
                int prev = (_currentHoveredIndex <= 0 ? 3 : _currentHoveredIndex) - 1;
                SetHoveredIndex(prev);
                return true;
            }
            if (keyData == Keys.Enter)
            {
                if (_currentHoveredIndex >= 0 && _currentHoveredIndex < 3)
                {
                    OnSlotClicked(_currentHoveredIndex + 1);
                    return true;
                }
                else
                {
                    int target = _mostRecentSlot ?? (_saveService.GetFirstAvailableSlot() ?? 1);
                    OnSlotClicked(target);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
