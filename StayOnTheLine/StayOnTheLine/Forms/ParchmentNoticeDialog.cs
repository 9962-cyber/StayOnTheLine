using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// In-world replacement for Windows message boxes (e.g. the Night Desk Personnel Ledger):
    /// a black notice with a double light rule, like the dialogue boxes, with an optional cancel choice.
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
                BackColor = Color.FromArgb(10, 10, 10)
            };
            _pnlParchment.Paint += (s, e) =>
            {
                // Double light rule, same as the in-room dialogue boxes
                int W = _pnlParchment.Width, H = _pnlParchment.Height;
                using var outer = new Pen(Color.FromArgb(225, 255, 255, 255), 1.5f);
                using var inner = new Pen(Color.FromArgb(110, 255, 255, 255), 1f);
                e.Graphics.DrawRectangle(outer, 3, 3, W - 7, H - 7);
                e.Graphics.DrawRectangle(inner, 9, 9, W - 19, H - 19);
            };

            int textW = DesignW - EdgeMargin * 2;

            var lblHeader = new Label
            {
                Text = header,
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(214, 88, 70),
                Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold),
                Location = new Point(EdgeMargin, 52)
            };

            _lblClose = new Label
            {
                Text = "✕",
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(190, 190, 186),
                Font = new Font("Segoe UI Symbol", 16F, FontStyle.Bold, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Bounds = new Rectangle(DesignW - EdgeMargin - 40, 42, 40, 38)
            };
            _lblClose.Click += (s, e) => Finish(DialogResult.Cancel);
            _lblClose.MouseEnter += (s, e) => _lblClose.ForeColor = Color.FromArgb(230, 80, 64);
            _lblClose.MouseLeave += (s, e) => _lblClose.ForeColor = Color.FromArgb(190, 190, 186);

            var lblTitle = new Label
            {
                Text = title,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(242, 238, 230),
                Font = FontManager.GetSmallCapsFont(22F, FontStyle.Bold),
                Bounds = new Rectangle(EdgeMargin - 3, 82, textW, 44),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            var bodyFont = FontManager.GetSmallCapsFont(12.5F);
            // Measure narrower than the label and pad generously: the dialog is rescaled afterwards,
            // and rounded font sizes can push the last word onto a new line
            int bodyH = TextRenderer.MeasureText(body, bodyFont, new Size(textW - 60, int.MaxValue), TextFormatFlags.WordBreak).Height + 44;
            var pnlCard = new Panel
            {
                BackColor = Color.FromArgb(22, 22, 22),
                Bounds = new Rectangle(EdgeMargin, 136, textW, bodyH)
            };
            var lblBody = new Label
            {
                Text = body,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(222, 216, 206),
                Font = bodyFont,
                Bounds = new Rectangle(18, 14, textW - 28, bodyH - 20)
            };
            pnlCard.Controls.Add(lblBody);
            pnlCard.Paint += (s, e) =>
            {
                var rect = pnlCard.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using var pen = new Pen(Color.FromArgb(70, 70, 70), 1);
                e.Graphics.DrawRectangle(pen, rect);
                using var spine = new SolidBrush(_isWarning ? Color.FromArgb(170, 44, 30) : Color.FromArgb(200, 200, 196));
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
                ? (_isWarning ? Color.FromArgb(120, 32, 20) : Color.FromArgb(236, 232, 224))
                : Color.FromArgb(18, 18, 18);
            Color bgHover = primary
                ? (_isWarning ? Color.FromArgb(160, 38, 24) : Color.White)
                : Color.FromArgb(52, 52, 52);
            Color fg = primary ? (_isWarning ? Color.FromArgb(250, 244, 230) : Color.FromArgb(14, 14, 14)) : Color.FromArgb(220, 216, 208);
            Color fgHover = primary ? (_isWarning ? Color.White : Color.Black) : Color.White;

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
