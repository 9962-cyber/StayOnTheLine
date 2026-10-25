using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// In-world replacement for Windows message boxes (e.g. the Night Desk Personnel Ledger):
    /// a parchment memo in the same style as the "Give the voice your name" dialog — ornate
    /// parchment frame, small header, large title, italic line, beige outlined buttons — shown over
    /// a dimmed screen. Optional cancel choice.
    /// </summary>
    public class ParchmentNoticeDialog : Form
    {
        private const int DesignW = 680;
        private const int EdgeMargin = 68;

        // Same ink as the name dialog
        private static readonly Color InkHeader = Color.FromArgb(30, 18, 10);
        private static readonly Color InkTitle = Color.FromArgb(16, 10, 4);
        private static readonly Color InkBody = Color.FromArgb(45, 28, 16);
        private static readonly Color InkClose = Color.FromArgb(40, 25, 12);

        private readonly Panel _pnlParchment;
        private readonly Label _lblClose;
        private readonly Label _btnConfirm;
        private readonly Label? _btnCancel;
        private readonly bool _isWarning;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth flicker-free compositing
                return cp;
            }
        }

        private ParchmentNoticeDialog(string header, string title, string body, string confirmText, string? cancelText, bool isWarning)
        {
            _isWarning = isWarning;

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(18, 18, 18);
            DoubleBuffered = true;
            KeyPreview = true;

            _pnlParchment = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(218, 208, 185),
                BackgroundImageLayout = ImageLayout.Stretch
            };
            LoadParchment();

            int textW = DesignW - EdgeMargin * 2;

            // Long headers shrink a little rather than running under the close button
            int headerMaxW = DesignW - EdgeMargin * 2 - 56;
            float headerSize = 10.5F;
            var headerFont = FontManager.GetSmallCapsFont(headerSize, FontStyle.Bold);
            while (headerSize > 7.5F && TextRenderer.MeasureText(header, headerFont, Size.Empty, TextFormatFlags.NoPrefix).Width > headerMaxW)
            {
                headerFont.Dispose();
                headerSize -= 0.5F;
                headerFont = FontManager.GetSmallCapsFont(headerSize, FontStyle.Bold);
            }
            var lblHeader = new Label
            {
                Text = header,
                AutoSize = true,
                UseMnemonic = false,
                BackColor = Color.Transparent,
                ForeColor = _isWarning ? Color.FromArgb(120, 32, 20) : InkHeader,
                Font = headerFont,
                Location = new Point(EdgeMargin, 52)
            };

            _lblClose = new Label
            {
                Text = "✕",
                BackColor = Color.Transparent,
                ForeColor = InkClose,
                Font = new Font("Segoe UI Symbol", 16F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(DesignW - EdgeMargin - 40, 42, 40, 38)
            };
            _lblClose.Click += (s, e) => Finish(DialogResult.Cancel);
            _lblClose.MouseEnter += (s, e) => _lblClose.ForeColor = Color.FromArgb(190, 20, 20);
            _lblClose.MouseLeave += (s, e) => _lblClose.ForeColor = InkClose;

            var lblTitle = new Label
            {
                Text = title,
                BackColor = Color.Transparent,
                ForeColor = InkTitle,
                Font = FontManager.GetSmallCapsFont(23F, FontStyle.Bold),
                Bounds = new Rectangle(EdgeMargin - 3, 85, textW, 44),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            // Body sits on the parchment like the name dialog's subtitle: italic, no card around it.
            // Measured narrower than the label and padded: the dialog is rescaled afterwards.
            var bodyFont = FontManager.GetSmallCapsFont(12F, FontStyle.Italic);
            int bodyH = TextRenderer.MeasureText(body, bodyFont, new Size(textW - 40, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + 16;
            var lblBody = new Label
            {
                Text = body,
                BackColor = Color.Transparent,
                ForeColor = InkBody,
                Font = bodyFont,
                UseMnemonic = false,
                Bounds = new Rectangle(EdgeMargin, 140, textW, bodyH)
            };

            int btnY = lblBody.Bottom + 30;
            _btnConfirm = MakeButton(confirmText, primary: true);
            int confirmW = ButtonWidth(_btnConfirm, 180);
            _btnConfirm.Bounds = new Rectangle(DesignW - EdgeMargin - confirmW, btnY, confirmW, 46);
            _btnConfirm.Click += (s, e) => Finish(DialogResult.OK);

            if (cancelText != null)
            {
                _btnCancel = MakeButton(cancelText, primary: false);
                int cancelW = ButtonWidth(_btnCancel, 155);
                _btnCancel.Bounds = new Rectangle(_btnConfirm.Left - 10 - cancelW, btnY, cancelW, 46);
                _btnCancel.Click += (s, e) => Finish(DialogResult.Cancel);
            }

            _pnlParchment.Controls.Add(lblHeader);
            _pnlParchment.Controls.Add(lblTitle);
            _pnlParchment.Controls.Add(lblBody);
            _pnlParchment.Controls.Add(_btnConfirm);
            if (_btnCancel != null) _pnlParchment.Controls.Add(_btnCancel);
            _pnlParchment.Controls.Add(_lblClose);
            _lblClose.BringToFront();
            Controls.Add(_pnlParchment);

            ClientSize = new Size(DesignW, btnY + 46 + 70);
            Text = title;

            Load += (s, e) =>
            {
                DesignScaler.FitDialog(this);
                (_btnCancel ?? _btnConfirm).Focus();
            };
        }

        /// <summary>Shows a single-button notice.</summary>
        public static void ShowNotice(IWin32Window? owner, string header, string title, string body, string confirmText = "UNDERSTOOD")
        {
            using var dlg = new ParchmentNoticeDialog(header, title, body, confirmText, null, isWarning: false);
            ShowOverDimmedScreen(owner, dlg);
        }

        /// <summary>Shows a confirm / cancel memo. Returns true when the operator confirms.</summary>
        public static bool Confirm(IWin32Window? owner, string header, string title, string body, string confirmText, string cancelText = "CANCEL")
        {
            using var dlg = new ParchmentNoticeDialog(header, title, body, confirmText, cancelText, isWarning: true);
            return ShowOverDimmedScreen(owner, dlg) == DialogResult.OK;
        }

        /// <summary>Darkens the owner window behind the memo while it is open.</summary>
        private static DialogResult ShowOverDimmedScreen(IWin32Window? owner, ParchmentNoticeDialog dlg)
        {
            if (owner is not Form ownerForm || !ownerForm.Visible)
                return dlg.ShowDialog(owner);

            using var shade = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Bounds = ownerForm.Bounds,
                BackColor = Color.Black,
                Opacity = 0.62
            };
            shade.Show(ownerForm);
            shade.Update();
            try
            {
                return dlg.ShowDialog(shade);
            }
            finally
            {
                shade.Close();
            }
        }

        /// <summary>Wide enough for the label on one line (it would otherwise wrap and be cut off).</summary>
        private static int ButtonWidth(Label button, int minimum) =>
            Math.Max(minimum, TextRenderer.MeasureText(button.Text, button.Font, Size.Empty, TextFormatFlags.NoPrefix).Width + 44);

        private Label MakeButton(string text, bool primary)
        {
            // Same buttons as the name dialog: beige outlined, the main action dark ink
            Color bg = primary
                ? (_isWarning ? Color.FromArgb(120, 32, 20) : Color.FromArgb(32, 22, 12))
                : Color.FromArgb(232, 222, 200);
            Color bgHover = primary
                ? (_isWarning ? Color.FromArgb(160, 38, 24) : Color.FromArgb(52, 36, 20))
                : Color.FromArgb(248, 240, 222);
            Color fg = primary ? Color.FromArgb(250, 244, 230) : Color.FromArgb(35, 22, 12);
            Color fgHover = primary ? Color.FromArgb(255, 252, 245) : Color.FromArgb(10, 5, 2);

            var btn = new Label
            {
                Text = text,
                BackColor = bg,
                ForeColor = fg,
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontManager.GetSmallCapsFont(primary ? 12F : 11.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false,
                Cursor = Cursors.Hand
            };
            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = bgHover;
                btn.ForeColor = fgHover;
                AudioManager.Instance.PlayFrequencyBlip();
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = bg;
                btn.ForeColor = fg;
            };
            return btn;
        }

        private void LoadParchment()
        {
            try
            {
                foreach (var name in new[] { "parchment_modal_bg.png", "creategame_bg.png" })
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", name);
                    if (!File.Exists(path))
                        path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", name);
                    if (File.Exists(path))
                    {
                        _pnlParchment.BackgroundImage = Image.FromFile(path);
                        return;
                    }
                }
            }
            catch
            {
                // Keep the flat parchment colour
            }
        }

        private void Finish(DialogResult result)
        {
            AudioManager.Instance.PlayPaperRustle();
            DialogResult = result;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Finish(_btnCancel != null ? DialogResult.Cancel : DialogResult.OK);
                return true;
            }
            if (keyData == Keys.Enter)
            {
                // Enter never confirms a destructive memo by accident
                Finish(_btnCancel != null ? DialogResult.Cancel : DialogResult.OK);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
