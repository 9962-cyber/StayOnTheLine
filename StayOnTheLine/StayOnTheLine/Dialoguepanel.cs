using System.Drawing;
using System.Windows.Forms;

namespace StayOnTheLine
{
    /// <summary>
    /// Translucent dialogue box with the thin double border seen in the reference screenshots.
    /// Drawing it ourselves avoids the flicker/artifacts of a Panel with an alpha BackColor.
    /// </summary>
    public class DialoguePanel : Panel
    {
        public DialoguePanel()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x04000000; // WS_CLIPSIBLINGS
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Dark translucent fill (the background art shows through a little)
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(200, 6, 6, 6)))
                g.FillRectangle(fill, 0, 0, Width, Height);

            // Double border: outer + inner thin lines
            using (Pen outer = new Pen(Color.FromArgb(210, 255, 255, 255), 1f))
            using (Pen inner = new Pen(Color.FromArgb(150, 255, 255, 255), 1f))
            {
                g.DrawRectangle(outer, 1, 1, Width - 3, Height - 3);
                g.DrawRectangle(inner, 5, 5, Width - 11, Height - 11);
            }

            base.OnPaint(e);
        }
    }
}