using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using StayOnTheLine.Models.Dialogue;

namespace StayOnTheLine.Controls.Messenger
{
    /// <summary>
    /// OOP Principles: ABSTRACTION, INHERITANCE & POLYMORPHISM
    /// One row in the chat thread. The thread only knows this base type: it asks each entry
    /// for its height and lets it paint itself, so speech bubbles, system notices, narration
    /// and the typing indicator are interchangeable. Rows slide and fade in through
    /// <see cref="Reveal"/>, driven by the thread's animation timer.
    /// </summary>
    public abstract class ChatEntry : Control, IThemedControl
    {
        private static readonly Bitmap s_measureSurface = new(1, 1);
        protected static readonly Graphics Measurer = CreateMeasurer();

        protected MessengerTheme? Theme { get; private set; }
        public ChatMessage? Message { get; }
        public float Reveal { get; private set; }

        protected ChatEntry(ChatMessage? message)
        {
            Message = message;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = MessengerTheme.ThreadBack;
        }

        /// <summary>Picks the entry type for a delivered line.</summary>
        public static ChatEntry Create(ChatMessage message) => message.Sender switch
        {
            ChatSender.System => new SystemLineEntry(message),
            ChatSender.Narration => new NarrationEntry(message),
            _ => new ChatBubbleEntry(message)
        };

        public void ApplyTheme(MessengerTheme theme)
        {
            Theme = theme;
            Invalidate();
        }

        public abstract int MeasureHeight(int width);

        /// <summary>Advances the appear animation; returns true while still animating.</summary>
        internal bool StepReveal(float amount)
        {
            if (Reveal >= 1f) return false;
            Reveal = Math.Min(1f, Reveal + amount);
            Invalidate();
            return true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            if (Theme == null || Width < 2 || Height < 2) return;
            Shapes.Prepare(e.Graphics);
            PaintEntry(e.Graphics, Theme, Shapes.Ease(Reveal));
        }

        protected abstract void PaintEntry(Graphics g, MessengerTheme t, float reveal);

        protected static string FormatTime(ChatMessage m) => m.Timestamp?.ToString("HH:mm") ?? "--:--";

        private static Graphics CreateMeasurer()
        {
            var g = Graphics.FromImage(s_measureSurface);
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            return g;
        }
    }

    /// <summary>
    /// Speech bubble: Stef's on the left (pink), the player's on the right (blue), with a tail
    /// and a small sender/time stamp. A line stamped earlier than the real clock shows its time in red.
    /// </summary>
    public sealed class ChatBubbleEntry : ChatEntry
    {
        private const float MaxWidthFraction = 0.68f;

        public ChatBubbleEntry(ChatMessage message) : base(message) { }

        private bool Incoming => Message!.Sender != ChatSender.Player;

        private (SizeF Text, float Width, float Height) MeasureBubble(MessengerTheme t, int width)
        {
            float maxText = Math.Max(t.F(60), width * MaxWidthFraction - t.F(30));
            SizeF text = Measurer.MeasureString(Message!.Text, t.Body, (int)maxText);
            return (text, text.Width + t.F(30), text.Height + t.F(16));
        }

        public override int MeasureHeight(int width)
        {
            if (Theme == null) return 0;
            var bubble = MeasureBubble(Theme, width);
            return (int)Math.Ceiling(bubble.Height + Theme.F(8 + 18));
        }

        protected override void PaintEntry(Graphics g, MessengerTheme t, float reveal)
        {
            var (text, bw, bh) = MeasureBubble(t, Width);
            float slide = (1f - reveal) * t.F(16);
            float margin = t.F(18);
            float x = Incoming ? margin - slide : Width - margin - bw + slide;
            var rect = new RectangleF(x, t.F(4), bw, bh);

            Color fill = Incoming ? MessengerTheme.IncomingFill : MessengerTheme.OutgoingFill;
            Color edge = Incoming ? MessengerTheme.IncomingEdge : MessengerTheme.OutgoingEdge;
            Color ink = Incoming ? MessengerTheme.IncomingText : MessengerTheme.OutgoingText;

            // Tail at the bottom corner on the sender's side
            float tailX = Incoming ? rect.X + t.F(14) : rect.Right - t.F(14);
            float dir = Incoming ? -1f : 1f;
            var tail = new[]
            {
                new PointF(tailX - t.F(7), rect.Bottom - t.F(3)),
                new PointF(tailX + dir * t.F(12), rect.Bottom + t.F(6)),
                new PointF(tailX + t.F(7), rect.Bottom - t.F(3))
            };

            using (var path = Shapes.Rounded(rect, Math.Min(bh / 2f, t.F(15))))
            using (var brush = new SolidBrush(Shapes.Fade(fill, reveal)))
            using (var pen = new Pen(Shapes.Fade(edge, reveal), 1f))
            {
                g.FillPolygon(brush, tail);
                g.DrawLines(pen, new[] { tail[0], tail[1], tail[2] });
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            using (var textBrush = new SolidBrush(Shapes.Fade(ink, reveal)))
                g.DrawString(Message!.Text, t.Body, textBrush,
                    new RectangleF(rect.X + t.F(15), rect.Y + t.F(8), text.Width + 2, text.Height + 2));

            // "STEF · 01:52" under the bubble; a skewed (impossible) time is printed in red
            bool skewed = Message.TimestampSkewMinutes < 0;
            string stamp = $"{Message.SpeakerName}  ·  {FormatTime(Message)}";
            var stampRect = new RectangleF(Incoming ? rect.X + t.F(6) : rect.X, rect.Bottom + t.F(5), Math.Max(bw, t.F(160)), t.F(16));
            if (!Incoming) stampRect.X = rect.Right - stampRect.Width - t.F(6);
            using var stampBrush = new SolidBrush(Shapes.Fade(skewed ? MessengerTheme.AlertInk : MessengerTheme.MutedInk, reveal));
            g.DrawString(stamp, t.MonoSmall, stampBrush, stampRect, Incoming ? Shapes.Left : Shapes.Right);
        }
    }

