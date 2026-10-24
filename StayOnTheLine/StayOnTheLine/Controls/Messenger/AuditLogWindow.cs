using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StayOnTheLine.Models.Dialogue;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Controls.Messenger
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Read-only records-archive audit log, drawn as a dark terminal next to the messenger.
    /// Rows print one at a time; rows tied to the player's login flicker red, then the two
    /// detection lines type out and <see cref="RevealCompleted"/> fires so the scene can let
    /// Stef react.
    /// </summary>
    public sealed class AuditLogWindow : Control, IThemedControl
    {
        private const int FramesPerRow = 6;
        private const int FramesPerFooterLine = 9;

        private static readonly Color Back = Color.FromArgb(10, 13, 11);
        private static readonly Color Ink = Color.FromArgb(178, 204, 172);
        private static readonly Color Dim = Color.FromArgb(96, 120, 98);
        private static readonly Color Alarm = Color.FromArgb(232, 74, 70);
        private static readonly float[] Columns = { 0f, 0.13f, 0.27f, 0.51f, 0.76f };

        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 60 };
        private IReadOnlyList<AuditRecord> _records = Array.Empty<AuditRecord>();
        private string[] _footer = Array.Empty<string>();
        private MessengerTheme? _theme;
        private Rectangle _closeRect;
        private int _frame;
        private bool _completed;

        public event EventHandler? RevealCompleted;
        public event EventHandler? CloseRequested;

        public AuditLogWindow()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Back;
            _timer.Tick += OnTick;
        }

        private int RevealFrames => _records.Count * FramesPerRow + FramesPerRow + _footer.Length * FramesPerFooterLine;

        public void Open(IReadOnlyList<AuditRecord> records, string operatorName)
        {
            _records = records;
            string op = string.IsNullOrWhiteSpace(operatorName) ? "OPERATOR" : operatorName.Trim().ToUpperInvariant();
            _footer = new[]
            {
                "> MULTIPLE RECORD MODIFICATIONS DETECTED.",
                $"> MODIFICATIONS ASSOCIATED WITH OPERATOR LOGIN: {op}"
            };
            _frame = 0;
            _completed = false;
            _timer.Start();
            Invalidate();
        }

        public void ApplyTheme(MessengerTheme theme)
        {
            _theme = theme;
            Invalidate();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _frame++;
            if (_frame % FramesPerRow == 0 && _frame <= _records.Count * FramesPerRow)
                AudioManager.Instance.PlayTypewriterKeystroke();

            if (!_completed && _frame >= RevealFrames)
            {
                _completed = true;
                RevealCompleted?.Invoke(this, EventArgs.Empty);
            }
            Invalidate(); // keeps the cursor blinking and flagged rows flickering
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) _timer.Stop();
            else if (_records.Count > 0) _timer.Start();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = _closeRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_closeRect.Contains(e.Location)) CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Back);
            if (_theme == null || Width < 20 || Height < 20) return;
            var t = _theme;
            Shapes.Prepare(g);

            // Title bar
            var bar = new Rectangle(0, 0, Width, t.S(30));
            using (var barFill = new SolidBrush(Color.FromArgb(46, 18, 20))) g.FillRectangle(barFill, bar);
            using (var title = new SolidBrush(Color.FromArgb(236, 206, 204)))
                g.DrawString("AUDIT LOG  //  RECORDS ARCHIVE", t.Caps, title, new RectangleF(t.F(12), 0, Width - t.F(60), bar.Height), Shapes.Left);

            int cs = t.S(20);
            _closeRect = new Rectangle(Width - cs - t.S(8), (bar.Height - cs) / 2, cs, cs);
            using (var closePen = new Pen(Color.FromArgb(200, 236, 206, 204)))
                g.DrawRectangle(closePen, _closeRect);
            using (var x = new SolidBrush(Color.FromArgb(236, 206, 204)))
            {
                g.DrawString("×", t.Mono, x, _closeRect, Shapes.Center);
                g.DrawString("READ ONLY", t.Caps, x, new RectangleF(0, 0, _closeRect.Left - t.F(10), bar.Height), Shapes.Right);
            }

            float pad = t.F(12);
            float tableW = Width - pad * 2;
            float y = bar.Bottom + t.F(10);
            float rowH = t.F(24);

            using var dim = new SolidBrush(Dim);
            DrawRow(g, t.MonoSmall, dim, pad, y, tableW, rowH, "WHEN", "RECORD", "LOCATION", "CHANGE", "OPERATOR");
            y += rowH;
            using (var rule = new Pen(Dim)) g.DrawLine(rule, pad, y, Width - pad, y);
            y += t.F(4);

            int shownRows = Math.Min(_records.Count, _frame / FramesPerRow);
            using var ink = new SolidBrush(Ink);
            for (int i = 0; i < shownRows; i++)
            {
                var r = _records[i];
                bool flicker = r.MatchesPlayer && (_frame + i * 3) % 23 < 2;
                using var opBrush = new SolidBrush(r.MatchesPlayer ? (flicker ? Back : Alarm) : Ink);
                DrawRow(g, t.MonoSmall, ink, pad, y, tableW, rowH, r.When, r.RecordId, r.Location, r.Change, null);
                g.DrawString(r.Operator, t.MonoSmall, opBrush, new RectangleF(pad + tableW * Columns[4], y, tableW * (1 - Columns[4]), rowH), Shapes.Left);
                y += rowH;
            }

            // Detection lines after the table
            int footerStart = _records.Count * FramesPerRow + FramesPerRow;
            y += t.F(14);
            for (int i = 0; i < _footer.Length; i++)
            {
                int lineFrame = _frame - footerStart - i * FramesPerFooterLine;
                if (lineFrame < 0) break;
                string line = _footer[i];
                int chars = Math.Min(line.Length, lineFrame * 7);
                using var brush = new SolidBrush(i == _footer.Length - 1 ? Alarm : Ink);
                g.DrawString(line.Substring(0, chars), t.Mono, brush, new RectangleF(pad, y, tableW, t.F(40)));
                y += t.F(22);
            }

            // Blinking block cursor
            if ((_frame / 6) % 2 == 0)
            {
                using var cursor = new SolidBrush(Ink);
                g.FillRectangle(cursor, pad, y + t.F(4), t.F(8), t.F(14));
            }

            // Phosphor scanlines over the whole log
            using var scan = new Pen(Color.FromArgb(28, 0, 0, 0));
            for (int sy = 0; sy < Height; sy += 3) g.DrawLine(scan, 0, sy, Width, sy);

            using var border = new Pen(Color.FromArgb(70, 92, 76));
            g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }

        private static void DrawRow(Graphics g, Font font, Brush brush, float x, float y, float w, float h,
            string c0, string c1, string c2, string c3, string? c4)
        {
            string[] cells = { c0, c1, c2, c3, c4 ?? string.Empty };
            for (int i = 0; i < cells.Length; i++)
            {
                float left = x + w * Columns[i];
                float right = i + 1 < Columns.Length ? x + w * Columns[i + 1] : x + w;
                if (cells[i].Length > 0)
                    g.DrawString(cells[i], font, brush, new RectangleF(left, y, right - left - 4, h), Shapes.Left);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
