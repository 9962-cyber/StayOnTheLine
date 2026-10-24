using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Controls.Messenger
{
    /// <summary>
    /// OOP Principle: ABSTRACTION
    /// Anything inside the messenger that redraws itself for a new scale/font set.
    /// </summary>
    public interface IThemedControl
    {
        void ApplyTheme(MessengerTheme theme);
    }

    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Palette and scaled fonts for the retro messenger. One instance per scale; the scene
    /// swaps in a new theme when the window size changes and disposes the old fonts.
    /// IM FELL is used for prose, Consolas for terminal/system text (see FontManager fallbacks).
    /// </summary>
    public sealed class MessengerTheme : IDisposable
    {
        // Window chrome
        public static readonly Color WindowBack = Color.FromArgb(236, 234, 227);
        public static readonly Color ThreadBack = Color.FromArgb(243, 242, 237);
        public static readonly Color PanelBack = Color.FromArgb(230, 228, 221);
        public static readonly Color Border = Color.FromArgb(166, 170, 176);
        public static readonly Color TitleTop = Color.FromArgb(108, 142, 182);
        public static readonly Color TitleBottom = Color.FromArgb(56, 84, 120);
        public static readonly Color HeaderPinkTop = Color.FromArgb(249, 224, 234);
        public static readonly Color HeaderPinkBottom = Color.FromArgb(234, 202, 217);

        // Bubbles: Stef's are dusty pink, the player's pale blue
        public static readonly Color IncomingFill = Color.FromArgb(243, 204, 221);
        public static readonly Color IncomingEdge = Color.FromArgb(210, 146, 176);
        public static readonly Color IncomingText = Color.FromArgb(70, 26, 42);
        public static readonly Color OutgoingFill = Color.FromArgb(213, 231, 246);
        public static readonly Color OutgoingEdge = Color.FromArgb(138, 180, 220);
        public static readonly Color OutgoingText = Color.FromArgb(22, 42, 68);

        // Text
        public static readonly Color Ink = Color.FromArgb(40, 40, 44);
        public static readonly Color MutedInk = Color.FromArgb(118, 118, 124);
        public static readonly Color SystemInk = Color.FromArgb(44, 96, 68);
        public static readonly Color AlertInk = Color.FromArgb(166, 26, 34);
        public static readonly Color Highlight = Color.FromArgb(207, 226, 246);
        public static readonly Color HighlightEdge = Color.FromArgb(82, 134, 196);
        public static readonly Color CrtInk = Color.FromArgb(196, 210, 186);

        public float Scale { get; }

        public Font Title { get; }
        public Font Body { get; }
        public Font BodyItalic { get; }
        public Font Caps { get; }
        public Font CapsLarge { get; }
        public Font Mono { get; }
        public Font MonoSmall { get; }

        public MessengerTheme(float scale)
        {
            Scale = Math.Clamp(scale, 0.35f, 4f);
            Title = FontManager.GetRegularFont(11.5f * Scale, FontStyle.Bold);
            Body = FontManager.GetRegularFont(11f * Scale);
            BodyItalic = FontManager.GetItalicFont(10.5f * Scale);
            Caps = FontManager.GetSmallCapsFont(8.5f * Scale);
            CapsLarge = FontManager.GetSmallCapsFont(14f * Scale);
            Mono = FontManager.GetMonospaceFont(8.5f * Scale);
            MonoSmall = FontManager.GetMonospaceFont(7f * Scale);
        }

        /// <summary>Scales a design-unit length to pixels.</summary>
        public int S(float units) => (int)Math.Round(units * Scale);

        public float F(float units) => units * Scale;

        /// <summary>
        /// Applies the theme children-first, so a container that lays out from its children's
        /// measurements (thread, response panel) sees their new fonts.
        /// </summary>
        public static void ApplyTo(Control root, MessengerTheme theme)
        {
            foreach (Control child in root.Controls)
                ApplyTo(child, theme);
            if (root is IThemedControl themed)
                themed.ApplyTheme(theme);
        }

        public void Dispose()
        {
            Title.Dispose();
            Body.Dispose();
            BodyItalic.Dispose();
            Caps.Dispose();
            CapsLarge.Dispose();
            Mono.Dispose();
            MonoSmall.Dispose();
        }
    }

    /// <summary>Shared drawing helpers for the messenger controls.</summary>
    internal static class Shapes
    {
        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Max(1f, Math.Min(radius * 2f, Math.Min(r.Width, r.Height)));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void Prepare(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }

        public static Color Fade(Color c, float alpha) =>
            Color.FromArgb(Math.Clamp((int)(c.A * alpha), 0, 255), c);

        public static float Ease(float t) => t * t * (3f - 2f * t);

        /// <summary>Generic contact silhouette (grey square, head and shoulders).</summary>
        public static void DrawAvatar(Graphics g, RectangleF r)
        {
            using (var bg = new SolidBrush(Color.FromArgb(150, 156, 166)))
                g.FillRectangle(bg, r);
            var state = g.Save();
            g.SetClip(r);
            using (var fg = new SolidBrush(Color.FromArgb(232, 232, 236)))
            {
                float head = r.Width * 0.36f;
                g.FillEllipse(fg, r.X + (r.Width - head) / 2f, r.Y + r.Height * 0.18f, head, head);
                g.FillEllipse(fg, r.X + r.Width * 0.16f, r.Y + r.Height * 0.62f, r.Width * 0.68f, r.Height * 0.7f);
            }
            g.Restore(state);
            using var edge = new Pen(Color.FromArgb(110, 116, 126));
            g.DrawRectangle(edge, r.X, r.Y, r.Width, r.Height);
        }

        public static readonly StringFormat Left = new() { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        public static readonly StringFormat Center = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        public static readonly StringFormat Right = new() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
    }
}
