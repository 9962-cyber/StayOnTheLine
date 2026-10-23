using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StayOnTheLine.Controls
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION & EXTENSION
    /// Custom-painted vintage analog slider control styled for 1990s civil defense terminal equipment.
    /// Replaces standard generic WinForms TrackBar with an authentic antique brass/ink finish.
    /// </summary>
    public class VintageTrackBar : Control
    {
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 80;
        private bool _isDragging = false;
        private bool _isHovered = false;

        public event EventHandler? ValueChanged;
        public event EventHandler? Scroll;

        [Category("Behavior")]
        [DefaultValue(0)]
        public int Minimum
        {
            get => _minimum;
            set
            {
                if (value >= _maximum) value = _maximum - 1;
                _minimum = value;
                if (_value < _minimum) Value = _minimum;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(100)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                if (value <= _minimum) value = _minimum + 1;
                _maximum = value;
                if (_value > _maximum) Value = _maximum;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(80)]
        public int Value
        {
            get => _value;
            set
            {
                int clamped = Math.Clamp(value, _minimum, _maximum);
                if (_value != clamped)
                {
                    _value = clamped;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public VintageTrackBar()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.Selectable,
                true);

            BackColor = Color.Transparent;
            Size = new Size(280, 36);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                Focus();
                UpdateValueFromPoint(e.X);
                Scroll?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging && e.Button == MouseButtons.Left)
            {
                UpdateValueFromPoint(e.X);
                Scroll?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isDragging)
            {
                _isDragging = false;
                Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int step = 2;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
            {
                Value -= step;
                Scroll?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up)
            {
                Value += step;
                Scroll?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                Value -= 10;
                Scroll?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                Value += 10;
                Scroll?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Home)
            {
                Value = _minimum;
                Scroll?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.End)
            {
                Value = _maximum;
                Scroll?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void UpdateValueFromPoint(int x)
        {
            int trackPadding = 14;
            int trackWidth = Width - (trackPadding * 2);
            if (trackWidth <= 0) return;

            float fraction = (float)(x - trackPadding) / trackWidth;
            fraction = Math.Clamp(fraction, 0f, 1f);
            Value = _minimum + (int)Math.Round(fraction * (_maximum - _minimum));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int trackPadding = 14;
            int trackWidth = Width - (trackPadding * 2);
            int centerY = Height / 2;
            int trackHeight = 4;
            int trackTop = centerY - (trackHeight / 2);

            // 1. Draw Tick Marks
            using (var tickPen = new Pen(Color.FromArgb(130, 110, 85), 1f))
            {
                int[] ticks = { 0, 25, 50, 75, 100 };
                foreach (int tick in ticks)
                {
                    float frac = (float)(tick - _minimum) / (_maximum - _minimum);
                    int tickX = trackPadding + (int)(frac * trackWidth);
                    g.DrawLine(tickPen, tickX, centerY + 8, tickX, centerY + 13);
                }
            }

            // 2. Draw Track Groove
            var trackRect = new Rectangle(trackPadding, trackTop, trackWidth, trackHeight);
            using (var trackBrush = new SolidBrush(Color.FromArgb(50, 38, 25)))
            {
                g.FillRectangle(trackBrush, trackRect);
            }
            using (var trackBorderPen = new Pen(Color.FromArgb(100, 82, 60), 1f))
            {
                g.DrawRectangle(trackBorderPen, trackRect);
            }

            // 3. Draw Active Track Fill
            float activeFrac = (_maximum == _minimum) ? 0f : (float)(_value - _minimum) / (_maximum - _minimum);
            int activeWidth = (int)(activeFrac * trackWidth);
            if (activeWidth > 0)
            {
                var activeRect = new Rectangle(trackPadding, trackTop, activeWidth, trackHeight);
                using var activeBrush = new LinearGradientBrush(
                    activeRect,
                    Color.FromArgb(145, 80, 42),
                    Color.FromArgb(185, 105, 55),
                    LinearGradientMode.Horizontal);
                g.FillRectangle(activeBrush, activeRect);
            }

            // 4. Draw Thumb Knob
            int thumbX = trackPadding + (int)(activeFrac * trackWidth);
            int thumbWidth = 14;
            int thumbHeight = 22;
            var thumbRect = new Rectangle(thumbX - (thumbWidth / 2), centerY - (thumbHeight / 2), thumbWidth, thumbHeight);

            // Subtle drop shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillRectangle(shadowBrush, thumbRect.X + 2, thumbRect.Y + 2, thumbRect.Width, thumbRect.Height);
            }

            // Brass thumb body gradient
            Color topColor = _isHovered || _isDragging ? Color.FromArgb(225, 198, 155) : Color.FromArgb(200, 175, 135);
            Color botColor = _isHovered || _isDragging ? Color.FromArgb(170, 140, 98) : Color.FromArgb(145, 118, 80);

            using (var thumbBrush = new LinearGradientBrush(thumbRect, topColor, botColor, LinearGradientMode.Vertical))
            {
                g.FillRectangle(thumbBrush, thumbRect);
            }

            // Dark ink border
            using (var thumbPen = new Pen(Color.FromArgb(42, 28, 16), 1.4f))
            {
                g.DrawRectangle(thumbPen, thumbRect);
            }

            // Vertical grip notches on thumb
            using (var notchPen = new Pen(Color.FromArgb(75, 55, 35), 1f))
            {
                g.DrawLine(notchPen, thumbRect.X + 4, thumbRect.Y + 4, thumbRect.X + 4, thumbRect.Bottom - 5);
                g.DrawLine(notchPen, thumbRect.X + 7, thumbRect.Y + 4, thumbRect.X + 7, thumbRect.Bottom - 5);
                g.DrawLine(notchPen, thumbRect.X + 10, thumbRect.Y + 4, thumbRect.X + 10, thumbRect.Bottom - 5);
            }
        }
    }
}
