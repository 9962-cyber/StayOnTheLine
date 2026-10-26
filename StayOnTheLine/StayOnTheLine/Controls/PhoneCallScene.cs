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

        private Image? _sceneBackground;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Image? SceneBackground
        {
            get => _sceneBackground;
            set
            {
                if (!ReferenceEquals(_sceneBackground, value))
                {
                    _sceneBackground = value;
                    _backdropDirty = true;
                }
            }
        }

        /// <summary>True from the first line until the scene closes.</summary>
        public bool Active { get; private set; }

        public PhoneCallScene()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.StandardDoubleClick, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.Black;
            Cursor = Cursors.Hand;
            Visible = false;

            _typeTimer.Tick += (s, e) =>
            {
                if (_current == null) { _typeTimer.Stop(); return; }
                if (SettingsManager.Instance.Current.GetDelayPerCharacterMs() == 0)
                {
                    _typed = _current.Text.Length;
                    _typeTimer.Stop();
                }
                else
                {
                    _typed = Math.Min(_current.Text.Length, _typed + 1);
                    if (_typed % 2 == 0)
                    {
                        AudioManager.Instance.PlayTypewriterKeystroke();
                    }
                }
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
                AudioManager.Instance.PlayPhonePickup();
            }
            if (_current == null || _typed >= _current.Text.Length) ShowNext();
        }

        public bool IsTyping => _current != null && _typed < _current.Text.Length;
        public bool HasQueuedLines => _queue.Count > 0;
        public event EventHandler? LineAdvanceRequested;

        /// <summary>Completes typing immediately for the current line.</summary>
        public void CompleteTyping()
        {
            if (_current == null) return;
            if (_typed < _current.Text.Length)
            {
                _typed = _current.Text.Length;
                _typeTimer.Stop();
                if (_queue.Count == 0 && !_endAfterLines) ShowChoices();
                Invalidate();
            }
        }

        /// <summary>Immediately completes all lines and displays pending choices or closes the call scene.</summary>
        public void CompleteAll()
        {
            _queue.Clear();
            _typeTimer.Stop();
            if (_current != null)
            {
                _typed = _current.Text.Length;
            }
            if (_endAfterLines)
            {
                Close();
                return;
            }
            if (_pendingChoices.Count > 0)
            {
                ShowChoices();
                Invalidate();
                return;
            }
            Close();
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
            if (_queue.Count == 0 && _current == null) Close();
        }

        /// <summary>Closes immediately (e.g. the player switched to another line).</summary>
        public void Close()
        {
            _queue.Clear();
            _current = null;
            _portrait = null;
            _backdropDirty = true;
            _typeTimer.Stop();
            ClearButtons();
            bool wasActive = Active;
            Active = false;
            _endAfterLines = false;
            Visible = false;
            if (wasActive)
            {
                AudioManager.Instance.PlayPhoneHangup();
                Ended?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Space / Enter / click: finish the typing line, or move to the next one.</summary>
        public void Advance()
        {
            if (_current == null) return;
            if (_typed < _current.Text.Length)
            {
                _typed = _current.Text.Length;
                _typeTimer.Stop();
                if (_queue.Count == 0 && !_endAfterLines) ShowChoices();
                Invalidate();
                return;
            }
            if (_queue.Count > 0)
            {
                ShowNext();
                return;
            }
            if (_endAfterLines)
            {
                Close();
                return;
            }
            if (_pendingChoices.Count > 0)
            {
                ShowChoices();
                return;
            }
            LineAdvanceRequested?.Invoke(this, EventArgs.Empty);
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
            if (_current.ClearPortrait) { _portrait = null; _backdropDirty = true; }
            else if (_current.Portrait != null && !ReferenceEquals(_portrait, _current.Portrait)) { _portrait = _current.Portrait; _backdropDirty = true; }
            int delay = SettingsManager.Instance.Current.GetDelayPerCharacterMs();
            if (delay == 0)
            {
                _typed = _current.Text.Length;
                _typeTimer.Stop();
                if (_queue.Count == 0 && !_endAfterLines) ShowChoices();
            }
            else
            {
                _typed = 0;
                _typeTimer.Interval = Math.Max(1, delay);
                _typeTimer.Start();
            }
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

        private long _lastClickTicks;

        protected override void OnClick(EventArgs e)
        {
            if (_buttons.Count == 0)
            {
                long now = Environment.TickCount64;
                if (now - _lastClickTicks <= SystemInformation.DoubleClickTime)
                {
                    CompleteAll();
                }
                else
                {
                    Advance();
                }
                _lastClickTicks = now;
            }
            base.OnClick(e);
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            if (_buttons.Count == 0)
            {
                CompleteAll();
            }
            base.OnDoubleClick(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _backdropDirty = true;
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

        private Bitmap? _cachedBackdrop;
        private bool _backdropDirty = true;

        private void UpdateBackdrop(int W, int H)
        {
            if (_cachedBackdrop != null && (_cachedBackdrop.Width != W || _cachedBackdrop.Height != H))
            {
                _cachedBackdrop.Dispose();
                _cachedBackdrop = null;
            }

            _cachedBackdrop ??= new Bitmap(W, H, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            using var g = Graphics.FromImage(_cachedBackdrop);
            g.Clear(Color.Black);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

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

            _backdropDirty = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            int W = Width, H = Height;
            if (W < 10 || H < 10) return;
            float s = S;

            if (_backdropDirty || _cachedBackdrop == null || _cachedBackdrop.Width != W || _cachedBackdrop.Height != H)
            {
                UpdateBackdrop(W, H);
            }

            if (_cachedBackdrop != null)
            {
                g.DrawImageUnscaled(_cachedBackdrop, 0, 0);
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
            if (disposing)
            {
                _typeTimer.Dispose();
                _cachedBackdrop?.Dispose();
                _cachedBackdrop = null;
            }
            base.Dispose(disposing);
        }
    }
}
