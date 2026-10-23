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

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth flicker-free double buffering
                return cp;
            }
        }

        public MainForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
            menu = new[] { lblNewShift, lblContinue, lblSettings, lblQuit };
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
            AudioManager.Instance.PlayAmbientLoop();
            pbBackground.Paint += (s, ev) => DrawVignetteOverlay(ev.Graphics, pbBackground.ClientRectangle);
        }

        private static void DrawVignetteOverlay(Graphics g, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;

            // 1. Subtle Analog CRT Vignette (shadowing the 4 corners of the dispatcher room)
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddRectangle(rect);
                using var pgb = new System.Drawing.Drawing2D.PathGradientBrush(path);
                pgb.CenterColor = Color.FromArgb(0, 0, 0, 0);
                pgb.SurroundColors = new[] { Color.FromArgb(170, 0, 0, 0) };
                pgb.FocusScales = new PointF(0.55f, 0.45f);
                g.FillRectangle(pgb, rect);
            }

            // 2. Subtle Analog Scanlines
            using var scanPen = new Pen(Color.FromArgb(12, 0, 0, 0), 1f);
            for (int y = 0; y < rect.Height; y += 4)
            {
                g.DrawLine(scanPen, 0, y, rect.Width, y);
            }
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
                case Keys.F1: OpenTutorialFlow(); break; // Open tutorial with F1
            }
        }

        // ---- Logic ------------------------------------------------------
        void Select(int index)
        {
            if (index >= 0 && index != selected)
            {
                AudioManager.Instance.PlayFrequencyBlip();
            }
            selected = index;
            for (int i = 0; i < menu.Length; i++)
                menu[i].ForeColor = i == selected ? HoverColor
                                  : i == menu.Length - 1 ? DimColor : NormalColor;
        }

        void Activate(int index)
        {
            switch (index)
            {
                case 0: // New Shift
                    StartNewShiftFlow();
                    break;

                case 1: // Continue
                    ContinueShiftFlow();
                    break;

                case 2: // Settings
                    OpenSettingsFlow();
                    break;

                case 3: // Quit
                    Application.Exit();
                    break;
            }
        }

        private void OpenSettingsFlow()
        {
            using var settingsForm = new Forms.SettingsForm();
            settingsForm.ShowDialog(this);
        }

        // Opens the tutorial form (automatically after creating a shift or via shortcut)
        private void OpenTutorialFlow()
        {
            this.Hide();
            using (var tutorial = new Tutorial())
            {
                tutorial.ShowDialog();
            }
            this.Show();
        }

        private void StartNewShiftFlow()
        {
            using var archiveForm = new Forms.OperatorArchiveForm(SaveManager.Instance, isNewShiftMode: true);
            if (archiveForm.ShowDialog(this) == DialogResult.OK && archiveForm.SelectedProfile != null)
            {
                LaunchGameWithProfile(archiveForm.SelectedProfile);
            }
        }

        private void ContinueShiftFlow()
        {
            if (!SaveManager.Instance.HasAnySaves())
            {
                MessageBox.Show(
                    this,
                    "NO ARCHIVED OPERATORS FOUND.\n\nPlease select 'NEW SHIFT' to begin your first night shift assignment.",
                    "Recovered Operator Archive",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var archiveForm = new Forms.OperatorArchiveForm(SaveManager.Instance, isNewShiftMode: false);
            if (archiveForm.ShowDialog(this) == DialogResult.OK && archiveForm.SelectedProfile != null)
            {
                LaunchGameWithProfile(archiveForm.SelectedProfile);
            }
        }

        private void LaunchGameWithProfile(Models.OperatorProfile profile)
        {
            var overlay = new Controls.OperatorVerificationOverlay(profile)
            {
                Dock = DockStyle.Fill
            };

            overlay.VerificationCompleted += () =>
            {
                if (pbBackground.Controls.Contains(overlay))
                {
                    pbBackground.Controls.Remove(overlay);
                }
                else if (this.Controls.Contains(overlay))
                {
                    this.Controls.Remove(overlay);
                }
                overlay.Dispose();
                OpenTutorialFlow();
            };

            pbBackground.Controls.Add(overlay);
            overlay.BringToFront();
            overlay.Focus();
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
            lblSettings.Font = menuFont;
            lblSettings.Location = new Point(blockX, lblContinue.Bottom + menuSpacing);

            lblQuit.Font = menuFont;
            lblQuit.Location = new Point(blockX, lblSettings.Bottom + menuSpacing);
        }

        private void lblKicker_Click(object sender, EventArgs e)
        {

        }
    }
}