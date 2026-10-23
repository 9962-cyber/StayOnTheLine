using StayOnTheLine.Models;
using StayOnTheLine.Systems;
using static StayOnTheLine.Systems.AudioManager;

namespace StayOnTheLine
{
    /// <summary>
    /// OOP Principles Demonstrated:
    /// 1. ENCAPSULATION: Delegates all progress calculation and font handling to 
    ///    specialized system classes (LoadingManager and FontManager).
    /// 2. ABSTRACTION: Communicates with LoadingManager through decoupled events
    ///    (ProgressChanged and LoadingCompleted) without knowing inner step details.
    /// 3. INHERITANCE: Inherits from System.Windows.Forms.Form.
    /// </summary>
    public partial class LoadingForm : Form
    {
        // Encapsulation: domain logic managed by specialized system classes
        private readonly LoadingManager _loadingManager = new();
        private bool _isTransitioning = false;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth double-buffered compositing
                return cp;
            }
        }

        public LoadingForm()
        {
            InitializeComponent();
            ApplyCustomFonts();
            SubscribeToLoadingEvents();
            lblPrompt.Click += (s, e) => HandleUserAction();
            lblPrompt.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Subscribes to events published by LoadingManager (Observer pattern / Decoupled Abstraction).
        /// </summary>
        private void SubscribeToLoadingEvents()
        {
            _loadingManager.ProgressChanged += OnLoadingProgressChanged;
            _loadingManager.LoadingCompleted += OnLoadingCompleted;
        }

        /// <summary>
        /// Applies custom TrueType fonts managed by FontManager.
        /// </summary>
        private void ApplyCustomFonts()
        {
            lblTitleStayOn.Font = FontManager.GetSmallCapsFont(62F);
            lblSubtitle.Font = FontManager.GetSmallCapsFont(12F);
            lblStatus.Font = FontManager.GetSmallCapsFont(13F);
            lblPercentage.Font = FontManager.GetSmallCapsFont(11F);
            lblPrompt.Font = FontManager.GetSmallCapsFont(13F);
            lblTitleTheLine.Font = FontManager.GetItalicFont(56F);
        }


        private void LoadingForm_Load(object? sender, EventArgs e)
        {
            ApplyCustomFonts();
            CenterControls();
            loadingTimer.Start();
            AudioManager.Instance.PlayAmbientLoop();
        }

        private void LoadingForm_Resize(object? sender, EventArgs e)
        {
            CenterControls();
        }

        /// <summary>
        /// Dynamically keeps all title and loading controls centered
        /// regardless of the display resolution (1080p, 1440p, 4K, etc.)
        /// </summary>
        private void CenterControls()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            int centerX = ClientSize.Width / 2;
            int centerY = ClientSize.Height / 2;

            // Center Title Group
            int titleBaseY = Math.Max(40, centerY - 250);

            lblSubtitle.Left = centerX - (lblSubtitle.Width / 2);
            lblSubtitle.Top = titleBaseY;

            lblTitleStayOn.Left = centerX - (lblTitleStayOn.Width / 2);
            lblTitleStayOn.Top = titleBaseY + 38;

            lblTitleTheLine.Left = centerX - (lblTitleTheLine.Width / 2);
            lblTitleTheLine.Top = titleBaseY + 148;

            // Center Loading Section (approx 83% down the screen)
            int loadingBaseY = Math.Max(titleBaseY + 290, (int)(ClientSize.Height * 0.82));

            lblStatus.Left = centerX - (lblStatus.Width / 2);
            lblStatus.Top = loadingBaseY;

            pnlProgressBarBorder.Left = centerX - (pnlProgressBarBorder.Width / 2);
            pnlProgressBarBorder.Top = loadingBaseY + 38;

            lblPercentage.Left = centerX - (lblPercentage.Width / 2);
            lblPercentage.Top = loadingBaseY + 58;

            lblPrompt.Left = centerX - (lblPrompt.Width / 2);
            lblPrompt.Top = loadingBaseY + 88;
        }

        private void LoadingTimer_Tick(object? sender, EventArgs e)
        {
            // Delegate simulation tick to LoadingManager
            _loadingManager.Tick();
        }

        /// <summary>
        /// Reacts to progress changes triggered by LoadingManager.
        /// </summary>
        private void OnLoadingProgressChanged(object? sender, LoadingProgressEventArgs e)
        {
            int intPercent = (int)e.OverallProgress;
            lblPercentage.Text = $"{intPercent}%";

            // Update Progress Bar fill width
            int totalBarWidth = pnlProgressBarBorder.Width;
            int fillWidth = (int)((e.OverallProgress / 100f) * totalBarWidth);
            pnlProgressBarFill.Width = Math.Clamp(fillWidth, 0, totalBarWidth);

            lblStatus.Text = e.StatusMessage;
        }

        /// <summary>
        /// Reacts to completion event triggered by LoadingManager.
        /// </summary>
        private void OnLoadingCompleted(object? sender, EventArgs e)
        {
            loadingTimer.Stop();

            lblStatus.Text = "SYSTEM ONLINE. ALL LINES ACTIVE.";
            lblStatus.ForeColor = Color.FromArgb(170, 220, 180);
            pnlProgressBarFill.BackColor = Color.FromArgb(170, 220, 180);

            lblPrompt.Visible = true;
            blinkTimer.Start();
        }

        private void BlinkTimer_Tick(object? sender, EventArgs e)
        {
            lblPrompt.Visible = !lblPrompt.Visible;
        }

        private void LoadingForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
                return;
            }

            HandleUserAction();
        }

        private void LoadingForm_Click(object? sender, EventArgs e)
        {
            HandleUserAction();
        }

        private void HandleUserAction()
        {
            AudioManager.Instance.PlayFrequencyBlip();
            if (!_loadingManager.IsCompleted)
            {
                // Speed up loading if player clicks or presses key during loading
                _loadingManager.FastForward();
                return;
            }

            ProceedToNextScreen();
        }

        private void ProceedToNextScreen()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            loadingTimer.Stop();
            blinkTimer.Stop();
            lblPrompt.Visible = false;

            // Pre-instantiate and display MainForm first so the display never reveals the desktop
            var mainForm = new MainForm();
            mainForm.FormClosed += (s, e) => this.Close();
            mainForm.Show();
            mainForm.BringToFront();
            mainForm.Activate();
            mainForm.Focus();
            mainForm.Update();

            // Seamlessly hide loading form now that MainForm is already visible
            this.Hide();
        }

        private void lblTitleStayOn_Click(object sender, EventArgs e)
        {
            HandleUserAction();
        }
    }
}
