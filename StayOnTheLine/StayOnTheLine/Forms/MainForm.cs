using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using StayOnTheLine.Systems;

namespace StayOnTheLine
{
    public partial class MainForm : Form
    {
        // Normal / hover / dim colours for the menu labels
        static readonly Color NormalColor = Color.FromArgb(200, 198, 190);
        static readonly Color DimColor = Color.FromArgb(120, 118, 112);
        static readonly Color HoverColor = Color.White;

        Label[] menu;
        int selected = -1;

        public MainForm()
        {
            InitializeComponent();
            menu = new[] { lblNewShift, lblContinue, lblQuit };
            Resize += (s, e) => AdjustLayout();
        }

        void MainMenu_Load(object sender, EventArgs e)
        {
#if !DEBUG
            TopMost = true; // only in Release so debugging isn't annoying
#endif
            // Auto-load the background from Assets
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", "loadingandmainscreen_bg.png");
                if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "Assets", "background.png");
                if (File.Exists(path)) pbBackground.Image = Image.FromFile(path);
            }
            catch { }

            lblPlaceholder.Visible = pbBackground.Image == null;
            AdjustLayout();
        }

        // ---- Mouse ------------------------------------------------------
        void MenuItem_MouseEnter(object sender, EventArgs e) => Select(Array.IndexOf(menu, (Label)sender));
        void MenuItem_MouseLeave(object sender, EventArgs e) => Select(-1);
        void MenuItem_Click(object sender, EventArgs e) => Activate(Array.IndexOf(menu, (Label)sender));

        // ---- Keyboard ---------------------------------------------------
        void MainMenu_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down: Select((selected + 1) % menu.Length); break;
                case Keys.Up: Select((selected <= 0 ? menu.Length : selected) - 1); break;
                case Keys.Enter: if (selected >= 0) Activate(selected); break;
                case Keys.Escape: Application.Exit(); break;
            }
        }

        // ---- Logic ------------------------------------------------------
        void Select(int index)
        {
            selected = index;
            for (int i = 0; i < menu.Length; i++)
                menu[i].ForeColor = i == selected ? HoverColor
                                  : i == menu.Length - 1 ? DimColor : NormalColor;
        }

        void Activate(int index)
        {
            switch (index)
            {
                case 0: MessageBox.Show("TODO: start new shift"); break;
                case 1: MessageBox.Show("TODO: load saved shift"); break;
                case 2: Application.Exit(); break;
            }
        }

        private void pbBackground_Click(object sender, EventArgs e)
        {

        }

        private void lblTagline_Click(object sender, EventArgs e)
        {

        }

        private void lblTitle1_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Dynamically scales and positions all titles, headers, footers, and menu items
        /// to ensure a balanced, proportional layout across all screen resolutions (1080p, 864p, 1440p, 4K).
        /// </summary>
        private void AdjustLayout()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            float scaleX = (float)ClientSize.Width / 1920f;
            float scaleY = (float)ClientSize.Height / 1080f;
            float fontScale = Math.Min(scaleX, scaleY);

            // 1. Header controls
            int marginX = (int)(67 * scaleX);
            int headerY = (int)(75 * scaleY);

            lblHeaderLeft.Location = new Point(marginX, headerY);
            lblHeaderLeft.Font = new Font("IM FELL DW Pica", Math.Max(7.5f, 9f * fontScale), FontStyle.Regular);

            lblHeaderRight.Font = new Font("IM FELL DW Pica", Math.Max(7.5f, 9f * fontScale), FontStyle.Regular);
            lblHeaderRight.Width = (int)(400 * scaleX);
            lblHeaderRight.Location = new Point(ClientSize.Width - marginX - lblHeaderRight.Width, headerY);

            // 2. Footer controls
            int footerY = ClientSize.Height - (int)(75 * scaleY);

            lblFooterLeft.Font = new Font("IM FELL DW Pica", Math.Max(7.5f, 9f * fontScale), FontStyle.Regular);
            lblFooterLeft.Location = new Point(marginX, footerY);

            lblFooterRight.Font = new Font("IM FELL DW Pica", Math.Max(7.5f, 9f * fontScale), FontStyle.Regular);
            lblFooterRight.Width = (int)(400 * scaleX);
            lblFooterRight.Location = new Point(ClientSize.Width - marginX - lblFooterRight.Width, footerY);

            // 3. Title and Menu block on left side
            int blockX = (int)(180 * scaleX);

            // Kicker ("A NIGHT DISPATCH STORY")
            lblKicker.Font = new Font("IM FELL DW Pica", Math.Max(8f, 10f * fontScale), FontStyle.Regular);
            lblKicker.Location = new Point(blockX, (int)(130 * scaleY));

            // Title 1 ("Stay On")
            lblTitle1.Font = FontManager.GetSmallCapsFont(Math.Max(34f, 66f * fontScale), FontStyle.Bold);
            lblTitle1.Location = new Point(blockX - (int)(10 * scaleX), (int)(145 * scaleY));

            // Title 2 ("The Line")
            lblTitle2.Font = FontManager.GetItalicFont(Math.Max(36f, 70.2f * fontScale));
            lblTitle2.Location = new Point(blockX + (int)(15 * scaleX), lblTitle1.Bottom - (int)(15 * scaleY));

            // Tagline ("“There are eight calls on tonight’s list.”")
            lblTagline.Font = new Font("IM FELL DW Pica", Math.Max(10f, 16f * fontScale), FontStyle.Italic);
            lblTagline.Location = new Point(blockX, lblTitle2.Bottom + (int)(15 * scaleY));

            // Menu Items: New shift, Continue, Quit
            float menuFontSize = Math.Max(16f, 28f * fontScale);
            Font menuFont = new Font("IM FELL DW Pica", menuFontSize, FontStyle.Regular);

            int menuSpacing = (int)(18 * scaleY);
            int menuStartY = lblTagline.Bottom + (int)(32 * scaleY);

            lblNewShift.Font = menuFont;
            lblNewShift.Location = new Point(blockX, menuStartY);

            lblContinue.Font = menuFont;
            lblContinue.Location = new Point(blockX, lblNewShift.Bottom + menuSpacing);

            lblQuit.Font = menuFont;
            lblQuit.Location = new Point(blockX, lblContinue.Bottom + menuSpacing);
        }

        private void lblKicker_Click(object sender, EventArgs e)
        {

        }
    }
}