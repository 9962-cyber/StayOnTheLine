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
        private readonly Label lblContinueSub = new Label();
        private readonly System.Windows.Forms.Timer tmrRejectFlash = new System.Windows.Forms.Timer();

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

            lblContinueSub.AutoSize = true;
            lblContinueSub.BackColor = Color.Transparent;
            lblContinueSub.Click += (s, e) => { if (SaveManager.Instance.HasAnySaves()) Activate(1); else ContinueShiftFlow(); };
            lblContinueSub.MouseEnter += (s, e) => { if (SaveManager.Instance.HasAnySaves()) Select(1); };
            lblContinueSub.MouseLeave += (s, e) => Select(-1);
            pbBackground.Controls.Add(lblContinueSub);

            Resize += (s, e) => AdjustLayout();
            VisibleChanged += (s, e) => { if (Visible) RefreshContinueState(); };
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
            RefreshContinueState();
            AdjustLayout();
            AudioManager.Instance.StartMenuAmbience();
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

        public void RefreshContinueState()
        {
            bool hasSaves = SaveManager.Instance.HasAnySaves();
            if (hasSaves)
            {
                int? slot = SaveManager.Instance.GetMostRecentSlot();
                var profile = slot.HasValue ? SaveManager.Instance.GetProfile(slot.Value) : null;
                if (profile != null)
                {
                    lblContinue.Text = "CONTINUE";
                    lblContinue.ForeColor = (selected == 1) ? HoverColor : NormalColor;
                    lblContinue.Cursor = Cursors.Hand;

                    string timeStr = profile.LastPlayedTimestamp.ToString("hh:mm tt");
                    lblContinueSub.Text = $"▸ SHIFT {profile.CurrentShift:D2} — OP. {profile.OperatorName.ToUpperInvariant()} [{timeStr}]";
                    lblContinueSub.ForeColor = (selected == 1) ? Color.White : Color.FromArgb(140, 215, 160);
                    lblContinueSub.Cursor = Cursors.Hand;
                    return;
                }
            }

            lblContinue.Text = "CONTINUE";
            lblContinue.ForeColor = DimColor;
            lblContinue.Cursor = Cursors.Default;
            lblContinueSub.Text = "[ NO ARCHIVE FOUND ]";
            lblContinueSub.ForeColor = Color.FromArgb(100, 98, 92);
            lblContinueSub.Cursor = Cursors.Default;
        }

        // ---- Mouse ------------------------------------------------------
        void MenuItem_MouseEnter(object sender, EventArgs e) => Select(Array.IndexOf(menu, (Label)sender));
        void MenuItem_MouseLeave(object sender, EventArgs e) => Select(-1);
        void MenuItem_Click(object sender, EventArgs e) => Activate(Array.IndexOf(menu, (Label)sender));

        // ---- Keyboard ---------------------------------------------------
        void MainMenu_KeyDown(object sender, KeyEventArgs e)
        {
            bool hasSaves = SaveManager.Instance.HasAnySaves();
            switch (e.KeyCode)
            {
                case Keys.Down:
                    int next = (selected + 1) % menu.Length;
                    if (next == 1 && !hasSaves) next = (next + 1) % menu.Length;
                    Select(next);
                    break;
                case Keys.Up:
                    int prev = (selected <= 0 ? menu.Length : selected) - 1;
                    if (prev == 1 && !hasSaves) prev = (prev <= 0 ? menu.Length : prev) - 1;
                    Select(prev);
                    break;
                case Keys.Enter: if (selected >= 0) Activate(selected); break;
                case Keys.Escape: Application.Exit(); break;
                case Keys.F1: OpenTutorialFlow(); break; // Open tutorial with F1
                case Keys.F2: OpenArchiveFlow(); break; // Open multi-slot archive ledger with F2
                case Keys.F3: OpenStefSceneFlow(); break; // Open the Shift 3 Stef scene with F3
            }
        }

        // ---- Logic ------------------------------------------------------
        void Select(int index)
        {
            bool hasSaves = SaveManager.Instance.HasAnySaves();
            if (index >= 0 && index != selected)
            {
                if (index != 1 || hasSaves)
                {
                    AudioManager.Instance.PlayFrequencyBlip();
                }
            }
            selected = index;
            for (int i = 0; i < menu.Length; i++)
            {
                if (i == 1 && !hasSaves)
                {
                    menu[i].ForeColor = DimColor;
                }
                else
                {
                    menu[i].ForeColor = i == selected ? HoverColor
                                      : i == menu.Length - 1 ? DimColor : NormalColor;
                }
            }

            if (hasSaves)
            {
                lblContinueSub.ForeColor = (selected == 1) ? Color.White : Color.FromArgb(140, 215, 160);
            }
        }

        void Activate(int index)
        {
            AudioManager.Instance.PlayClick();
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

        // Opens the tutorial form with the verified operator's profile and smooth transition
        private void OpenTutorialFlow(Models.OperatorProfile? profile = null, Controls.OperatorVerificationOverlay? overlay = null, Action? onShown = null)
        {
            AudioManager.Instance.StopMenuAmbience();
            var tutorial = profile != null ? new Tutorial(profile) : new Tutorial("OPERATOR");
            bool continuingToShift = false;

            // Hook Shown so MainForm hides ONLY after Tutorial has completely painted its first frame
            tutorial.Shown += (s, e) =>
            {
                this.Hide();
                onShown?.Invoke();
                if (overlay != null)
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
                }
            };

            tutorial.OnCompletedTransition = () =>
            {
                continuingToShift = true;
                if (profile != null)
                {
                    if (profile.CurrentShift == 1)
                    {
                        profile.TutorialCompleted = true;
                    }
                    else if (profile.CurrentShift == 2)
                    {
                        if (!profile.AnomalyFlags.Contains("Shift2IntroSeen"))
                            profile.AnomalyFlags.Add("Shift2IntroSeen");
                    }
                    SaveManager.Instance.SaveProfile(profile);
                }

                // Seamless handoff: Open next shift, and only close/dispose tutorial when ShiftPlayForm.Shown fires!
                OpenShiftFlow(profile ?? new Models.OperatorProfile { OperatorName = "OPERATOR" }, onShown: () =>
                {
                    tutorial.Hide();
                    tutorial.Close();
                    tutorial.Dispose();
                });
            };

            tutorial.FormClosing += (s, e) =>
            {
                if (continuingToShift) return;

                this.Show();
                this.BringToFront();
                this.Activate();
                this.Update();
                AudioManager.Instance.StartMenuAmbience();
            };

            tutorial.FormClosed += (s, e) =>
            {
                tutorial.Dispose();
            };

            tutorial.Show();
            tutorial.BringToFront();
            tutorial.Activate();
            tutorial.Focus();
            tutorial.Update();
        }

        // Opens the Shift 3 records-terminal scene (Stef's messenger thread) for the most recent save.
        // Flags are applied to the profile in memory only; the shift flow is responsible for saving.
        private void OpenStefSceneFlow()
        {
            AudioManager.Instance.StopMenuAmbience();
            int? slot = SaveManager.Instance.GetMostRecentSlot();
            var profile = slot.HasValue ? SaveManager.Instance.GetProfile(slot.Value) : null;
            var scene = new Forms.StefSceneForm(profile);

            scene.Shown += (s, e) => this.Hide();
            scene.FormClosing += (s, e) =>
            {
                this.Show();
                this.BringToFront();
                this.Activate();
                this.Update();
                AudioManager.Instance.StartMenuAmbience();
            };
            scene.FormClosed += (s, e) =>
            {
                scene.Dispose();
            };
            scene.Show();
        }

        private void OpenShiftFlow(Models.OperatorProfile profile, Controls.OperatorVerificationOverlay? overlay = null, Action? onShown = null)
        {
            AudioManager.Instance.StopMenuAmbience();
            var scenario = Systems.ShiftScenarioFactory.Create(profile.CurrentShift);
            var shiftForm = new Forms.ShiftPlayForm(profile, scenario);
            bool continuingToNextShift = false;

            shiftForm.Shown += (s, e) =>
            {
                this.Hide();
                onShown?.Invoke();
                if (overlay != null)
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
                }
            };

            shiftForm.OnShiftTransitionRequested = (nextProfile) =>
            {
                continuingToNextShift = true;
                ShowShiftTransition(nextProfile, previousScreen: shiftForm);
            };

            // In case of closing back to Main Menu (e.g. Save & Quit), show MainForm *before* shiftForm
            // finishes closing, ensuring zero gap where the desktop is revealed.
            shiftForm.FormClosing += (s, e) =>
            {
                if (continuingToNextShift) return;

                this.Show();
                this.BringToFront();
                this.Activate();
                this.Update();
                AudioManager.Instance.StartMenuAmbience();
            };

            shiftForm.FormClosed += (s, e) =>
            {
                shiftForm.Dispose();
            };

            shiftForm.Show();
            shiftForm.BringToFront();
            shiftForm.Activate();
            shiftForm.Focus();
            shiftForm.Update();
        }

        /// <summary>
        /// Loading screen titled with the next shift (e.g. "SHIFT 2"), then that
        /// shift's intro scene if it hasn't been seen, otherwise the shift itself.
        /// Keeps the previous screen visible until LoadingForm has finished painting.
        /// </summary>
        private void ShowShiftTransition(Models.OperatorProfile profile, Form? previousScreen = null)
        {
            var loading = new LoadingForm(
                $"SHIFT {profile.CurrentShift}",
                string.Empty,
                screen =>
                {
                    bool needsIntro = profile.CurrentShift == 2 && !profile.AnomalyFlags.Contains("Shift2IntroSeen");
                    Action closeLoading = () =>
                    {
                        screen.Hide();
                        screen.Close();
                        screen.Dispose();
                    };

                    if (needsIntro)
                        OpenTutorialFlow(profile, onShown: closeLoading);
                    else
                        OpenShiftFlow(profile, onShown: closeLoading);
                });

            loading.Shown += (s, e) =>
            {
                if (previousScreen != null && !previousScreen.IsDisposed)
                {
                    previousScreen.Hide();
                    previousScreen.Close();
                    previousScreen.Dispose();
                }
            };

            loading.Show();
            loading.BringToFront();
            loading.Activate();
            loading.Focus();
            loading.Update();
        }

        private void StartNewShiftFlow()
        {
            using var archiveForm = new Forms.OperatorArchiveForm(SaveManager.Instance, isNewShiftMode: true);
            if (archiveForm.ShowDialog(this) == DialogResult.OK && archiveForm.SelectedProfile != null)
            {
                LaunchGameWithProfile(archiveForm.SelectedProfile);
            }
            RefreshContinueState();
        }

        private void ContinueShiftFlow()
        {
            if (!SaveManager.Instance.HasAnySaves())
            {
                AudioManager.Instance.PlayFrequencyBlip();
                lblContinueSub.Text = "⚠ NO ACTIVE ARCHIVE DETECTED // SELECT 'NEW SHIFT'";
                lblContinueSub.ForeColor = Color.FromArgb(255, 140, 100);

                tmrRejectFlash.Stop();
                tmrRejectFlash.Interval = 2000;
                tmrRejectFlash.Tick += (s, e) =>
                {
                    tmrRejectFlash.Stop();
                    RefreshContinueState();
                };
                tmrRejectFlash.Start();
                return;
            }

            int? slot = SaveManager.Instance.GetMostRecentSlot();
            var profile = slot.HasValue ? SaveManager.Instance.GetProfile(slot.Value) : null;
            if (profile != null)
            {
                LaunchGameWithProfile(profile);
            }
            else
            {
                OpenArchiveFlow();
            }
        }

        private void OpenArchiveFlow()
        {
            using var archiveForm = new Forms.OperatorArchiveForm(SaveManager.Instance, isNewShiftMode: false);
            if (archiveForm.ShowDialog(this) == DialogResult.OK && archiveForm.SelectedProfile != null)
            {
                LaunchGameWithProfile(archiveForm.SelectedProfile);
            }
            RefreshContinueState();
        }

        private void LaunchGameWithProfile(Models.OperatorProfile profile)
        {
            var overlay = new Controls.OperatorVerificationOverlay(profile)
            {
                Dock = DockStyle.Fill
            };

            overlay.VerificationCompleted += () =>
            {
                // Smooth transition: keep overlay (which is solid black) visible
                // until Tutorial or Shift is shown and painted, completely preventing any desktop or menu flicker!
                bool needsIntro = (profile.CurrentShift == 1 && !profile.TutorialCompleted) ||
                                  (profile.CurrentShift == 2 && !profile.AnomalyFlags.Contains("Shift2IntroSeen"));

                if (needsIntro)
                {
                    OpenTutorialFlow(profile, overlay);
                }
                else
                {
                    OpenShiftFlow(profile, overlay);
                }
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
            lblHeaderLeft.Font = new Font("IM FELL DW Pica SC", Math.Max(7.5f, 9f * fontScale), FontStyle.Regular);

            lblHeaderRight.Font = new Font("IM FELL DW Pica SC", Math.Max(7.5f, 9f * fontScale), FontStyle.Regular);
            lblHeaderRight.Width = (int)(400 * scaleX);
            lblHeaderRight.Location = new Point(ClientSize.Width - marginX - lblHeaderRight.Width, headerY);

            // 2. Footer controls
            int footerY = ClientSize.Height - (int)(75 * scaleY);

            lblFooterLeft.Font = new Font("IM FELL DW Pica SC", Math.Max(7.5f, 9f * fontScale), FontStyle.Regular);
            lblFooterLeft.Location = new Point(marginX, footerY);

            lblFooterRight.Font = new Font("IM FELL DW Pica SC", Math.Max(7.5f, 9f * fontScale), FontStyle.Regular);
            lblFooterRight.Width = (int)(400 * scaleX);
            lblFooterRight.Location = new Point(ClientSize.Width - marginX - lblFooterRight.Width, footerY);

            // 3. Title and Menu block on left side
            int blockX = (int)(180 * scaleX);

            // Kicker ("A NIGHT DISPATCH STORY")
            lblKicker.Font = new Font("IM FELL DW Pica SC", Math.Max(8f, 10f * fontScale), FontStyle.Regular);
            lblKicker.Location = new Point(blockX, (int)(130 * scaleY));

            // Title 1 ("Stay On")
            lblTitle1.Font = FontManager.GetSmallCapsFont(Math.Max(34f, 66f * fontScale), FontStyle.Bold);
            lblTitle1.Location = new Point(blockX - (int)(10 * scaleX), (int)(145 * scaleY));

            // Title 2 ("The Line")
            lblTitle2.Font = FontManager.GetItalicFont(Math.Max(36f, 70.2f * fontScale));
            lblTitle2.Location = new Point(blockX + (int)(15 * scaleX), lblTitle1.Bottom - (int)(15 * scaleY));

            // Tagline ("“There are eight calls on tonight’s list.”")
            lblTagline.Font = new Font("IM FELL DW Pica SC", Math.Max(10f, 16f * fontScale), FontStyle.Italic);
            lblTagline.Location = new Point(blockX, lblTitle2.Bottom + (int)(15 * scaleY));

            // Menu Items: New shift, Continue, Quit
            float menuFontSize = Math.Max(16f, 28f * fontScale);
            Font menuFont = new Font("IM FELL DW Pica SC", menuFontSize, FontStyle.Regular);

            int menuSpacing = (int)(18 * scaleY);
            int menuStartY = lblTagline.Bottom + (int)(32 * scaleY);

            lblNewShift.Font = menuFont;
            lblNewShift.Location = new Point(blockX, menuStartY);

            lblContinue.Font = menuFont;
            lblContinue.Location = new Point(blockX, lblNewShift.Bottom + menuSpacing);

            float subFontSize = Math.Max(9.5f, 13f * fontScale);
            lblContinueSub.Font = new Font("IM FELL DW Pica SC", subFontSize, FontStyle.Italic);
            int subMarginY = (int)(2 * scaleY);
            lblContinueSub.Location = new Point(blockX + (int)(4 * scaleX), lblContinue.Bottom + subMarginY);

            lblSettings.Font = menuFont;
            lblSettings.Location = new Point(blockX, lblContinueSub.Bottom + menuSpacing);

            lblQuit.Font = menuFont;
            lblQuit.Location = new Point(blockX, lblSettings.Bottom + menuSpacing);
        }

        private void lblKicker_Click(object sender, EventArgs e)
        {

        }
    }
}