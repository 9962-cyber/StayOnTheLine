using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Controls
{
    /// <summary>
    /// One line of a phone-call scene. An empty speaker is narration / system text.
    /// <paramref name="Background"/> switches the place shown (null keeps the current one);
    /// <paramref name="ClearPortrait"/> takes the character off screen (e.g. terminal output nobody is saying).
    /// </summary>
    public sealed record CallSceneLine(string Speaker, string Text, Image? Portrait, string Tag, Image? Background = null, bool ClearPortrait = false);

    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// A live call shown as a story scene: the caller's room behind, the caller on the phone, the
    /// usual double-ruled dialogue box, and the call's choices as buttons under it. It knows nothing
    /// about calls or scripts — the shift screen feeds it lines and choice captions and is told which
    /// choice was clicked, so the call logic stays where it is.
    /// </summary>
    public sealed class PhoneCallScene : Control
    {
        private readonly Queue<CallSceneLine> _queue = new();
        private readonly List<RoomChoiceButton> _buttons = new();
        private readonly System.Windows.Forms.Timer _typeTimer = new() { Interval = 18 };
        private CallSceneLine? _current;
        private Image? _portrait;
        private IReadOnlyList<(string Caption, bool Recommended)> _pendingChoices = Array.Empty<(string, bool)>();
        private bool _endAfterLines;
        private int _typed;

        /// <summary>The player picked choice <c>index</c> (same order as given to <see cref="SetChoices"/>).</summary>
        public event EventHandler<int>? ChoiceSelected;

        /// <summary>The call's last lines have been read and the scene closed itself.</summary>
        public event EventHandler? Ended;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Image? SceneBackground { get; set; }

        /// <summary>True from the first line until the scene closes.</summary>
        public bool Active { get; private set; }

        public PhoneCallScene()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.Black;
            Cursor = Cursors.Hand;
            Visible = false;

            _typeTimer.Tick += (s, e) =>
            {
                if (_current == null) { _typeTimer.Stop(); return; }
                _typed = Math.Min(_current.Text.Length, _typed + 2);
                if (_typed % 6 == 0) AudioManager.Instance.PlayTypewriterKeystroke();
                if (_typed >= _current.Text.Length)
                {
                    _typeTimer.Stop();
                    if (_queue.Count == 0 && !_endAfterLines) ShowChoices(); // the question is on screen: offer the answers
                }
                Invalidate(BoxRect());
            };
        }

        /// <summary>Queues lines; the scene opens if it was closed.</summary>
        public void PlayLines(IEnumerable<CallSceneLine> lines)
        {
            foreach (var line in lines) _queue.Enqueue(line);
            ClearButtons();
            if (!Active)
            {
                Active = true;
                Visible = true;
                BringToFront();
            }
            if (_current == null || _typed >= _current.Text.Length) ShowNext();
        }

        /// <summary>Choices shown once every queued line has been read.</summary>
        public void SetChoices(IReadOnlyList<(string Caption, bool Recommended)> choices)
        {
            _endAfterLines = false;
            _pendingChoices = choices;
            if (_queue.Count == 0 && _current != null && _typed >= _current.Text.Length) ShowChoices();
        }

        /// <summary>The call is over: close the scene after the remaining lines.</summary>
        public void EndAfterLines()
        {
            _endAfterLines = true;
            _pendingChoices = Array.Empty<(string, bool)>();
            ClearButtons();
            if (_queue.Count == 0 && (_current == null || _typed >= _current.Text.Length)) Close();
        }

        /// <summary>Closes immediately (e.g. the player switched to another line).</summary>
        public void Close()
        {
            _queue.Clear();
            _current = null;
            _portrait = null;
            _typeTimer.Stop();
            ClearButtons();
            bool wasActive = Active;
            Active = false;
            _endAfterLines = false;
            Visible = false;
            if (wasActive) Ended?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Space / Enter / click: finish the typing line, or move to the next one.</summary>
        public void Advance()
        {
            if (_current == null) return;
            if (_typed < _current.Text.Length)
            {
                _typed = _current.Text.Length;
                _typeTimer.Stop();
                Invalidate();
                if (_queue.Count == 0) AfterLastLine();
                return;
            }
            if (_queue.Count > 0) ShowNext();
            else AfterLastLine();
        }

        /// <summary>Number keys pick the visible choices.</summary>
        public bool TrySelect(int index)
        {
            if (index < 0 || index >= _buttons.Count) return false;
            Pick(index);
            return true;
        }

        private void ShowNext()
        {
            if (_queue.Count == 0) { AfterLastLine(); return; }
            _current = _queue.Dequeue();
            if (_current.ClearPortrait) _portrait = null;
            else if (_current.Portrait != null) _portrait = _current.Portrait;
            if (_current.Background != null) SceneBackground = _current.Background;
            _typed = 0;
            _typeTimer.Start();
            Invalidate();
        }

        private void AfterLastLine()
        {
            if (_endAfterLines) Close();
            else ShowChoices();
        }

        private void ShowChoices()
        {
            if (_buttons.Count > 0 || _pendingChoices.Count == 0) return;
            for (int i = 0; i < _pendingChoices.Count; i++)
            {
                int index = i;
                var (caption, recommended) = _pendingChoices[i];
                var button = new RoomChoiceButton { Text = recommended ? "► " + caption : caption };
                button.Click += (s, e) => Pick(index);
                _buttons.Add(button);
                Controls.Add(button);
            }
            LayoutButtons();
            Invalidate();
        }

        private void Pick(int index)
        {
            ClearButtons();
            _pendingChoices = Array.Empty<(string, bool)>();
            ChoiceSelected?.Invoke(this, index);
        }

        private void ClearButtons()
        {
            foreach (var b in _buttons) { Controls.Remove(b); b.Dispose(); }
            _buttons.Clear();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (_buttons.Count == 0) Advance();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutButtons();
        }

        // Layout (design units at 1080p): box near the bottom, choices in rows of three beneath it
        private float S => Height / 1080f;

        private Rectangle BoxRect()
        {
            int marginX = (int)(Width * 0.055f);
            int boxH = (int)(Height * 0.17f);
            int rows = _buttons.Count == 0 ? 0 : (_buttons.Count + 2) / 3;
            int below = rows == 0 ? (int)(Height * 0.055f) : rows * (int)(96 * S) + (int)(36 * S);
            return new Rectangle(marginX, Height - boxH - below, Width - marginX * 2, boxH);
        }

        private void LayoutButtons()
        {
            if (_buttons.Count == 0 || Width <= 0) return;
            // Three evenly spaced buttons per row under the box, each centred in its third
            var box = BoxRect();
            int btnW = (int)(box.Width * 0.21f);
            int btnH = (int)(84 * S);
            for (int i = 0; i < _buttons.Count; i++)
            {
                int row = i / 3, col = i % 3;
                int rowCount = Math.Min(3, _buttons.Count - row * 3);
                float slot = box.Width / (float)rowCount;
                int cx = box.X + (int)(slot * (col + 0.5f));
                _buttons[i].Bounds = new Rectangle(cx - btnW / 2, box.Bottom + (int)(26 * S) + row * (btnH + (int)(12 * S)), btnW, btnH);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            int W = Width, H = Height;
            if (W < 10 || H < 10) return;
            float s = S;

            // 1. The caller's room, cover-fit, darkened at the edges
            if (SceneBackground != null)
            {
                float k = Math.Max((float)W / SceneBackground.Width, (float)H / SceneBackground.Height);
                float bw = SceneBackground.Width * k, bh = SceneBackground.Height * k;
                g.DrawImage(SceneBackground, (W - bw) / 2f, (H - bh) / 2f, bw, bh);
            }
            using (var path = new GraphicsPath())
            {
                path.AddRectangle(new Rectangle(0, 0, W, H));
                using var vignette = new PathGradientBrush(path)
                {
                    CenterColor = Color.FromArgb(0, 0, 0, 0),
                    SurroundColors = new[] { Color.FromArgb(150, 0, 0, 0) },
                    FocusScales = new PointF(0.7f, 0.65f)
                };
                g.FillRectangle(vignette, 0, 0, W, H);
            }

            // 2. The caller on the phone, standing on the right (the dialogue box overlaps her)
            if (_portrait != null)
            {
                int ph = (int)(H * 1.02f);
                int pw = (int)(ph * (float)_portrait.Width / _portrait.Height);
                g.DrawImage(_portrait, (int)(W * 0.80f) - pw / 2, H - ph + (int)(H * 0.04f), pw, ph);
            }

            // 3. Dialogue box: near-black with the double white rule
            var rc = BoxRect();
            using (var bg = new SolidBrush(Color.FromArgb(225, 8, 8, 8))) g.FillRectangle(bg, rc);
            using (var outer = new Pen(Color.FromArgb(225, 255, 255, 255), 1.5f)) g.DrawRectangle(outer, rc.X + 2, rc.Y + 2, rc.Width - 5, rc.Height - 5);
            using (var inner = new Pen(Color.FromArgb(120, 255, 255, 255), 1f)) g.DrawRectangle(inner, rc.X + 7, rc.Y + 7, rc.Width - 15, rc.Height - 15);
            if (_current == null) return;

            int pad = (int)(36 * s);
            int textLeft = rc.X + pad;
            int textW = rc.Width - pad * 2 - (int)(30 * s);
            bool narration = _current.Speaker.Length == 0;

            if (!narration)
            {
                using var fontName = FontManager.GetSmallCapsFont(15f * s, FontStyle.Bold);
                TextRenderer.DrawText(g, _current.Speaker, fontName, new Point(textLeft, rc.Y + (int)(20 * s)), Color.White, TextFormatFlags.NoPrefix);
            }
            if (_current.Tag.Length > 0)
            {
                using var fontTag = FontManager.GetSmallCapsFont(11f * s);
                TextRenderer.DrawText(g, _current.Tag, fontTag, new Rectangle(rc.X, rc.Y + (int)(22 * s), rc.Width - pad, (int)(30 * s)),
                    Color.Gray, TextFormatFlags.Right | TextFormatFlags.NoPrefix);
            }

            float bodySize = _current.Text.Length > 120 ? 17f : 20f;
            using var fontBody = narration ? FontManager.GetItalicFont(bodySize * s) : FontManager.GetDialogueFont(bodySize * s);
            int bodyTop = rc.Y + (narration ? (int)(30 * s) : (int)(62 * s));
            string shown = _current.Text.Substring(0, Math.Min(_typed, _current.Text.Length));
            TextRenderer.DrawText(g, shown, fontBody, new Rectangle(textLeft, bodyTop, textW, rc.Bottom - bodyTop - (int)(14 * s)),
                narration ? Color.FromArgb(200, 200, 196) : Color.White, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

            if (_typed >= _current.Text.Length && (_queue.Count > 0 || _buttons.Count == 0))
            {
                using var fontArrow = FontManager.GetSmallCapsFont(13f * s);
                TextRenderer.DrawText(g, "▼", fontArrow, new Point(rc.Right - (int)(54 * s), rc.Bottom - (int)(48 * s)), Color.White, TextFormatFlags.NoPrefix);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _typeTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
