using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Controls
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Caption for things said out loud at the desk (Charm) and room narration, typed out in the
    /// same dark, double-ruled box as the other in-room dialogue. Unlike the messenger, these
    /// lines are not logged on the terminal.
    /// </summary>
    public sealed class RoomCaption : Control
    {
        private readonly System.Windows.Forms.Timer _typeTimer = new() { Interval = 18 };
        private string _speaker = string.Empty;
        private string _text = string.Empty;
        private int _typed;

        public RoomCaption()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.Black;
            Cursor = Cursors.Hand;

            _typeTimer.Tick += (s, e) =>
            {
                _typed = Math.Min(_text.Length, _typed + 2);
                if (_typed % 6 == 0) AudioManager.Instance.PlayTypewriterKeystroke();
                if (_typed >= _text.Length) _typeTimer.Stop();
                Invalidate();
            };
        }

        public bool IsTyping => _typed < _text.Length;

        /// <summary>Shows a line; an empty speaker means narration.</summary>
        public void ShowLine(string speaker, string text)
        {
            _speaker = speaker;
            _text = text;
            _typed = 0;
            Visible = true;
            _typeTimer.Start();
            Invalidate();
        }

        public void CompleteTyping()
        {
            _typed = _text.Length;
            _typeTimer.Stop();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            int W = Width, H = Height;
            if (W < 10 || H < 10) return;
            float s = H / 150f;

            using (var bg = new SolidBrush(Color.FromArgb(10, 10, 10))) g.FillRectangle(bg, 0, 0, W, H);
            using (var outer = new Pen(Color.FromArgb(225, 255, 255, 255), 1.5f)) g.DrawRectangle(outer, 2, 2, W - 5, H - 5);
            using (var inner = new Pen(Color.FromArgb(120, 255, 255, 255), 1f)) g.DrawRectangle(inner, 7, 7, W - 15, H - 15);

            float pad = 24f * s;
            float top = pad;
            bool narration = _speaker.Length == 0;

            if (!narration)
            {
                using var nameFont = FontManager.GetSmallCapsFont(14f * s);
                using var tagFont = FontManager.GetSmallCapsFont(9.5f * s);
                g.DrawString(_speaker, nameFont, Brushes.White, pad, top - 4f * s);
                using var grey = new SolidBrush(Color.Gray);
                using var tagFormat = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString("AT YOUR DESK", tagFont, grey, new RectangleF(pad, top, W - pad * 2, 24f * s), tagFormat);
                top += 34f * s;
            }

            using var body = narration ? FontManager.GetItalicFont(14.5f * s) : FontManager.GetDialogueFont(16f * s);
            using var ink = new SolidBrush(narration ? Color.FromArgb(190, 190, 186) : Color.White);
            string shown = _text.Substring(0, Math.Min(_typed, _text.Length));
            g.DrawString(shown, body, ink, new RectangleF(pad, top, W - pad * 2 - 24f * s, H - top - pad));

            if (!IsTyping)
            {
                using var arrow = FontManager.GetSmallCapsFont(11f * s);
                g.DrawString("▼", arrow, Brushes.White, W - pad - 16f * s, H - pad - 14f * s);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _typeTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
