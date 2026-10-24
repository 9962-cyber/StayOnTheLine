using System.Drawing.Drawing2D;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Controls
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Full-screen in-room dialogue scene used during shifts: the dispatch room behind,
    /// the speaking character standing in it, and the same dark, double-ruled dialogue box as
    /// the story scenes. Lines are queued, typed out, and advanced by clicking
    /// (or Space / Enter via <see cref="Advance"/>).
    /// </summary>
    public class CharacterDialogueBox : Control
    {
        private sealed record DialogueLine(string Speaker, string Text, Image? Portrait, string Tag);

        private readonly Queue<DialogueLine> _queue = new();
        private readonly System.Windows.Forms.Timer _typeTimer = new() { Interval = 18 };
        private DialogueLine? _current;
        private Image? _standingSprite; // stays on screen through narration lines
        private int _typedChars;

        public event EventHandler? Finished;

        /// <summary>Room art drawn behind the character (cover-fit).</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Image? SceneBackground { get; set; }

        public bool IsActive => _current != null;

        /// <summary>
        /// True: a compact strip under the computer (framed portrait + box) while the CRT stays visible.
        /// False: the full-screen room scene.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Docked { get; set; }

        // Head-and-shoulders crop of the 1536x2048 character sprites (docked portrait)
        private static readonly RectangleF PortraitCrop = new(190, 110, 1160, 1160);

        public CharacterDialogueBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Black;
            Cursor = Cursors.Hand;
            Visible = false;

            _typeTimer.Tick += (s, e) =>
            {
                if (_current == null) { _typeTimer.Stop(); return; }
                _typedChars = Math.Min(_current.Text.Length, _typedChars + 2);
                if (_typedChars % 6 == 0) AudioManager.Instance.PlayTypewriterKeystroke();
                if (_typedChars >= _current.Text.Length) _typeTimer.Stop();
                Invalidate();
            };
        }

        /// <summary>Queues a line; the scene appears if it was hidden.</summary>
        /// <param name="tag">Label shown opposite the speaker's name (Charm's console by default).</param>
        public void Enqueue(string speaker, string text, Image? portrait, string tag = "NEIGHBORING CONSOLE")
        {
            _queue.Enqueue(new DialogueLine(speaker, text, portrait, tag));
            if (_current == null) ShowNext();
        }

        /// <summary>Completes the typing line, or moves on to the next queued line.</summary>
        public void Advance()
        {
            if (_current == null) return;
            if (_typedChars < _current.Text.Length)
            {
                _typedChars = _current.Text.Length;
                _typeTimer.Stop();
                Invalidate();
                return;
            }
            ShowNext();
        }

        /// <summary>Drops any queued lines and hides the scene.</summary>
        public void Clear()
        {
            _queue.Clear();
            _current = null;
            _standingSprite = null;
            _typeTimer.Stop();
            Visible = false;
        }

        private void ShowNext()
        {
            if (_queue.Count == 0)
            {
                _current = null;
                _standingSprite = null;
                _typeTimer.Stop();
                Visible = false;
                Finished?.Invoke(this, EventArgs.Empty);
                return;
            }

            _current = _queue.Dequeue();
            if (_current.Portrait != null) _standingSprite = _current.Portrait;
            _typedChars = 0;
            Visible = true;
            BringToFront();
            _typeTimer.Start();
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Advance();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _typeTimer.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>Compact strip: framed portrait on the left, name and typed line beside it.</summary>
        private void PaintDocked(Graphics g, int W, int H)
        {
            if (_current == null) return;
            float s = H / 210f;
            var rc = new Rectangle(0, 0, W, H);

            using (var bg = new SolidBrush(Color.FromArgb(242, 8, 8, 8)))
                g.FillRectangle(bg, rc);
            using (var outer = new Pen(Color.FromArgb(225, 255, 255, 255), 1.5f))
                g.DrawRectangle(outer, 2, 2, W - 5, H - 5);
            using (var inner = new Pen(Color.FromArgb(120, 255, 255, 255), 1f))
                g.DrawRectangle(inner, 7, 7, W - 15, H - 15);

            int pad = (int)(22 * s);
            int textLeft = pad;

            if (_standingSprite != null)
            {
                int ph = H - pad * 2;
                var frame = new Rectangle(pad, pad, ph, ph);
                using (var frameBg = new SolidBrush(Color.FromArgb(28, 34, 28)))
                    g.FillRectangle(frameBg, frame);

                var crop = PortraitCrop;
                if (_standingSprite.Width < crop.Right || _standingSprite.Height < crop.Bottom)
                    crop = new RectangleF(0, 0, _standingSprite.Width, _standingSprite.Width);
                g.DrawImage(_standingSprite, frame, crop, GraphicsUnit.Pixel);

                using (var framePen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
                    g.DrawRectangle(framePen, frame);
                textLeft = frame.Right + pad;
            }

            int textW = W - textLeft - pad - (int)(24 * s);

            if (_current.Speaker.Length > 0)
            {
                using var fontName = FontManager.GetSmallCapsFont(15f * s, FontStyle.Bold);
                TextRenderer.DrawText(g, _current.Speaker, fontName, new Point(textLeft, pad - (int)(2 * s)), Color.White, TextFormatFlags.NoPrefix);

                using var fontTag = FontManager.GetSmallCapsFont(10f * s);
                TextRenderer.DrawText(g, _current.Tag, fontTag,
                    new Rectangle(textLeft, pad, W - textLeft - pad, (int)(24 * s)), Color.Gray,
                    TextFormatFlags.Right | TextFormatFlags.NoPrefix);
            }

            float bodySize = _current.Text.Length > 140 ? 15f : 17.5f;
            using var fontBody = FontManager.GetItalicFont(bodySize * s);
            int bodyTop = _current.Speaker.Length > 0 ? pad + (int)(40 * s) : pad + (int)(8 * s);
            string shown = _current.Text.Substring(0, Math.Min(_typedChars, _current.Text.Length));
            TextRenderer.DrawText(g, shown, fontBody,
                new Rectangle(textLeft, bodyTop, textW, H - bodyTop - pad),
                Color.White, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

            if (_typedChars >= _current.Text.Length)
            {
                using var fontArrow = FontManager.GetSmallCapsFont(13f * s);
                TextRenderer.DrawText(g, "▼", fontArrow,
                    new Point(W - pad - (int)(18 * s), H - pad - (int)(20 * s)), Color.White, TextFormatFlags.NoPrefix);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int W = ClientSize.Width, H = ClientSize.Height;
            if (W <= 2 || H <= 2 || _current == null) return;
            if (Docked)
            {
                PaintDocked(g, W, H);
                return;
            }
            float s = H / 1080f;

            // 1. The dispatch room, cover-fit, with the story scenes' soft edge vignette
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
                    SurroundColors = new[] { Color.FromArgb(110, 0, 0, 0) },
                    FocusScales = new PointF(0.75f, 0.70f)
                };
                g.FillRectangle(vignette, 0, 0, W, H);
            }

            // 2. Charmee standing at her console (right side, feet off the bottom like the story scenes)
            if (_standingSprite != null)
            {
                int cH = (int)(H * 0.92f);
                int cW = (int)(H * 0.70f);
                var box = new Rectangle(W - cW - (int)(W * 0.06f), H - cH, cW, cH);
                float aspect = (float)_standingSprite.Width / _standingSprite.Height;
                int dw = box.Width, dh = box.Height, dx = box.X, dy = box.Y;
                if (aspect > (float)box.Width / box.Height) { dh = (int)(box.Width / aspect); dy = box.Bottom - dh; }
                else { dw = (int)(box.Height * aspect); dx = box.X + (box.Width - dw) / 2; }
                g.DrawImage(_standingSprite, dx, dy, dw, dh);
            }

            // 3. Dialogue box: near-black with the double white rule
            int marginX = (int)(W * 0.055f);
            int boxH = (int)(H * 0.20f);
            var rc = new Rectangle(marginX, H - boxH - (int)(H * 0.055f), W - marginX * 2, boxH);
            using (var bg = new SolidBrush(Color.FromArgb(225, 8, 8, 8)))
                g.FillRectangle(bg, rc);
            using (var outer = new Pen(Color.FromArgb(225, 255, 255, 255), 1.5f))
                g.DrawRectangle(outer, rc.X + 2, rc.Y + 2, rc.Width - 5, rc.Height - 5);
            using (var inner = new Pen(Color.FromArgb(120, 255, 255, 255), 1f))
                g.DrawRectangle(inner, rc.X + 7, rc.Y + 7, rc.Width - 15, rc.Height - 15);

            int pad = (int)(40 * s);
            int textLeft = rc.X + pad;
            int textW = rc.Width - pad * 2 - (int)(30 * s);

            if (_current.Speaker.Length > 0)
            {
                using var fontName = FontManager.GetSmallCapsFont(15f * s, FontStyle.Bold);
                TextRenderer.DrawText(g, _current.Speaker, fontName, new Point(textLeft, rc.Y + (int)(22 * s)), Color.White, TextFormatFlags.NoPrefix);

                using var fontTag = FontManager.GetSmallCapsFont(11f * s);
                TextRenderer.DrawText(g, _current.Tag, fontTag,
                    new Rectangle(rc.X, rc.Y + (int)(24 * s), rc.Width - pad, (int)(30 * s)), Color.Gray,
                    TextFormatFlags.Right | TextFormatFlags.NoPrefix);
            }

            float bodySize = _current.Text.Length > 120 ? 17.5f : 22f;
            using var fontBody = FontManager.GetItalicFont(bodySize * s);
            int bodyTop = rc.Y + (_current.Speaker.Length > 0 ? (int)(70 * s) : (int)(36 * s));
            string shown = _current.Text.Substring(0, Math.Min(_typedChars, _current.Text.Length));
            TextRenderer.DrawText(g, shown, fontBody,
                new Rectangle(textLeft, bodyTop, textW, rc.Bottom - bodyTop - (int)(20 * s)),
                Color.White, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

            // Continue marker once the line is fully typed
            if (_typedChars >= _current.Text.Length)
            {
                using var fontArrow = FontManager.GetSmallCapsFont(14f * s);
                TextRenderer.DrawText(g, "▼", fontArrow,
                    new Point(rc.Right - (int)(60 * s), rc.Bottom - (int)(55 * s)), Color.White, TextFormatFlags.NoPrefix);
            }
        }
    }
}
