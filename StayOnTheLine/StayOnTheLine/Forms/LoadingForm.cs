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
        private DesignScaler? _scaler;
        private bool _isTransitioning = false;

        // Set when this screen bridges two shifts instead of booting the game
        private readonly Action<LoadingForm>? _onProceed;

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
        /// Between-shifts loading screen: same look, titled with the next shift
        /// (e.g. "Shift 02" / "Escalation"). When the player proceeds, <paramref name="onProceed"/>
        /// opens the next screen; the caller closes this form once that screen is showing.
        /// </summary>
        public LoadingForm(string title, string subtitle, Action<LoadingForm> onProceed) : this()
        {
            lblTitleStayOn.Text = title;
            lblTitleTheLine.Text = subtitle;
            lblTitleTheLine.Visible = subtitle.Length > 0;
            lblSubtitle.Text = "N I G H T   D E S K   —   C L O C K I N G   I N";
            _onProceed = onProceed;
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
            lblTitleStayOn.Font = FontManager.GetSmallCapsFont(84F);
            lblSubtitle.Font = FontManager.GetSmallCapsFont(14F);
            lblStatus.Font = FontManager.GetSmallCapsFont(13F);
            lblPercentage.Font = FontManager.GetSmallCapsFont(11F);
            lblPrompt.Font = FontManager.GetSmallCapsFont(13F);
            lblTitleTheLine.Font = FontManager.GetItalicFont(76F);
        }


        private void LoadingForm_Load(object? sender, EventArgs e)
        {
            ApplyCustomFonts();
            _scaler = new DesignScaler(this);
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

            _scaler?.Apply(DesignScaler.FactorFor(ClientSize));
            float s = _scaler?.Factor ?? 1f;

            int centerX = ClientSize.Width / 2;
            int centerY = ClientSize.Height / 2;

            // Center Title Group
            int titleBaseY = Math.Max((int)(40 * s), centerY - (int)(300 * s));

            lblSubtitle.Left = centerX - (lblSubtitle.Width / 2);
            lblSubtitle.Top = titleBaseY;

            lblTitleStayOn.Left = centerX - (lblTitleStayOn.Width / 2);
            lblTitleStayOn.Top = titleBaseY + (int)(40 * s);

            lblTitleTheLine.Left = centerX - (lblTitleTheLine.Width / 2);
            lblTitleTheLine.Top = titleBaseY + (int)(185 * s);

            // Center Loading Section (approx 83% down the screen)
            int loadingBaseY = Math.Max(titleBaseY + (int)(340 * s), (int)(ClientSize.Height * 0.82));

            lblStatus.Left = centerX - (lblStatus.Width / 2);
            lblStatus.Top = loadingBaseY;

            pnlProgressBarBorder.Left = centerX - (pnlProgressBarBorder.Width / 2);
            pnlProgressBarBorder.Top = loadingBaseY + (int)(38 * s);

            lblPercentage.Left = centerX - (lblPercentage.Width / 2);
            lblPercentage.Top = loadingBaseY + (int)(58 * s);

            lblPrompt.Left = centerX - (lblPrompt.Width / 2);
            lblPrompt.Top = loadingBaseY + (int)(88 * s);
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
            if (e.KeyCode == Keys.Escape && _onProceed == null)
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

            if (_onProceed != null)
            {
                _onProceed(this);
                return;
            }

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
