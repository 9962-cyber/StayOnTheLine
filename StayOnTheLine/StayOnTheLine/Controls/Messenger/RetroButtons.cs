using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Controls.Messenger
{
    /// <summary>
    /// OOP Principles: INHERITANCE & POLYMORPHISM
    /// Base for the messenger's clickable controls: owns hover/pressed state, the click
    /// sound and double buffering; subclasses only override <see cref="PaintControl"/>.
    /// Not focusable, so keyboard shortcuts stay with the scene form.
    /// </summary>
    public abstract class RetroControlBase : Control, IThemedControl
    {
        protected MessengerTheme? Theme { get; private set; }
        protected bool IsHovered { get; private set; }
        protected bool IsPressed { get; private set; }

        protected RetroControlBase()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            Cursor = Cursors.Hand;
        }

        public virtual void ApplyTheme(MessengerTheme theme)
        {
            Theme = theme;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { IsHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { IsHovered = false; IsPressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { IsPressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { IsPressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        protected override void OnClick(EventArgs e)
        {
            if (!Enabled) return;
            AudioManager.Instance.PlayKeyClick();
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (Theme == null || Width < 2 || Height < 2) return;
            Shapes.Prepare(g);
            PaintControl(g, Theme, ClientRectangle);
        }

        protected abstract void PaintControl(Graphics g, MessengerTheme t, Rectangle bounds);
    }

    /// <summary>
    /// A numbered response row ("01  Some records have been inconsistent.").
    /// </summary>
    public sealed class OptionRowButton : RetroControlBase
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Number { get; set; } = "01";

        public OptionRowButton()
        {
            BackColor = MessengerTheme.PanelBack;
        }

        protected override void PaintControl(Graphics g, MessengerTheme t, Rectangle bounds)
        {
            var rect = new RectangleF(1, 1, bounds.Width - 3, bounds.Height - 3);
            using var path = Shapes.Rounded(rect, rect.Height / 2f);

            bool live = Enabled && IsHovered;
            Color fill = !Enabled ? Color.FromArgb(238, 237, 232)
                : IsPressed ? Color.FromArgb(186, 212, 240)
                : live ? MessengerTheme.Highlight : Color.White;
            using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
            using (var pen = new Pen(live ? MessengerTheme.HighlightEdge : MessengerTheme.Border, live ? 1.5f : 1f))
                g.DrawPath(pen, path);

            float numW = t.F(40);
            using (var numBrush = new SolidBrush(live ? MessengerTheme.HighlightEdge : MessengerTheme.MutedInk))
                g.DrawString(Number, t.Mono, numBrush, new RectangleF(t.F(10), 0, numW, bounds.Height), Shapes.Center);

            using var textBrush = new SolidBrush(Enabled ? MessengerTheme.Ink : MessengerTheme.MutedInk);
            g.DrawString(Text, t.Body, textBrush,
                new RectangleF(t.F(10) + numW + t.F(6), 0, bounds.Width - numW - t.F(30), bounds.Height), Shapes.Left);
        }
    }

    /// <summary>
    /// Beveled, Windows-98-style command button (SAVE LOG, OPEN AUDIT LOG).
    /// <see cref="Primary"/> gives the larger blue action style; <see cref="Pulse"/> draws a red
    /// attention ring while a new notification is waiting.
    /// </summary>
    public sealed class BevelButton : RetroControlBase
    {
        private float _pulse;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Primary { get; set; }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public float Pulse
        {
            get => _pulse;
            set { _pulse = Math.Clamp(value, 0f, 1f); Invalidate(); }
        }

        public BevelButton()
        {
            BackColor = MessengerTheme.PanelBack;
        }

        protected override void PaintControl(Graphics g, MessengerTheme t, Rectangle bounds)
        {
            var r = new Rectangle(0, 0, bounds.Width - 1, bounds.Height - 1);
            g.SmoothingMode = SmoothingMode.None;

            if (Primary)
            {
                using var grad = new LinearGradientBrush(r,
                    IsHovered && Enabled ? Color.FromArgb(236, 244, 252) : Color.FromArgb(228, 238, 250),
                    IsPressed ? Color.FromArgb(168, 198, 232) : Color.FromArgb(190, 214, 240), 90f);
                g.FillRectangle(grad, r);
            }
            else
            {
                using var fill = new SolidBrush(IsPressed ? Color.FromArgb(210, 208, 202)
                    : IsHovered && Enabled ? Color.FromArgb(242, 240, 234) : Color.FromArgb(226, 224, 218));
                g.FillRectangle(fill, r);
            }

            // Bevel: light top-left, dark bottom-right (swapped while pressed)
            Color light = Color.White, dark = Color.FromArgb(118, 118, 122);
            if (IsPressed) (light, dark) = (dark, light);
            using (var lp = new Pen(light)) { g.DrawLine(lp, 0, 0, r.Right, 0); g.DrawLine(lp, 0, 0, 0, r.Bottom); }
            using (var dp = new Pen(dark)) { g.DrawLine(dp, 0, r.Bottom, r.Right, r.Bottom); g.DrawLine(dp, r.Right, 0, r.Right, r.Bottom); }
            using (var edge = new Pen(Primary ? MessengerTheme.HighlightEdge : MessengerTheme.Border))
                g.DrawRectangle(edge, 1, 1, r.Width - 2, r.Height - 2);

            if (_pulse > 0f)
            {
                using var ring = new Pen(Shapes.Fade(MessengerTheme.AlertInk, _pulse), Math.Max(2f, t.F(2f)));
                g.DrawRectangle(ring, 1, 1, r.Width - 2, r.Height - 2);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            string label = Primary ? Text.ToUpperInvariant() : Text;
            Font font = Primary ? t.Title : t.Caps;
            // Long action labels ("ATTEMPT TO RECONNECT") drop to the smaller caps face instead of being cut off
            if (Primary && g.MeasureString(label, font).Width > bounds.Width - t.F(16)) font = t.Caps;
            Color ink = !Enabled ? MessengerTheme.MutedInk : Primary ? Color.FromArgb(28, 58, 98) : MessengerTheme.Ink;
            using var brush = new SolidBrush(ink);
            g.DrawString(label, font, brush, new RectangleF(0, 0, bounds.Width, bounds.Height), Shapes.Center);
        }
    }
}