    /// <summary>Centered single-purpose line (system notices, narration).</summary>
    public abstract class CenteredLineEntry : ChatEntry
    {
        protected CenteredLineEntry(ChatMessage message) : base(message) { }

        protected abstract Font LineFont(MessengerTheme t);
        protected abstract Color LineColor { get; }
        protected virtual string DisplayText => Message!.Text;
        protected virtual bool DrawRules => false;

        private float TextWidth(int width) => Math.Max(40f, width - Theme!.F(90));

        public override int MeasureHeight(int width)
        {
            if (Theme == null) return 0;
            SizeF size = Measurer.MeasureString(DisplayText, LineFont(Theme), (int)TextWidth(width));
            return (int)Math.Ceiling(size.Height + Theme.F(16));
        }

        protected override void PaintEntry(Graphics g, MessengerTheme t, float reveal)
        {
            Font font = LineFont(t);
            float maxW = TextWidth(Width);
            SizeF size = g.MeasureString(DisplayText, font, (int)maxW);
            var rect = new RectangleF((Width - size.Width) / 2f, (Height - size.Height) / 2f, size.Width + 2, size.Height + 2);

            if (DrawRules && size.Height < font.GetHeight(g) * 1.6f)
            {
                using var rule = new Pen(Shapes.Fade(LineColor, 0.35f * reveal), 1f);
                float mid = Height / 2f, gap = t.F(10), inset = t.F(18) + (1f - reveal) * t.F(40);
                g.DrawLine(rule, inset, mid, rect.X - gap, mid);
                g.DrawLine(rule, rect.Right + gap, mid, Width - inset, mid);
            }

            using var brush = new SolidBrush(Shapes.Fade(LineColor, reveal));
            using var format = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(DisplayText, font, brush, rect, format);
        }
    }

    /// <summary>"SYSTEM // ..." notices in terminal type; alerts are red.</summary>
    public sealed class SystemLineEntry : CenteredLineEntry
    {
        public SystemLineEntry(ChatMessage message) : base(message) { }

        protected override Font LineFont(MessengerTheme t) => t.Mono;
        protected override Color LineColor => Message!.IsAlert ? MessengerTheme.AlertInk : MessengerTheme.SystemInk;
        protected override string DisplayText => $"SYSTEM // {Message!.Text}";
        protected override bool DrawRules => true;
    }

    /// <summary>Stage directions in grey italics, e.g. "[Another message appears ...]".</summary>
    public sealed class NarrationEntry : CenteredLineEntry
    {
        public NarrationEntry(ChatMessage message) : base(message) { }

        protected override Font LineFont(MessengerTheme t) => t.BodyItalic;
        protected override Color LineColor => MessengerTheme.MutedInk;
    }

    /// <summary>"Stef is typing" pill with three bouncing dots.</summary>
    public sealed class TypingIndicatorEntry : ChatEntry
    {
        private float _phase;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Label { get; set; } = "Steff is typing";

        public TypingIndicatorEntry() : base(null) { }

        internal void Tick()
        {
            _phase += 0.12f;
            if (Visible) Invalidate();
        }

        public override int MeasureHeight(int width) => Theme?.S(40) ?? 0;

        protected override void PaintEntry(Graphics g, MessengerTheme t, float reveal)
        {
            var pill = new RectangleF(t.F(18), t.F(6), t.F(58), t.F(26));
            using (var path = Shapes.Rounded(pill, pill.Height / 2f))
            using (var brush = new SolidBrush(MessengerTheme.IncomingFill))
            using (var pen = new Pen(MessengerTheme.IncomingEdge))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            float d = t.F(6);
            for (int i = 0; i < 3; i++)
            {
                float bounce = (float)Math.Max(0, Math.Sin(_phase - i * 0.9f)) * t.F(4);
                float cx = pill.X + t.F(16) + i * t.F(13);
                using var dot = new SolidBrush(Color.FromArgb(150 + i * 25, MessengerTheme.IncomingText));
                g.FillEllipse(dot, cx - d / 2f, pill.Y + pill.Height / 2f - d / 2f - bounce, d, d);
            }

            using var label = new SolidBrush(MessengerTheme.MutedInk);
            g.DrawString(Label + "...", t.BodyItalic, label,
                new RectangleF(pill.Right + t.F(10), pill.Y, Width - pill.Right - t.F(20), pill.Height), Shapes.Left);
        }
    }
}
