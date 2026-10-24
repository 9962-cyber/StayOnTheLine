using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StayOnTheLine.Controls.Messenger
{
    /// <summary>
    /// OOP Principles: COMPOSITION & ENCAPSULATION
    /// The records-terminal messenger window: painted chrome (title bar, menu, contact list,
    /// pink contact header, tabs) around two real child controls, the <see cref="Thread"/> and the
    /// <see cref="Response"/> panel. The response panel's height eases to whatever its current
    /// mode needs, pushing the thread up or down. <see cref="TriggerGlitch"/> makes the contact
    /// status and header stutter when the operator-login match comes up.
    /// </summary>
    public sealed class MessengerWindow : Control, IThemedControl
    {
        private static readonly string[] GlitchStatuses = { "ONLINE", "0NL1NE", "OFFLINE", "ONLINE", "0FF", "ONLINE" };

        private readonly System.Windows.Forms.Timer _anim = new() { Interval = 16 };
        private readonly Random _rng = new();
        private MessengerTheme? _theme;
        private float _responseHeight = -1f;
        private int _glitchFrames;
        private int _jitter;
        private string _shownStatus = "ONLINE";

        public ChatThreadPanel Thread { get; } = new();
        public ResponsePanel Response { get; } = new();

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string ContactName { get; set; } = "Stef";
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string ContactRole { get; set; } = "Records Archive";

        public string Status => _status;
        private string _status = "ONLINE";
        private bool _statusAlert;

        /// <summary>Sets the contact status; alert statuses (e.g. USER UNAVAILABLE) are drawn in red.</summary>
        public void SetStatus(string status, bool alert)
        {
            _status = status;
            _shownStatus = status;
            _statusAlert = alert;
            Invalidate();
        }

        /// <summary>Left edge of the chat thread inside this window (the sidebar's right edge).</summary>
        public int ThreadLeft { get; private set; }

        public MessengerWindow()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = MessengerTheme.WindowBack;

            Controls.Add(Thread);
            Controls.Add(Response);
            Response.PreferredHeightChanged += (s, e) => _anim.Start();
            _anim.Tick += OnAnimationTick;
        }

        public void TriggerGlitch()
        {
            _glitchFrames = 34;
            _anim.Start();
        }

        public void ApplyTheme(MessengerTheme theme)
        {
            _theme = theme;
            _responseHeight = Response.PreferredHeight;
            LayoutChildren();
            Invalidate();
        }

        // Design units: title 30, menu 22, contact header 60, tabs 26
        private int BodyTop => _theme?.S(52) ?? 0;
        private int HeaderHeight => _theme?.S(60) ?? 0;
        private int TabsHeight => _theme?.S(26) ?? 0;

        // The contact list collapses when the window is squeezed next to the audit log
        private int SidebarWidth => _theme != null && Width >= _theme.S(760) ? (int)(Width * 0.29f) : 0;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            if (_theme == null || Width <= 0 || Height <= 0) return;
            int side = SidebarWidth;
            int threadTop = BodyTop + HeaderHeight + TabsHeight;
            int respH = Math.Min(Height - threadTop - _theme.S(40), (int)Math.Round(_responseHeight < 0 ? Response.PreferredHeight : _responseHeight));
            respH = Math.Max(0, respH);

            Response.Bounds = new Rectangle(side, Height - respH, Width - side, respH);
            Thread.Bounds = new Rectangle(side, threadTop, Width - side, Math.Max(1, Height - respH - threadTop));
            ThreadLeft = side;
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            bool busy = false;
            float target = Response.PreferredHeight;
            if (Math.Abs(target - _responseHeight) > 0.5f)
            {
                _responseHeight += (target - _responseHeight) * 0.2f;
                busy = true;
            }
            else
            {
                _responseHeight = target;
            }
            LayoutChildren();

            if (_glitchFrames > 0)
            {
                _glitchFrames--;
                busy = true;
                _shownStatus = _glitchFrames == 0 ? _status : GlitchStatuses[_rng.Next(GlitchStatuses.Length)];
                _jitter = _glitchFrames == 0 || _rng.Next(3) == 0 ? 0 : _rng.Next(-6, 7);
                Invalidate(new Rectangle(0, 0, Width, BodyTop + HeaderHeight + TabsHeight));
            }

            if (!busy) _anim.Stop();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (_theme == null || Width < 20 || Height < 20) return;
            var t = _theme;
            Shapes.Prepare(g);

            PaintTitleBar(g, t);
            PaintMenuBar(g, t);
            int side = SidebarWidth;
            if (side > 0) PaintSidebar(g, t, side);
            PaintContactHeader(g, t, side);

            using var border = new Pen(MessengerTheme.Border);
            g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }

        private void PaintTitleBar(Graphics g, MessengerTheme t)
        {
            var bar = new Rectangle(0, 0, Width, t.S(30));
            using (var grad = new LinearGradientBrush(bar, MessengerTheme.TitleTop, MessengerTheme.TitleBottom, 90f))
                g.FillRectangle(grad, bar);

            // Two overlapping "buddy" rings as the app icon
            using (var ring = new Pen(Color.FromArgb(220, 230, 242), t.F(2f)))
            {
                float r = t.F(11);
                g.DrawEllipse(ring, t.F(12), (bar.Height - r) / 2f, r, r);
                g.DrawEllipse(ring, t.F(20), (bar.Height - r) / 2f, r, r);
            }

            using var white = new SolidBrush(Color.White);
            g.DrawString("Messenger", t.Title, white, new RectangleF(t.F(42), 0, t.F(220), bar.Height), Shapes.Left);

            // Decorative window buttons
            float bs = t.F(20), gap = t.F(4);
            float bx = Width - t.F(8) - bs * 3 - gap * 2;
            string[] glyphs = { "_", "□", "×" };
            using var boxBrush = new SolidBrush(Color.FromArgb(70, 255, 255, 255));
            using var boxPen = new Pen(Color.FromArgb(150, 255, 255, 255));
            for (int i = 0; i < 3; i++)
            {
                var box = new RectangleF(bx + i * (bs + gap), (bar.Height - bs) / 2f, bs, bs);
                g.FillRectangle(boxBrush, box);
                g.DrawRectangle(boxPen, box.X, box.Y, box.Width, box.Height);
                g.DrawString(glyphs[i], t.Mono, white, box, Shapes.Center);
            }

            using var faint = new SolidBrush(Color.FromArgb(190, 255, 255, 255));
            g.DrawString("INTERNAL NETWORK", t.Caps, faint, new RectangleF(0, 0, bx - t.F(12), bar.Height), Shapes.Right);
        }

        private void PaintMenuBar(Graphics g, MessengerTheme t)
        {
            var bar = new RectangleF(0, t.F(30), Width, t.F(22));
            using (var fill = new SolidBrush(MessengerTheme.PanelBack)) g.FillRectangle(fill, bar);
            using (var line = new Pen(MessengerTheme.Border)) g.DrawLine(line, 0, bar.Bottom - 1, Width, bar.Bottom - 1);

            using var ink = new SolidBrush(MessengerTheme.Ink);
            float x = t.F(12);
            foreach (string item in new[] { "File", "Contacts", "Archive", "Help" })
            {
                g.DrawString(item, t.Body, ink, new RectangleF(x, bar.Y, t.F(90), bar.Height), Shapes.Left);
                x += g.MeasureString(item, t.Body).Width + t.F(14);
            }

            using var muted = new SolidBrush(MessengerTheme.MutedInk);
            g.DrawString("ENCRYPTION: LOCAL", t.Caps, muted, new RectangleF(0, bar.Y, Width - t.F(12), bar.Height), Shapes.Right);
        }

        private void PaintSidebar(Graphics g, MessengerTheme t, int side)
        {
            float top = BodyTop;
            using var muted = new SolidBrush(MessengerTheme.MutedInk);
            using var ink = new SolidBrush(MessengerTheme.Ink);

            var search = new RectangleF(t.F(10), top + t.F(12), side - t.F(20), t.F(26));
            g.FillRectangle(Brushes.White, search);
            using (var pen = new Pen(MessengerTheme.Border)) g.DrawRectangle(pen, search.X, search.Y, search.Width, search.Height);
            g.DrawString("Search contacts", t.BodyItalic, muted, new RectangleF(search.X + t.F(8), search.Y, search.Width - t.F(16), search.Height), Shapes.Left);

            var group = new RectangleF(t.F(12), top + t.F(48), side - t.F(24), t.F(20));
            g.DrawString("NIGHT DESK CONTACTS", t.Caps, muted, group, Shapes.Left);
            g.DrawString("1", t.Caps, muted, group, Shapes.Right);

            var row = new RectangleF(t.F(4), top + t.F(72), side - t.F(8), t.F(54));
            using (var hl = new SolidBrush(MessengerTheme.Highlight)) g.FillRectangle(hl, row);
            Shapes.DrawAvatar(g, new RectangleF(row.X + t.F(8), row.Y + t.F(9), t.F(36), t.F(36)));
            float textX = row.X + t.F(54);
            g.DrawString(ContactName, t.Body, ink, new RectangleF(textX, row.Y + t.F(6), row.Right - textX - t.F(20), t.F(22)), Shapes.Left);
            g.DrawString(ContactRole, t.Caps, muted, new RectangleF(textX, row.Y + t.F(28), row.Right - textX - t.F(20), t.F(18)), Shapes.Left);
            using (var dot = new SolidBrush(MessengerTheme.IncomingEdge))
                g.FillEllipse(dot, row.Right - t.F(16), row.Y + row.Height / 2f - t.F(4), t.F(8), t.F(8));

            using var sep = new Pen(MessengerTheme.Border);
            g.DrawLine(sep, side - 1, top, side - 1, Height);
        }

        private void PaintContactHeader(Graphics g, MessengerTheme t, int side)
        {
            float x = side + _jitter;
            var header = new RectangleF(x, BodyTop, Width - side, HeaderHeight);
            using (var grad = new LinearGradientBrush(header, MessengerTheme.HeaderPinkTop, MessengerTheme.HeaderPinkBottom, 90f))
                g.FillRectangle(grad, header);

            Shapes.DrawAvatar(g, new RectangleF(header.X + t.F(12), header.Y + t.F(10), t.F(40), t.F(40)));

            using var ink = new SolidBrush(MessengerTheme.Ink);
            using var muted = new SolidBrush(MessengerTheme.MutedInk);
            float textX = header.X + t.F(64);
            g.DrawString(ContactName, t.CapsLarge, ink, new RectangleF(textX, header.Y + t.F(6), t.F(300), t.F(28)), Shapes.Left);
            g.DrawString(ContactRole, t.Caps, muted, new RectangleF(textX, header.Y + t.F(32), t.F(300), t.F(20)), Shapes.Left);

            bool glitching = _glitchFrames > 0;
            using var statusBrush = new SolidBrush(glitching || _statusAlert ? MessengerTheme.AlertInk : MessengerTheme.SystemInk);
            g.DrawString(_shownStatus, t.Caps, statusBrush, new RectangleF(header.X, header.Y, header.Width - t.F(16), header.Height), Shapes.Right);

            // Tabs strip
            var tabs = new RectangleF(side, header.Bottom, Width - side, TabsHeight);
            using (var fill = new SolidBrush(MessengerTheme.WindowBack)) g.FillRectangle(fill, tabs);
            var active = new RectangleF(tabs.X + t.F(8), tabs.Y + t.F(3), t.F(110), tabs.Height - t.F(3));
            g.FillRectangle(Brushes.White, active);
            using (var pen = new Pen(MessengerTheme.Border))
            {
                g.DrawLine(pen, tabs.X, tabs.Bottom - 1, Width, tabs.Bottom - 1);
                g.DrawRectangle(pen, active.X, active.Y, active.Width, active.Height - 1);
            }
            g.DrawString("MESSAGES", t.Caps, ink, active, Shapes.Center);
            g.DrawString("PERSONNEL FILE", t.Caps, muted, new RectangleF(active.Right + t.F(8), tabs.Y + t.F(3), t.F(170), active.Height), Shapes.Center);
            g.DrawString("LOCKED SESSION", t.Caps, muted, new RectangleF(tabs.X, tabs.Y, tabs.Width - t.F(14), tabs.Height), Shapes.Right);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _anim.Dispose();
            base.Dispose(disposing);
        }
    }
}
