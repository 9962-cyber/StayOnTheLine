using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StayOnTheLine.Models.Dialogue;

namespace StayOnTheLine.Controls.Messenger
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// The scrolling message history. Stacks <see cref="ChatEntry"/> rows, eases the view down
    /// to each new message, animates rows in and handles the mouse wheel itself (via a message
    /// filter, since the rows never take focus). Clicking anywhere raises <see cref="Control.Click"/>
    /// so the scene can use it to skip a wait.
    /// </summary>
    public sealed class ChatThreadPanel : Control, IThemedControl, IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x020A;

        private readonly List<ChatEntry> _entries = new();
        private readonly TypingIndicatorEntry _typing = new() { Visible = false };
        private readonly System.Windows.Forms.Timer _anim = new() { Interval = 16 };
        private MessengerTheme? _theme;
        private float _scroll;
        private float _scrollTarget;
        private int _contentHeight;

        public ChatThreadPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = MessengerTheme.ThreadBack;

            _typing.Click += (s, e) => OnClick(e);
            Controls.Add(_typing);
            _anim.Tick += OnAnimationTick;
            Application.AddMessageFilter(this);
        }

        public int Count => _entries.Count;

        public void Append(ChatMessage message)
        {
            var entry = ChatEntry.Create(message);
            if (_theme != null) entry.ApplyTheme(_theme);
            entry.Click += (s, e) => OnClick(e);
            _entries.Add(entry);
            Controls.Add(entry);
            Relayout(followBottom: true);
            _anim.Start();
        }

        public void SetTyping(bool visible, string label = "Stef is typing")
        {
            if (_typing.Visible == visible && _typing.Label == label) return;
            _typing.Label = label;
            _typing.Visible = visible;
            Relayout(followBottom: true);
            if (visible) _anim.Start();
        }

        public void ApplyTheme(MessengerTheme theme)
        {
            _theme = theme;
            Relayout(followBottom: true);
            _scroll = _scrollTarget;
            PositionEntries();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout(followBottom: true);
            _scroll = _scrollTarget;
            PositionEntries();
        }

        private int ScrollbarWidth => _theme?.S(8) ?? 8;
        private float MaxScroll => Math.Max(0, _contentHeight - Height);

        private void Relayout(bool followBottom)
        {
            if (_theme == null || Width <= 0) return;
            int width = Width - ScrollbarWidth;
            int y = _theme.S(12);
            foreach (var entry in _entries)
            {
                entry.Height = Math.Max(1, entry.MeasureHeight(width));
                entry.Width = width;
                entry.Tag = y;
                y += entry.Height + _theme.S(4);
            }
            if (_typing.Visible)
            {
                _typing.Height = _typing.MeasureHeight(width);
                _typing.Width = width;
                _typing.Tag = y;
                y += _typing.Height;
            }
            _contentHeight = y + _theme.S(12);

            if (followBottom) _scrollTarget = MaxScroll;
            _scrollTarget = Math.Clamp(_scrollTarget, 0, MaxScroll);
            PositionEntries();
            _anim.Start();
        }

        private void PositionEntries()
        {
            SuspendLayout();
            int offset = (int)Math.Round(_scroll);
            foreach (var entry in _entries)
                entry.Location = new Point(0, (entry.Tag is int top ? top : 0) - offset);
            if (_typing.Visible)
                _typing.Location = new Point(0, (_typing.Tag is int top ? top : 0) - offset);
            ResumeLayout(false);
            Invalidate(); // scrollbar thumb
        }

        private void OnAnimationTick(object? sender, EventArgs e)
        {
            bool busy = false;
            foreach (var entry in _entries)
                busy |= entry.StepReveal(0.07f);

            if (_typing.Visible)
            {
                _typing.StepReveal(0.1f);
                _typing.Tick();
                busy = true;
            }

            if (Math.Abs(_scrollTarget - _scroll) > 0.5f)
            {
                _scroll += (_scrollTarget - _scroll) * 0.22f;
                PositionEntries();
                busy = true;
            }
            else if (_scroll != _scrollTarget)
            {
                _scroll = _scrollTarget;
                PositionEntries();
            }

            if (!busy) _anim.Stop();
        }

        private void ScrollBy(float pixels)
        {
            _scrollTarget = Math.Clamp(_scrollTarget + pixels, 0, MaxScroll);
            _anim.Start();
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL || !Visible || !IsHandleCreated || IsDisposed) return false;
            var p = PointToClient(Cursor.Position);
            if (!ClientRectangle.Contains(p)) return false;

            int delta = (short)(((long)m.WParam >> 16) & 0xFFFF);
            ScrollBy(-delta / 120f * (_theme?.F(54) ?? 54f));
            return true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            if (_contentHeight <= Height || _theme == null) return;

            // Thin scrollbar thumb on the right edge
            float ratio = Height / (float)_contentHeight;
            float thumbH = Math.Max(_theme.F(24), Height * ratio);
            float thumbY = MaxScroll <= 0 ? 0 : (Height - thumbH) * (_scroll / MaxScroll);
            var thumb = new RectangleF(Width - ScrollbarWidth + _theme.F(2), thumbY + 2, ScrollbarWidth - _theme.F(4), thumbH - 4);
            Shapes.Prepare(e.Graphics);
            using var path = Shapes.Rounded(thumb, thumb.Width / 2f);
            using var brush = new SolidBrush(Color.FromArgb(120, MessengerTheme.Border));
            e.Graphics.FillPath(brush, path);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Application.RemoveMessageFilter(this);
                _anim.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
