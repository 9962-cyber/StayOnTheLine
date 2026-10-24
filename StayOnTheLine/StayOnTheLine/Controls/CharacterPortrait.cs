using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StayOnTheLine.Controls
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// A character sprite that can stand in front of other controls (the messenger) without a
    /// box around it. WinForms has no real transparency between sibling controls, so the
    /// control's Region is cut to the sprite's silhouette: everything outside the character
    /// simply isn't part of the control. The sprite is tinted toward the monitor's glow and
    /// shaded at the bottom so it sits in the dark room.
    /// </summary>
    public sealed class CharacterPortrait : Control
    {
        private const byte AlphaThreshold = 128;

        private Image? _sprite;
        private Bitmap? _frame;

        public CharacterPortrait()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Selectable, false);
            BackColor = Color.Black;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Image? Sprite
        {
            get => _sprite;
            set
            {
                if (ReferenceEquals(_sprite, value)) return;
                _sprite = value;
                Rebuild();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Rebuild();
        }

        private void Rebuild()
        {
            _frame?.Dispose();
            _frame = null;

            if (_sprite == null || Width < 2 || Height < 2)
            {
                ReplaceRegion(null);
                Invalidate();
                return;
            }

            _frame = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(_frame))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Contain-fit, anchored to the bottom (feet below the frame like the story scenes)
                float k = Math.Min((float)Width / _sprite.Width, (float)Height / _sprite.Height);
                float w = _sprite.Width * k, h = _sprite.Height * k;
                g.DrawImage(_sprite, (Width - w) / 2f, Height - h, w, h);
            }

            ReplaceRegion(BuildSilhouette(_frame));

            // Tint + bottom shade (painted after the silhouette is taken, clipped by it on screen)
            using (var g = Graphics.FromImage(_frame))
            {
                using (var tint = new SolidBrush(Color.FromArgb(34, 30, 60, 52)))
                    g.FillRectangle(tint, 0, 0, Width, Height);
                var shadeRect = new Rectangle(0, (int)(Height * 0.62f), Width, Height - (int)(Height * 0.62f) + 1);
                using var shade = new LinearGradientBrush(shadeRect, Color.FromArgb(0, 0, 0, 0), Color.FromArgb(185, 0, 0, 0), 90f);
                g.FillRectangle(shade, shadeRect);
            }

            Invalidate();
        }

        private void ReplaceRegion(Region? region)
        {
            var old = Region;
            Region = region;
            old?.Dispose();
        }

        /// <summary>Run-length scan of the alpha channel into a region of opaque pixels.</summary>
        private static Region BuildSilhouette(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = Math.Abs(data.Stride);
                var row = new byte[stride];
                using var path = new GraphicsPath();
                for (int y = 0; y < bmp.Height; y++)
                {
                    Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, stride);
                    int x = 0;
                    while (x < bmp.Width)
                    {
                        while (x < bmp.Width && row[x * 4 + 3] < AlphaThreshold) x++;
                        int start = x;
                        while (x < bmp.Width && row[x * 4 + 3] >= AlphaThreshold) x++;
                        if (x > start) path.AddRectangle(new Rectangle(start, y, x - start, 1));
                    }
                }
                return new Region(path);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_frame != null) e.Graphics.DrawImageUnscaled(_frame, 0, 0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _frame?.Dispose();
            base.Dispose(disposing);
        }
    }
}
