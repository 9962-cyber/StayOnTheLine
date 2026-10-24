using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Controls
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// A spoken-response choice shown under a character's dialogue box in the room: a dark box with
    /// a thin light border and typewriter text, lighter while hovered. Not focusable, so the number
    /// keys stay with the scene form.
    /// </summary>
    public sealed class RoomChoiceButton : Control
    {
        private bool _hovered;

        public RoomChoiceButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.Black;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

        protected override void OnClick(EventArgs e)
        {
            AudioManager.Instance.PlayKeyClick();
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            int W = Width, H = Height;

            using (var bg = new SolidBrush(_hovered ? Color.FromArgb(58, 58, 58) : Color.FromArgb(14, 14, 14)))
                g.FillRectangle(bg, 0, 0, W, H);
            using (var border = new Pen(Color.FromArgb(_hovered ? 255 : 210, 235, 235, 235), 1.5f))
                g.DrawRectangle(border, 1, 1, W - 3, H - 3);

            // Sized so a full option ("Say you're checking an unusual record.") fits on two lines
            float size = Math.Clamp(H / 6.4f, 7f, 14f);
            using var font = FontManager.GetMonospaceFont(size);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisWord
            };
            float pad = H * 0.18f;
            g.DrawString(Text, font, Brushes.White, new RectangleF(pad, 2, W - pad * 2, H - 4), format);
        }
    }
}
