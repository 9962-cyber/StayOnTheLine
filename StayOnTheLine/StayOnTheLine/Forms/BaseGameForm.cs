namespace StayOnTheLine
{
    /// <summary>
    /// OOP Principle: INHERITANCE
    /// Base game form demonstrating inheritance from System.Windows.Forms.Form.
    /// Provides common horror game window settings across all screens.
    /// </summary>
    public class BaseGameForm : Form
    {
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
