using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// In-world replacement for Windows message boxes: a parchment memo that matches
    /// the operator archive dialogs, with an optional cancel choice.
    /// </summary>
    public class ParchmentNoticeDialog : Form
    {
        private const int DesignW = 680;
        private const int EdgeMargin = 68;

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

            var lblHeader = new Label
            {
                Text = header,
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(120, 32, 20),
                Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold),
                Location = new Point(EdgeMargin, 52)
            };

            _lblClose = new Label
            {
                Text = "✕",
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(40, 25, 12),
                Font = new Font("Segoe UI Symbol", 16F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(DesignW - EdgeMargin - 40, 42, 40, 38)
            };
            _lblClose.Click += (s, e) => Finish(DialogResult.Cancel);
            _lblClose.MouseEnter += (s, e) => _lblClose.ForeColor = Color.FromArgb(190, 20, 20);
            _lblClose.MouseLeave += (s, e) => _lblClose.ForeColor = Color.FromArgb(40, 25, 12);

            var lblTitle = new Label
            {
                Text = title,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(16, 10, 4),
                Font = FontManager.GetSmallCapsFont(22F, FontStyle.Bold),
                Bounds = new Rectangle(EdgeMargin - 3, 82, textW, 44),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            var bodyFont = FontManager.GetItalicFont(12.5F);
            // Measure narrower than the label and pad generously: the dialog is rescaled afterwards,
            // and rounded font sizes can push the last word onto a new line
            int bodyH = TextRenderer.MeasureText(body, bodyFont, new Size(textW - 60, int.MaxValue), TextFormatFlags.WordBreak).Height + 44;
            var pnlCard = new Panel
            {
                BackColor = Color.FromArgb(236, 226, 204),
                Bounds = new Rectangle(EdgeMargin, 136, textW, bodyH)
            };
            var lblBody = new Label
            {
                Text = body,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(34, 24, 14),
                Font = bodyFont,
                Bounds = new Rectangle(18, 14, textW - 28, bodyH - 20)
            };
            pnlCard.Controls.Add(lblBody);
            pnlCard.Paint += (s, e) =>
            {
                var rect = pnlCard.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using var pen = new Pen(Color.FromArgb(155, 130, 98), 1);
                e.Graphics.DrawRectangle(pen, rect);
                using var spine = new SolidBrush(_isWarning ? Color.FromArgb(120, 32, 20) : Color.FromArgb(60, 90, 50));
                e.Graphics.FillRectangle(spine, 0, 0, 5, pnlCard.Height);
            };

            int btnY = pnlCard.Bottom + 28;
            _btnConfirm = MakeButton(confirmText, primary: true);
            _btnConfirm.Bounds = new Rectangle(DesignW - EdgeMargin - 200, btnY, 200, 46);
            _btnConfirm.Click += (s, e) => Finish(DialogResult.OK);

            if (cancelText != null)
            {
                _btnCancel = MakeButton(cancelText, primary: false);
                _btnCancel.Bounds = new Rectangle(_btnConfirm.Left - 12 - 170, btnY, 170, 46);
                _btnCancel.Click += (s, e) => Finish(DialogResult.Cancel);
            }

            _pnlParchment.Controls.Add(lblHeader);
            _pnlParchment.Controls.Add(lblTitle);
            _pnlParchment.Controls.Add(pnlCard);
            _pnlParchment.Controls.Add(_btnConfirm);
            if (_btnCancel != null) _pnlParchment.Controls.Add(_btnCancel);
            _pnlParchment.Controls.Add(_lblClose);
            _lblClose.BringToFront();
            Controls.Add(_pnlParchment);

            ClientSize = new Size(DesignW, btnY + 46 + 64);
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
            dlg.ShowDialog(owner);
        }

        /// <summary>Shows a confirm / cancel memo. Returns true when the operator confirms.</summary>
        public static bool Confirm(IWin32Window? owner, string header, string title, string body, string confirmText, string cancelText = "CANCEL")
        {
            using var dlg = new ParchmentNoticeDialog(header, title, body, confirmText, cancelText, isWarning: true);
            return dlg.ShowDialog(owner) == DialogResult.OK;
        }

        private Label MakeButton(string text, bool primary)
        {
            Color bg = primary
                ? (_isWarning ? Color.FromArgb(120, 32, 20) : Color.FromArgb(46, 34, 22))
                : Color.FromArgb(232, 222, 200);
            Color bgHover = primary
                ? (_isWarning ? Color.FromArgb(160, 38, 24) : Color.FromArgb(70, 52, 32))
                : Color.FromArgb(248, 240, 222);
            Color fg = primary ? Color.FromArgb(250, 244, 230) : Color.FromArgb(35, 22, 12);
            Color fgHover = primary ? Color.White : Color.FromArgb(10, 5, 2);

            var btn = new Label
            {
                Text = text,
                BackColor = bg,
                ForeColor = fg,
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontManager.GetSmallCapsFont(primary ? 12F : 11.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
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
