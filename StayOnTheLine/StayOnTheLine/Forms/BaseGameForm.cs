namespace StayOnTheLine
{
    /// <summary>
    /// OOP Principle: INHERITANCE & POLYMORPHISM
    /// Base game form demonstrating inheritance from System.Windows.Forms.Form.
    /// Provides common horror game window settings and hardware compositing across all screens.
    /// </summary>
    public class BaseGameForm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth double-buffered compositing across all child controls
                return cp;
            }
        }

        public BaseGameForm()
        {
            BackColor = Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.CenterScreen;
        }
    }
}
