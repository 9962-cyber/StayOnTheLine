using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using StayOnTheLine.Models;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principles Demonstrated:
    /// 1. INHERITANCE: Inherits from BaseShiftForm (which inherits from BaseGameForm).
    /// 2. POLYMORPHISM: Overrides lifecycle hooks (OnShiftStarted, OnCallSelected,
    ///    OnActionExecuted) to drive Shift 1's interactive training scenario.
    /// 3. ENCAPSULATION: Encapsulates dual-view state machine (DeskRoom vs ComputerTerminal),
    ///    CRT scanline rendering, teletype typewriter animation, panic/trust decay timers,
    ///    visual tutorial guidance system, and physical End-of-Shift clipboard modal.
    /// 4. ABSTRACTION: Decouples UI interactions from scenario scripts via IShiftScenario.
    /// </summary>
    public partial class ShiftPlayForm : BaseShiftForm
    {
        public enum ViewState
        {
            DeskRoom,
            ComputerTerminal
        }

        private ViewState _currentView = ViewState.DeskRoom;

        // Assets
        private Image? _imgDeskRoom;
        private Image? _imgComputerDesk;
        private Image? _imgShiftBriefingBook;
        private Image? _imgEndShiftBg;
        private Image? _imgEndShiftBoard;

        // Shift Scenario & Calls
        private readonly List<EmergencyCall> _calls;
        private int _activeCallIndex = 0;
        private EmergencyCall CurrentCall => _calls[Math.Clamp(_activeCallIndex, 0, _calls.Count - 1)];

        // Real-time Stop Watch / Timer
        private readonly System.Windows.Forms.Timer _shiftTimer = new();
        private DateTime _shiftStartTime = DateTime.Now;

        // Typewriter Animation
        private readonly System.Windows.Forms.Timer _typewriterTimer = new();
        private string _targetDialogue = string.Empty;
        private string _displayedDialogue = string.Empty;
        private int _typewriterCharIndex = 0;

        // Real-Time Panic Decay & Reassurance Cooldown Timers
        private readonly System.Windows.Forms.Timer _panicDecayTimer = new();
        private readonly System.Windows.Forms.Timer _reassuranceCooldownTimer = new();
        private int _reassuranceCooldownSeconds = 0;

        // Tutorial Visual Guidance System (Pulse & Highlights)
        private readonly System.Windows.Forms.Timer _tutorialPulseTimer = new();
        private bool _pulseHighlight = false;

        // Shift 2 Specific Dialogue & Discrepancy Tracking

        // UI Controls for Desk Room View
        private Panel _pnlDeskHeader = null!;
        private Label _lblDeskHeaderTitle = null!;
        private Label _lblDeskHotspot = null!;
        private Label _lblDeskBookHotspot = null!;
        private Label _btnDeskManual = null!;
        private Label _btnDeskSettings = null!;
        private Label _btnDeskEndShift = null!;
        private Panel? _pnlManualModal;

        // UI Controls for Computer Terminal View
        private Panel _pnlCrtScreen = null!;
        private Label _lblDeskStickyNote = null!;
        private Label _btnBackToDesk = null!;
        private Label _btnTerminalSettings = null!;
        private Label _btnTerminalEndShift = null!;
        private Panel? _pnlDebriefModal;

        // Controls inside CRT Screen
        private Label _lblCrtHeaderLeft = null!;
        private Label _lblCrtHeaderCenter = null!;
        private Label _lblCrtHeaderTimer = null!;
        private Label _lblProtocolChecklist = null!;
        private Label _lblGuideArrow = null!;

        // Queue list on left
        private Panel _pnlQueueContainer = null!;
        private Label _lblQueueHeader = null!;
        private readonly List<Panel> _queueCards = new();
        private Label _lblQueueFooter = null!;

        // Active Call Box (center)
        private Panel _pnlCallBox = null!;
        private Label _lblCallerName = null!;
        private Panel _pnlTranscriptScroll = null!;
        private Label _lblCallTranscript = null!;
        private Panel _pnlCallBottomBar = null!;
        private Label _lblCallSignalTag = null!;
        private Label _btnTranscriptScrollUp = null!;
        private Label _btnTranscriptScrollDown = null!;
        private int _transcriptScrollY = 0;
        private int _supervisorScrollY = 0;

        // Telemetry Row
        private Panel _pnlTelemetry = null!;
        private Label _lblLocation = null!;
        private Label _lblSignal = null!;
        private Label _lblTrust = null!;
        private Label _lblPanic = null!;

        // Dynamic Action Choices
        private class ActionChoice
        {
            public string Tag { get; set; } = string.Empty;
            public string Caption { get; set; } = string.Empty;
            public Color TagColor { get; set; } = Color.FromArgb(140, 205, 130);
            public Color BorderColor { get; set; } = Color.FromArgb(70, 120, 80);
            public Color HoverColor { get; set; } = Color.FromArgb(140, 255, 180);
            public Color BgColor { get; set; } = Color.FromArgb(200, 8, 18, 10);
            public Color HoverBgColor { get; set; } = Color.FromArgb(220, 16, 36, 20);
            public bool IsRecommended { get; set; } = false;
            public Action OnClick { get; set; } = () => { };
        }

        private readonly List<ActionChoice> _currentChoices = new();
        private readonly Label[] _actionButtons = new Label[6];

        // CRT text scales with the monitor so it never outgrows its boxes
        private readonly Dictionary<Control, Font> _crtBaseFonts = new();
        private float _crtFontScale = 1f;
        private float _deskFontScale = -1f;

        // Clue 01 Notification Banner & Supervisor Teletype Bar
        private Label _lblClueBanner = null!;
        private Panel _pnlSupervisorContainer = null!;
        private Panel _pnlSupervisorScroll = null!;
        private Label _lblSupervisorBar = null!;
        private Label _btnSupervisorScrollUp = null!;
        private Label _btnSupervisorScrollDown = null!;

        // Charmee speaks from the neighboring console, not through the CRT
        private Controls.CharacterDialogueBox _charmDialogue = null!;
        private readonly Dictionary<string, Image?> _charmPortraits = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _charmQueuedLines = new();   // lines in the current pop-up run
        private string _lastSystemBarText = "SYSTEM: ALL TRUNK LINES MONITORED.";

        // A response plays in script order: the CRT types the caller/system part, then Charmee
        // speaks in her pop-up, then the next CRT part continues, and so on.
        private sealed record ScriptSegment(string? CrtText, List<(string Speaker, string Text)>? CharmLines, bool Docked = false);
        private readonly Queue<ScriptSegment> _scriptSegments = new();
        private bool _freshTranscriptPending;

        // Who answers queue-misclicks: Krisha trains you in Shift 1; afterwards the switchboard does
        private string QueueAdvisor => Scenario.ShiftNumber == 1 ? "KRISHA" : "SYSTEM";

        public ShiftPlayForm(OperatorProfile profile, IShiftScenario scenario) : base(profile, scenario)
        {
            _calls = Scenario.GenerateCalls();
            LoadAssets();
            _charmDialogue = new Controls.CharacterDialogueBox();
            InitializeCustomUI();
            Controls.Add(_charmDialogue);
            _charmDialogue.Finished += (s, e) => PlayNextSegment();
            _charmDialogue.VisibleChanged += (s, e) =>
            {
                // While Charmee talks, the room scene replaces the desk / terminal; restore it afterwards
                bool scene = _charmDialogue.Visible && !_charmDialogue.Docked;
                bool reportOpen = _pnlDebriefModal != null && _pnlDebriefModal.Visible;
                bool isTerminal = !scene && !reportOpen && _currentView == ViewState.ComputerTerminal;
                bool isDesk = !scene && !reportOpen && _currentView == ViewState.DeskRoom;

                _pnlCrtScreen.Visible = isTerminal;
                _btnBackToDesk.Visible = isTerminal;
                _btnTerminalSettings.Visible = isTerminal;
                _btnTerminalEndShift.Visible = isTerminal;
                if (_lblDeskStickyNote != null) _lblDeskStickyNote.Visible = isTerminal && !_charmDialogue.Visible;
                _pnlDeskHeader.Visible = isDesk;
                _lblDeskHotspot.Visible = isDesk;
                if (_lblDeskBookHotspot != null) _lblDeskBookHotspot.Visible = isDesk;
            };
            _lblSupervisorBar.TextChanged += (s, e) => RouteCharmHint();
            ApplyResponsiveLayout();

            Resize += (s, e) => ApplyResponsiveLayout();
        }

        public ShiftPlayForm(OperatorProfile profile) : this(profile, ShiftScenarioFactory.Create(profile.CurrentShift))
        {
        }

        private static Image? TryLoadImage(string fileName)
        {
            var searchNames = new List<string> { fileName };
            if (!Path.HasExtension(fileName))
            {
                searchNames.Add(fileName + ".png");
                searchNames.Add(fileName + ".jpg");
            }

            var baseDirs = new[]
            {
                Application.StartupPath,
                AppDomain.CurrentDomain.BaseDirectory,
                Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", "..")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..")),
                Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..")),
                Directory.GetCurrentDirectory()
            };

            foreach (string baseDir in baseDirs)
            {
                if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir)) continue;

                foreach (string name in searchNames)
                {
                    string pathInAssets = Path.Combine(baseDir, "Assets", name);
                    if (File.Exists(pathInAssets))
                    {
                        try { return Image.FromFile(pathInAssets); } catch { }
                    }

                    string pathDirect = Path.Combine(baseDir, name);
                    if (File.Exists(pathDirect))
                    {
                        try { return Image.FromFile(pathDirect); } catch { }
                    }
                }
            }
            return null;
        }

        private void LoadAssets()
        {
            // 1. Desk Room Background
            _imgDeskRoom = TryLoadImage("maingameplay_bg.png");

            // 2. Computer Terminal Mockup Background (Prefer user asset computerscreenforcall.png)
            _imgComputerDesk = TryLoadImage("computerscreenforcall.png") ?? TryLoadImage("computer_desk_ui.jpg");

            // 3. Shift Directive / Briefing Manual
            _imgShiftBriefingBook = TryLoadImage("shiftbriefing_book.png");

            // 4. End of Shift Background & Photorealistic Clipboard Board
            _imgEndShiftBg = TryLoadImage("endshiftsummary_bg.png");
            _imgEndShiftBoard = TryLoadImage("endshiftboard.png");
        }

        private void InitializeCustomUI()
        {
            // -------------------------------------------------------------
            // 1. DESK ROOM VIEW CONTROLS
            // -------------------------------------------------------------
            _pnlDeskHeader = new Panel
            {
                BackColor = Color.FromArgb(200, 10, 14, 10),
                Dock = DockStyle.Top,
                Height = 60
            };

            _lblDeskHeaderTitle = new Label
            {
                Text = $"NIGHT DESK  //  TERMINAL 04   ·   OPERATOR: {Profile.OperatorName.ToUpperInvariant()}   ·   {Scenario.ShiftTitle}",
                ForeColor = Color.FromArgb(220, 235, 215),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(15f, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0),
                AutoEllipsis = true
            };

            _btnDeskSettings = new Label
            {
                Text = "[ SETTINGS / ESC ]",
                ForeColor = Color.FromArgb(180, 195, 175),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(13f, FontStyle.Regular),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 180,
                Cursor = Cursors.Hand
            };
            _btnDeskSettings.Click += (s, e) => OpenInGameSettings();
            _btnDeskSettings.MouseEnter += (s, e) => _btnDeskSettings.ForeColor = Color.White;
            _btnDeskSettings.MouseLeave += (s, e) => _btnDeskSettings.ForeColor = Color.FromArgb(180, 195, 175);

            _btnDeskManual = new Label
            {
                Text = "[ 📖 DIRECTIVES / RULES ]",
                ForeColor = Color.FromArgb(235, 215, 175),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(13f, FontStyle.Bold),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 210,
                Cursor = Cursors.Hand
            };
            _btnDeskManual.Click += (s, e) => ShowDirectiveManualModal();
            _btnDeskManual.MouseEnter += (s, e) => _btnDeskManual.ForeColor = Color.White;
            _btnDeskManual.MouseLeave += (s, e) => _btnDeskManual.ForeColor = Color.FromArgb(235, 215, 175);

            _btnDeskEndShift = new Label
            {
                Text = "[ SUBMIT INCIDENT LOGS ]",
                ForeColor = Color.FromArgb(140, 255, 170),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(13f, FontStyle.Bold),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 240,
                Cursor = Cursors.Hand
            };
            _btnDeskEndShift.Click += (s, e) => ShowDebriefModal();
            _btnDeskEndShift.MouseEnter += (s, e) => _btnDeskEndShift.ForeColor = Color.White;
            _btnDeskEndShift.MouseLeave += (s, e) => _btnDeskEndShift.ForeColor = Color.FromArgb(140, 255, 170);

            _pnlDeskHeader.Controls.Add(_lblDeskHeaderTitle);
            _pnlDeskHeader.Controls.Add(_btnDeskEndShift);
            _pnlDeskHeader.Controls.Add(_btnDeskManual);
            _pnlDeskHeader.Controls.Add(_btnDeskSettings);
            Controls.Add(_pnlDeskHeader);

            // Clickable Hotspot on Computer Monitor in Desk View
            _lblDeskHotspot = new Label
            {
                Text = "▶  CLICK TO ACCESS DISPATCH COMPUTER TERMINAL  ◀\n[ INCOMING EMERGENCY TRUNK LINES ACTIVE ]",
                ForeColor = Color.FromArgb(120, 255, 160),
                BackColor = Color.FromArgb(190, 8, 16, 10),
                Font = FontManager.GetSmallCapsFont(14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };
            _lblDeskHotspot.Click += (s, e) => SwitchView(ViewState.ComputerTerminal);
            _lblDeskHotspot.MouseEnter += (s, e) =>
            {
                _lblDeskHotspot.BackColor = Color.FromArgb(230, 16, 32, 20);
                _lblDeskHotspot.ForeColor = Color.FromArgb(200, 255, 220);
                AudioManager.Instance.PlayFrequencyBlip();
            };
            _lblDeskHotspot.MouseLeave += (s, e) =>
            {
                _lblDeskHotspot.BackColor = Color.FromArgb(190, 8, 16, 10);
                _lblDeskHotspot.ForeColor = Color.FromArgb(120, 255, 160);
            };
            Controls.Add(_lblDeskHotspot);

            // Clickable Hotspot for Directive Binder / 10-Codes on Desk
            _lblDeskBookHotspot = new Label
            {
                Text = "📖  INSPECT STATION DIRECTIVE BINDER & 10-CODES",
                ForeColor = Color.FromArgb(225, 210, 175),
                BackColor = Color.FromArgb(190, 26, 20, 14),
                Font = FontManager.GetSmallCapsFont(12.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };
            _lblDeskBookHotspot.Click += (s, e) => ShowDirectiveManualModal();
            _lblDeskBookHotspot.MouseEnter += (s, e) =>
            {
                _lblDeskBookHotspot.BackColor = Color.FromArgb(230, 48, 38, 22);
                _lblDeskBookHotspot.ForeColor = Color.FromArgb(255, 245, 215);
                AudioManager.Instance.PlayPaperRustle();
            };
            _lblDeskBookHotspot.MouseLeave += (s, e) =>
            {
                _lblDeskBookHotspot.BackColor = Color.FromArgb(190, 26, 20, 14);
                _lblDeskBookHotspot.ForeColor = Color.FromArgb(225, 210, 175);
            };
            Controls.Add(_lblDeskBookHotspot);

            // Tactile Sticky Note on Computer Desk below Monitor (matching computerscreenforcall.png)
            _lblDeskStickyNote = new Label
            {
                Text = "LINE 08\nDO NOT ANSWER",
                ForeColor = Color.FromArgb(36, 30, 18),
                BackColor = Color.FromArgb(230, 235, 215, 135),
                Font = FontManager.GetSmallCapsFont(8.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            _lblDeskStickyNote.Click += (s, e) =>
            {
                AudioManager.Instance.PlayPaperRustle();
                ParchmentNoticeDialog.ShowNotice(
                    this,
                    "HANDWRITTEN NOTE // TAPED TO CONSOLE",
                    "Line 08 — Do not answer.",
                    "\"Do not answer. Do not trace. That channel has been dead for twenty years.\"\n\n— Shift Supervisor Krisha Narciso",
                    "PUT IT BACK");
            };
            Controls.Add(_lblDeskStickyNote);

            // -------------------------------------------------------------
            // 2. COMPUTER TERMINAL VIEW CONTROLS
            // -------------------------------------------------------------
            _btnBackToDesk = new Label
            {
                Text = "< Back",
                ForeColor = Color.FromArgb(220, 225, 215),
                BackColor = Color.FromArgb(180, 36, 40, 36),
                Font = FontManager.GetSmallCapsFont(13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(96, 32),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnBackToDesk.Click += (s, e) => SwitchView(ViewState.DeskRoom);
            _btnBackToDesk.MouseEnter += (s, e) =>
            {
                _btnBackToDesk.BackColor = Color.FromArgb(220, 56, 62, 54);
                AudioManager.Instance.PlayKeyClick();
            };
            _btnBackToDesk.MouseLeave += (s, e) => _btnBackToDesk.BackColor = Color.FromArgb(180, 36, 40, 36);
            Controls.Add(_btnBackToDesk);

            _btnTerminalSettings = new Label
            {
                Text = "⚙ Settings",
                ForeColor = Color.FromArgb(190, 200, 190),
                BackColor = Color.FromArgb(180, 36, 40, 36),
                Font = FontManager.GetSmallCapsFont(12f, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(110, 32),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnTerminalSettings.Click += (s, e) => OpenInGameSettings();
            Controls.Add(_btnTerminalSettings);

            _btnTerminalEndShift = new Label
            {
                Text = "✓ End Shift",
                ForeColor = Color.FromArgb(140, 255, 170),
                BackColor = Color.FromArgb(180, 26, 48, 28),
                Font = FontManager.GetSmallCapsFont(12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(116, 32),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnTerminalEndShift.Click += (s, e) => ShowDebriefModal();
            _btnTerminalEndShift.MouseEnter += (s, e) => _btnTerminalEndShift.BackColor = Color.FromArgb(220, 36, 68, 38);
            _btnTerminalEndShift.MouseLeave += (s, e) => _btnTerminalEndShift.BackColor = Color.FromArgb(180, 26, 48, 28);
            Controls.Add(_btnTerminalEndShift);

            // The CRT Screen Canvas
            _pnlCrtScreen = new Panel
            {
                BackColor = Color.FromArgb(240, 6, 14, 8),
                Visible = false
            };
            _pnlCrtScreen.Paint += PnlCrtScreen_Paint;
            Controls.Add(_pnlCrtScreen);

            // Header labels inside CRT
            _lblCrtHeaderLeft = new Label
            {
                Text = $"NIGHT DESK // SHIFT 0{Scenario.ShiftNumber}",
                ForeColor = Color.FromArgb(170, 225, 160),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(13f, FontStyle.Bold),
                AutoSize = true
            };
            _lblCrtHeaderCenter = new Label
            {
                Text = "CALL 01 OF 04",
                ForeColor = Color.FromArgb(170, 225, 160),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(13f, FontStyle.Bold),
                AutoSize = true
            };
            _lblCrtHeaderTimer = new Label
            {
                Text = "00:00:00",
                ForeColor = Color.FromArgb(170, 225, 160),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(13f, FontStyle.Bold),
                AutoSize = true
            };

            _pnlCrtScreen.Controls.Add(_lblCrtHeaderLeft);
            _pnlCrtScreen.Controls.Add(_lblCrtHeaderCenter);
            _pnlCrtScreen.Controls.Add(_lblCrtHeaderTimer);

            // Tutorial / Operational Protocol Checklist Strip (Across CRT under Header)
            _lblProtocolChecklist = new Label
            {
                Text = Scenario.HasTutorialGuidance
                    ? "[✓] 1. Select Line   [►] 2. Open Audio   [ ] 3. Identify Caller   [ ] 4. Check Archive   [ ] 5. Resolve"
                    : $"ALL FREQUENCIES ACTIVE  //  DISPATCH PROTOCOL: STANDARD  //  LINE ANOMALIES: 0{State.CluesFound}",
                ForeColor = Color.FromArgb(170, 235, 160),
                BackColor = Color.FromArgb(200, 4, 10, 6),
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontManager.GetSmallCapsFont(10.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlCrtScreen.Controls.Add(_lblProtocolChecklist);

            // Guide Arrow Label (Pulsing Phosphor Marker)
            _lblGuideArrow = new Label
            {
                Text = "◄── [ACTION]",
                ForeColor = Color.FromArgb(255, 225, 120),
                BackColor = Color.FromArgb(220, 16, 24, 12),
                Font = FontManager.GetSmallCapsFont(10.5f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = true,
                Visible = false,
                Padding = new Padding(6, 4, 6, 4)
            };
            _pnlCrtScreen.Controls.Add(_lblGuideArrow);

            // Left Queue Panel
            _pnlQueueContainer = new Panel
            {
                BackColor = Color.FromArgb(150, 4, 10, 6),
                BorderStyle = BorderStyle.FixedSingle
            };
            _lblQueueHeader = new Label
            {
                Text = "CALLER QUEUE       04",
                ForeColor = Color.FromArgb(160, 215, 150),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlQueueContainer.Controls.Add(_lblQueueHeader);

            _lblQueueFooter = new Label
            {
                Text = "ACTIVE   01 / 04",
                ForeColor = Color.FromArgb(120, 165, 110),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(10f, FontStyle.Regular),
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlQueueContainer.Controls.Add(_lblQueueFooter);

            BuildQueueCards();
            _pnlCrtScreen.Controls.Add(_pnlQueueContainer);

            // Active Call Box (Dialogue Transcript)
            _pnlCallBox = new Panel
            {
                BackColor = Color.FromArgb(180, 5, 12, 7),
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblCallerName = new Label
            {
                Text = "▸ [ UNIDENTIFIED CALLER ]",
                ForeColor = Color.FromArgb(140, 205, 130),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(12, 6, 0, 0)
            };

            _pnlCallBottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = Color.Transparent
            };

            _lblCallSignalTag = new Label
            {
                Text = "[ SIGNAL STABLE ]",
                ForeColor = Color.FromArgb(130, 200, 120),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Regular),
                Dock = DockStyle.Left,
                Width = 220,
                Padding = new Padding(12, 0, 0, 4)
            };

            _btnTranscriptScrollDown = new Label
            {
                Text = "▼ SCROLL",
                ForeColor = Color.FromArgb(130, 200, 120),
                BackColor = Color.FromArgb(180, 10, 24, 14),
                Font = FontManager.GetSmallCapsFont(8.5f, FontStyle.Bold),
                Dock = DockStyle.Right,
                Width = 76,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnTranscriptScrollDown.Click += (s, e) => ScrollTranscript(40);
            _btnTranscriptScrollDown.MouseEnter += (s, e) => _btnTranscriptScrollDown.ForeColor = Color.White;
            _btnTranscriptScrollDown.MouseLeave += (s, e) => _btnTranscriptScrollDown.ForeColor = Color.FromArgb(130, 200, 120);

            _btnTranscriptScrollUp = new Label
            {
                Text = "▲ SCROLL",
                ForeColor = Color.FromArgb(130, 200, 120),
                BackColor = Color.FromArgb(180, 10, 24, 14),
                Font = FontManager.GetSmallCapsFont(8.5f, FontStyle.Bold),
                Dock = DockStyle.Right,
                Width = 76,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnTranscriptScrollUp.Click += (s, e) => ScrollTranscript(-40);
            _btnTranscriptScrollUp.MouseEnter += (s, e) => _btnTranscriptScrollUp.ForeColor = Color.White;
            _btnTranscriptScrollUp.MouseLeave += (s, e) => _btnTranscriptScrollUp.ForeColor = Color.FromArgb(130, 200, 120);

            _pnlCallBottomBar.Controls.Add(_btnTranscriptScrollUp);
            _pnlCallBottomBar.Controls.Add(_btnTranscriptScrollDown);
            _pnlCallBottomBar.Controls.Add(_lblCallSignalTag);

            _pnlTranscriptScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            _lblCallTranscript = new Label
            {
                Text = string.Empty,
                ForeColor = Color.FromArgb(245, 245, 235),
                BackColor = Color.Transparent,
                Font = FontManager.GetItalicFont(14.5f),
                AutoSize = true,
                Location = new Point(14, 8),
                Padding = new Padding(0)
            };

            _pnlTranscriptScroll.Controls.Add(_lblCallTranscript);
            _pnlCallBox.Controls.Add(_pnlTranscriptScroll);
            _pnlCallBox.Controls.Add(_lblCallerName);
            _pnlCallBox.Controls.Add(_pnlCallBottomBar);
            _pnlTranscriptScroll.BringToFront();
            _pnlCrtScreen.Controls.Add(_pnlCallBox);

            _pnlCallBox.MouseWheel += OnTranscriptMouseWheel;
            _pnlTranscriptScroll.MouseWheel += OnTranscriptMouseWheel;
            _lblCallTranscript.MouseWheel += OnTranscriptMouseWheel;

            // Telemetry Row (Location / Signal / Trust / Panic)
            _pnlTelemetry = new Panel
            {
                BackColor = Color.Transparent
            };
            _lblLocation = new Label
            {
                Text = "LOCATION: [ UNCONFIRMED / TRACING ]",
                ForeColor = Color.FromArgb(160, 215, 150),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Regular),
                AutoSize = true
            };
            _lblSignal = new Label
            {
                Text = "SIGNAL: STABLE",
                ForeColor = Color.FromArgb(160, 215, 150),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Regular),
                AutoSize = true
            };
            _lblTrust = new Label
            {
                Text = "TRUST: 80%",
                ForeColor = Color.FromArgb(160, 215, 150),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Regular),
                AutoSize = true
            };
            _lblPanic = new Label
            {
                Text = "PANIC: 25%",
                ForeColor = Color.FromArgb(255, 180, 120),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Regular),
                AutoSize = true
            };
            _pnlTelemetry.Controls.AddRange(new Control[] { _lblLocation, _lblSignal, _lblTrust, _lblPanic });
            _pnlCrtScreen.Controls.Add(_pnlTelemetry);

            // Clue Notification Banner across CRT screen
            _lblClueBanner = new Label
            {
                Text = "⭐ EVIDENCE FLAGGED: CLUE 01 — BELL STREET TIMESTAMP PARADOX",
                ForeColor = Color.FromArgb(240, 190, 255),
                BackColor = Color.FromArgb(230, 38, 12, 46),
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            _pnlCrtScreen.Controls.Add(_lblClueBanner);

            // The 6 Dynamic Action Choice Buttons
            for (int i = 0; i < 6; i++)
            {
                var btn = MakeActionButton();
                _actionButtons[i] = btn;
                _pnlCrtScreen.Controls.Add(btn);
            }

            // Supervisor Teletype Container & Bar
            _pnlSupervisorContainer = new Panel
            {
                BackColor = Color.FromArgb(200, 3, 8, 4),
                BorderStyle = BorderStyle.FixedSingle
            };

            var pnlSupervisorButtons = new Panel
            {
                Dock = DockStyle.Right,
                Width = 26,
                BackColor = Color.Transparent
            };

            _btnSupervisorScrollUp = new Label
            {
                Text = "▲",
                ForeColor = Color.FromArgb(140, 240, 160),
                BackColor = Color.FromArgb(160, 6, 16, 8),
                Font = FontManager.GetSmallCapsFont(8f, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnSupervisorScrollUp.Click += (s, e) => ScrollSupervisor(-30);
            _btnSupervisorScrollUp.MouseEnter += (s, e) => _btnSupervisorScrollUp.ForeColor = Color.White;
            _btnSupervisorScrollUp.MouseLeave += (s, e) => _btnSupervisorScrollUp.ForeColor = Color.FromArgb(140, 240, 160);

            _btnSupervisorScrollDown = new Label
            {
                Text = "▼",
                ForeColor = Color.FromArgb(140, 240, 160),
                BackColor = Color.FromArgb(160, 6, 16, 8),
                Font = FontManager.GetSmallCapsFont(8f, FontStyle.Bold),
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnSupervisorScrollDown.Click += (s, e) => ScrollSupervisor(30);
            _btnSupervisorScrollDown.MouseEnter += (s, e) => _btnSupervisorScrollDown.ForeColor = Color.White;
            _btnSupervisorScrollDown.MouseLeave += (s, e) => _btnSupervisorScrollDown.ForeColor = Color.FromArgb(140, 240, 160);

            pnlSupervisorButtons.Controls.Add(_btnSupervisorScrollUp);
            pnlSupervisorButtons.Controls.Add(_btnSupervisorScrollDown);

            _pnlSupervisorScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            _lblSupervisorBar = new Label
            {
                Text = "KRISHA: Rule number one trainee: Always verify the caller's name and exact address before taking action.",
                ForeColor = Color.FromArgb(140, 240, 160),
                BackColor = Color.Transparent,
                Font = FontManager.GetItalicFont(12f),
                AutoSize = true,
                Location = new Point(10, 6),
                Padding = new Padding(0)
            };
            _lblSupervisorBar.TextChanged += (s, e) =>
            {
                _supervisorScrollY = 0;
                _lblSupervisorBar.Location = new Point(10, 6);
            };

            _pnlSupervisorScroll.Controls.Add(_lblSupervisorBar);
            _pnlSupervisorContainer.Controls.Add(_pnlSupervisorScroll);
            _pnlSupervisorContainer.Controls.Add(pnlSupervisorButtons);
            _pnlSupervisorScroll.BringToFront();
            _pnlCrtScreen.Controls.Add(_pnlSupervisorContainer);

            _pnlSupervisorContainer.MouseWheel += OnSupervisorMouseWheel;
            _pnlSupervisorScroll.MouseWheel += OnSupervisorMouseWheel;
            _lblSupervisorBar.MouseWheel += OnSupervisorMouseWheel;

            // Timers
            _shiftTimer.Interval = 50;
            _shiftTimer.Tick += (s, e) => UpdateStopwatch();
            _shiftTimer.Start();

            _typewriterTimer.Interval = Math.Max(1, SettingsManager.Instance.Current.GetDelayPerCharacterMs());
            _typewriterTimer.Tick += (s, e) => UpdateTypewriter();

            // Panic Decay Timer (+4% every 2s during Call 2 danger)
            _panicDecayTimer.Interval = 2000;
            _panicDecayTimer.Tick += (s, e) => UpdatePanicDecay();
            _panicDecayTimer.Start();

            // Reassurance 15-second Cooldown Timer
            _reassuranceCooldownTimer.Interval = 1000;
            _reassuranceCooldownTimer.Tick += (s, e) =>
            {
                if (_reassuranceCooldownSeconds > 0)
                {
                    _reassuranceCooldownSeconds--;
                    UpdateDynamicChoices();
                }
                else
                {
                    _reassuranceCooldownTimer.Stop();
                }
            };

            // Tutorial Pulse Timer for Glowing Halos & Arrows
            _tutorialPulseTimer.Interval = 600;
            _tutorialPulseTimer.Tick += (s, e) =>
            {
                _pulseHighlight = !_pulseHighlight;
                for (int i = 0; i < _currentChoices.Count; i++)
                {
                    if (_currentChoices[i].IsRecommended)
                    {
                        _actionButtons[i].Invalidate();
                    }
                }
                if (_lblGuideArrow.Visible)
                {
                    _lblGuideArrow.ForeColor = _pulseHighlight ? Color.FromArgb(255, 235, 130) : Color.FromArgb(200, 180, 90);
                }
            };

            if (Scenario.HasTutorialGuidance)
            {
                _tutorialPulseTimer.Start();
            }
        }

        private static string QueueLocationText(EmergencyCall call) =>
            call.IsRestricted ? "SOURCE CLASSIFIED"
            : call.AddressVerified ? call.Location
            : "TRACING…";

        private void ApplyCrtFontScale(float fontScale)
        {
            bool changed = Math.Abs(fontScale - _crtFontScale) > 0.01f;
            _crtFontScale = fontScale;
            ScaleFontsIn(_pnlCrtScreen, changed);
            if (changed)
            {
                foreach (var btn in _actionButtons) btn?.Invalidate();
            }
        }

        private void ScaleFontsIn(Control parent, bool rescaleKnown)
        {
            foreach (Control child in parent.Controls)
            {
                bool known = _crtBaseFonts.TryGetValue(child, out var baseFont);
                if (!known)
                {
                    baseFont = child.Font;
                    _crtBaseFonts[child] = baseFont;
                }
                if (!known || rescaleKnown)
                {
                    child.Font = new Font(baseFont!.FontFamily, Math.Max(5f, baseFont.Size * _crtFontScale), baseFont.Style, baseFont.Unit);
                }
                ScaleFontsIn(child, rescaleKnown);
            }
        }

        private void BuildQueueCards()
        {
            _queueCards.Clear();
            for (int i = 0; i < _calls.Count; i++)
            {
                int callIdx = i;
                var call = _calls[i];

                var card = new Panel
                {
                    BackColor = (i == _activeCallIndex)
                        ? Color.FromArgb(200, 18, 40, 22)
                        : Color.FromArgb(160, 8, 16, 10),
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };

                var lblNum = new Label
                {
                    Text = (i + 1).ToString("D2"),
                    ForeColor = (i == _activeCallIndex) ? Color.FromArgb(200, 255, 180) : Color.FromArgb(140, 175, 130),
                    BackColor = Color.Transparent,
                    Font = FontManager.GetSmallCapsFont(15f, FontStyle.Bold),
                    Size = new Size(38, 48),
                    Location = new Point(4, 0),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                var lblLine = new Label
                {
                    Text = $"{call.CallId}\n{QueueLocationText(call)}",
                    ForeColor = Color.FromArgb(210, 225, 205),
                    BackColor = Color.Transparent,
                    Font = FontManager.GetSmallCapsFont(9f, FontStyle.Regular),
                    Size = new Size(114, 38),
                    Location = new Point(44, 8),
                    AutoEllipsis = true
                };

                var lblBadge = new Label
                {
                    Text = call.Status.ToString().ToUpperInvariant(),
                    ForeColor = call.Status switch
                    {
                        CallQueueStatus.Live => Color.FromArgb(120, 255, 160),
                        CallQueueStatus.Waiting => Color.FromArgb(255, 215, 110),
                        CallQueueStatus.OnHold => Color.FromArgb(130, 200, 255),
                        CallQueueStatus.Restricted => Color.FromArgb(255, 120, 120),
                        _ => Color.FromArgb(180, 180, 180)
                    },
                    BackColor = Color.Transparent,
                    Font = FontManager.GetSmallCapsFont(8f, FontStyle.Bold),
                    Size = new Size(54, 20),
                    Location = new Point(card.Width - 60, 14),
                    TextAlign = ContentAlignment.MiddleRight
                };

                card.Controls.AddRange(new Control[] { lblNum, lblLine, lblBadge });

                // Wire click to safely handle queue selection
                Action selectAction = () => TrySelectQueueCall(callIdx);
                card.Click += (s, e) => selectAction();
                lblNum.Click += (s, e) => selectAction();
                lblLine.Click += (s, e) => selectAction();
                lblBadge.Click += (s, e) => selectAction();

                card.MouseEnter += (s, e) =>
                {
                    if (callIdx != _activeCallIndex)
                    {
                        bool nextAvail = (CurrentCall.Status == CallQueueStatus.Resolved && callIdx == _activeCallIndex + 1);
                        if (nextAvail)
                            card.BackColor = Color.FromArgb(220, 24, 48, 26);
                        else if (_calls[callIdx].Status != CallQueueStatus.Resolved)
                            card.BackColor = Color.FromArgb(180, 14, 24, 14);
                    }
                };
                card.MouseLeave += (s, e) =>
                {
                    if (callIdx != _activeCallIndex)
                        UpdateQueueCards();
                };

                _queueCards.Add(card);
                _pnlQueueContainer.Controls.Add(card);
            }
        }

        private void ScrollTranscript(int delta)
        {
            int visibleH = _pnlTranscriptScroll.ClientSize.Height;
            int totalH = _lblCallTranscript.Height + 16;
            int maxScroll = Math.Max(0, totalH - visibleH);
            _transcriptScrollY = Math.Clamp(_transcriptScrollY + delta, 0, maxScroll);
            _lblCallTranscript.Location = new Point(14, 8 - _transcriptScrollY);
        }

        private void OnTranscriptMouseWheel(object? sender, MouseEventArgs e)
        {
            ScrollTranscript(e.Delta > 0 ? -40 : 40);
        }

        private void ScrollSupervisor(int delta)
        {
            int visibleH = _pnlSupervisorScroll.ClientSize.Height;
            int totalH = _lblSupervisorBar.Height + 12;
            int maxScroll = Math.Max(0, totalH - visibleH);
            _supervisorScrollY = Math.Clamp(_supervisorScrollY + delta, 0, maxScroll);
            _lblSupervisorBar.Location = new Point(10, 6 - _supervisorScrollY);
        }

        private void OnSupervisorMouseWheel(object? sender, MouseEventArgs e)
        {
            ScrollSupervisor(e.Delta > 0 ? -30 : 30);
        }

        private void TrySelectQueueCall(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _calls.Count) return;

            // Line 08 is strictly restricted by civil defense order
            if (_calls[targetIndex].IsRestricted)
            {
                AudioManager.Instance.PlayFrequencyBlip();
                _lblSupervisorBar.Text = Scenario.ShiftNumber == 1
                    ? "KRISHA: Look at the yellow note on your desk: 'Line 08 Do not answer.' That channel has been dead for twenty years."
                    : "SYSTEM: LINE 08 IS LOCKED BY CIVIL DEFENSE DIRECTIVE. DO NOT ANSWER.";
                return;
            }

            // Already on this active call
            if (targetIndex == _activeCallIndex)
            {
                return;
            }

            // Attempting to select an already resolved call
            if (_calls[targetIndex].Status == CallQueueStatus.Resolved)
            {
                AudioManager.Instance.PlayFrequencyBlip();
                _lblSupervisorBar.Text = $"{QueueAdvisor}: Trunk Line 0{(targetIndex + 1)} ({_calls[targetIndex].CallerName}) is already resolved and filed. Focus on your active emergency lines!";
                return;
            }

            // Current call is still active/unresolved: cannot abandon it to pick up another queued call
            if (CurrentCall.Status != CallQueueStatus.Resolved)
            {
                AudioManager.Instance.PlayFrequencyBlip();
                _lblSupervisorBar.Text = $"{QueueAdvisor}: Trunk Line 0{(targetIndex + 1)} is holding in queue. Emergency dispatch protocol requires completing active Line 0{(_activeCallIndex + 1)} first!";
                return;
            }

            // Current call IS resolved: operator is advancing to the next incoming emergency
            if (targetIndex == _activeCallIndex + 1)
            {
                SelectCall(targetIndex);
                return;
            }

            // Operator attempted to skip multiple calls ahead in the queue
            AudioManager.Instance.PlayFrequencyBlip();
            _lblSupervisorBar.Text = $"{QueueAdvisor}: Trunk Line 0{(targetIndex + 1)} is holding in queue. Emergency dispatch protocol requires taking incoming calls in order!";
        }

        private Label MakeActionButton()
        {
            var lbl = new Label
            {
                BackColor = Color.FromArgb(200, 8, 18, 10),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lbl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rc = new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1);

                if (lbl.Tag is not ActionChoice choice) return;

                bool isHovered = lbl.ClientRectangle.Contains(lbl.PointToClient(Cursor.Position));
                Color borderCol = isHovered ? choice.HoverColor : choice.BorderColor;

                if (choice.IsRecommended && _pulseHighlight)
                {
                    borderCol = Color.FromArgb(255, 225, 110);
                }

                float penWidth = (choice.IsRecommended && _pulseHighlight) ? 2.5f : 1.5f;
                using var borderPen = new Pen(borderCol, penWidth);
                e.Graphics.DrawRectangle(borderPen, rc);

                // Top Tag
                float fs = _crtFontScale;
                using var tagFont = FontManager.GetSmallCapsFont(9f * fs, FontStyle.Bold);
                int tagH = Math.Max(12, (int)(16 * fs));
                var tagRect = new Rectangle(0, Math.Max(2, (int)(4 * fs)), lbl.Width, tagH);
                string tagText = (choice.IsRecommended && Scenario.HasTutorialGuidance) ? $"[►] {choice.Tag}" : choice.Tag;
                Color tagCol = (choice.IsRecommended && _pulseHighlight) ? Color.FromArgb(255, 235, 140) : choice.TagColor;
                TextRenderer.DrawText(e.Graphics, tagText, tagFont, tagRect, tagCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);

                // Main Caption
                using var captionFont = FontManager.GetItalicFont(12.5f * fs);
                int capTop = tagRect.Bottom;
                var capRect = new Rectangle(6, capTop, lbl.Width - 12, lbl.Height - capTop - 3);
                Color capCol = isHovered ? Color.White : Color.FromArgb(220, 235, 215);
                TextRenderer.DrawText(e.Graphics, choice.Caption, captionFont, capRect, capCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
            };

            lbl.MouseEnter += (s, e) =>
            {
                if (lbl.Tag is ActionChoice choice)
                {
                    lbl.BackColor = choice.HoverBgColor;
                }
                AudioManager.Instance.PlayFrequencyBlip();
                lbl.Invalidate();
            };
            lbl.MouseLeave += (s, e) =>
            {
                if (lbl.Tag is ActionChoice choice)
                {
                    lbl.BackColor = choice.BgColor;
                }
                lbl.Invalidate();
            };
            lbl.Click += (s, e) =>
            {
                if (lbl.Tag is ActionChoice choice)
                {
                    choice.OnClick?.Invoke();
                }
            };

            return lbl;
        }

        public void SwitchView(ViewState view)
        {
            _currentView = view;
            if (_pnlDebriefModal != null) _pnlDebriefModal.Visible = false;
            if (_pnlManualModal != null) _pnlManualModal.Visible = false;

            bool isTerminal = (_currentView == ViewState.ComputerTerminal);

            _pnlDeskHeader.Visible = !isTerminal;
            _lblDeskHotspot.Visible = !isTerminal;
            if (_lblDeskBookHotspot != null) _lblDeskBookHotspot.Visible = !isTerminal;
            if (_lblDeskStickyNote != null) _lblDeskStickyNote.Visible = isTerminal && !(_charmDialogue?.Visible ?? false);

            _btnBackToDesk.Visible = isTerminal;
            _btnTerminalSettings.Visible = isTerminal;
            _btnTerminalEndShift.Visible = isTerminal;
            _pnlCrtScreen.Visible = isTerminal;

            if (isTerminal)
            {
                AudioManager.Instance.PlayKeyClick();
                SelectCall(_activeCallIndex);
            }
            else
            {
                AudioManager.Instance.PlayPaperRustle();
            }

            Invalidate();
        }

        private void SelectCall(int index)
        {
            if (index < 0 || index >= _calls.Count) return;
            _activeCallIndex = index;
            var call = CurrentCall;

            _lblCrtHeaderCenter.Text = $"CALL {(index + 1):D2} OF {_calls.Count:D2}";
            _lblQueueFooter.Text = $"ACTIVE   {(index + 1):D2} / {_calls.Count:D2}";

            _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
            _lblCallSignalTag.Text = $"[ SIGNAL {call.SignalStatus} ]";

            _lblLocation.Text = $"LOCATION: {call.DisplayLocation}";
            _lblSignal.Text = $"SIGNAL: {call.SignalStatus}";
            _lblTrust.Text = $"TRUST: {call.TrustPercentage}%";
            _lblPanic.Text = $"PANIC: {call.PanicPercentage}%";
            _lblPanic.ForeColor = call.PanicPercentage >= 80 ? Color.FromArgb(255, 100, 100) : Color.FromArgb(255, 180, 120);

            if (call.Stage == CallStage.Ringing)
            {
                _lblCallTranscript.Text = $"[ INCOMING EMERGENCY TRUNK LINE 0{(index + 1)}: RINGING... ]";
                _transcriptScrollY = 0;
                _lblCallTranscript.Location = new Point(14, 8);
                _lblSupervisorBar.Text = call.SupervisorHint;
            }
            else
            {
                StartTypewriter(call.InitialDialogue);
                _lblSupervisorBar.Text = call.SupervisorHint;
            }

            UpdateQueueCards();
            UpdateDynamicChoices();
            UpdateTutorialGuidance();

            OnCallSelected(call);
        }

        private void SetDynamicActionChoices(List<ActionChoice> choices)
        {
            _currentChoices.Clear();
            _currentChoices.AddRange(choices);

            for (int i = 0; i < _actionButtons.Length; i++)
            {
                var btn = _actionButtons[i];
                if (i < choices.Count)
                {
                    btn.Tag = choices[i];
                    btn.BackColor = choices[i].BgColor;
                    btn.Visible = true;
                    btn.Invalidate();
                }
                else
                {
                    btn.Tag = null;
                    btn.Visible = false;
                }
            }

            ApplyResponsiveLayout();
            UpdateTutorialGuidance();
        }

        private void UpdateDynamicChoices()
        {
            var call = CurrentCall;
            var choices = new List<ActionChoice>();

            if (call.IsRestricted)
            {
                choices.Add(new ActionChoice
                {
                    Tag = "RESTRICTED",
                    Caption = "⛔ Line 08 Locked by Civil Defense Directive (See Desk Note)",
                    BorderColor = Color.FromArgb(160, 60, 60),
                    TagColor = Color.FromArgb(255, 120, 120),
                    OnClick = () =>
                    {
                        _lblSupervisorBar.Text = Scenario.ShiftNumber >= 2
                            ? "YSA: Line 08 is locked by Civil Defense directive. Do not touch it."
                            : "KRISHA: Look at the yellow note on your desk: 'Line 08 Do not answer.' That channel has been dead for twenty years.";
                        AudioManager.Instance.PlayFrequencyBlip();
                    }
                });
            }
            else if (Scenario.ShiftNumber == 1 && _activeCallIndex == 0) // SHIFT 1 – CALL 01: Eleanor Vance (21 West Street, Apt 5B)
            {
                switch (call.Stage)
                {
                    case CallStage.Ringing:
                        choices.Add(new ActionChoice
                        {
                            Tag = "INCOMING LINE 01",
                            Caption = "📞 Answer Trunk Line 01 (West District)",
                            BorderColor = Color.FromArgb(120, 255, 160),
                            TagColor = Color.FromArgb(160, 255, 180),
                            HoverBgColor = Color.FromArgb(220, 20, 50, 26),
                            IsRecommended = true,
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Inquiry;
                                call.Status = CallQueueStatus.Live;
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter(call.InitialDialogue);
                                _lblSupervisorBar.Text = "KRISHA: Rule number one trainee: Always verify the caller's identity and exact address before taking action.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Inquiry:
                        if (!call.IdentityVerified)
                        {
                            choices.Add(new ActionChoice
                            {
                                Tag = "IDENTIFY",
                                Caption = "📍 Ask Name & Exact Address",
                                BorderColor = Color.FromArgb(255, 215, 120),
                                TagColor = Color.FromArgb(255, 230, 140),
                                IsRecommended = true,
                                OnClick = () =>
                                {
                                    call.IdentityVerified = true;
                                    call.AddressVerified = true;
                                    _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
                                    _lblLocation.Text = $"LOCATION: {call.DisplayLocation}";
                                    call.TrustPercentage = Math.Min(100, call.TrustPercentage + 10);
                                    _lblTrust.Text = $"TRUST: {call.TrustPercentage}%";
                                    AudioManager.Instance.PlayTypewriterKeystroke();
                                    StartTypewriter("YOU: \"Can you state your name and confirm your building address for the dispatch log?\"\n\nELEANOR: \"Yes, my name is Eleanor Vance. I live at 21 West Street, Apartment 5B. The racket is directly above me in Apartment 6B.\"");
                                    _lblSupervisorBar.Text = "KRISHA: Identity verified: Eleanor Vance, 21 West St, Apt 5B. Now ask about the disturbance or inspect the archive records.";
                                    UpdateQueueCards();
                                    UpdateDynamicChoices();
                                }
                            });
                            choices.Add(new ActionChoice
                            {
                                Tag = "INQUIRE",
                                Caption = "🗣 Inquire about disturbance details",
                                OnClick = () =>
                                {
                                    AudioManager.Instance.PlayKeyClick();
                                    StartTypewriter("YOU: \"Can you describe the noise you're hearing upstairs?\"\n\nCALLER: \"It sounds like heavy furniture scraping across floorboards. No shouting or fighting, but it's relentless.\"");
                                    _lblSupervisorBar.Text = "KRISHA: Good. Confirmed no immediate danger. But remember to log her name and address!";
                                    UpdateDynamicChoices();
                                }
                            });
                        }
                        else
                        {
                            choices.Add(new ActionChoice
                            {
                                Tag = "INQUIRE",
                                Caption = "🗣 Inquire about disturbance details",
                                OnClick = () =>
                                {
                                    AudioManager.Instance.PlayKeyClick();
                                    StartTypewriter("YOU: \"Can you describe the noise you're hearing upstairs, Ms. Vance?\"\n\nELEANOR: \"It sounds like heavy furniture scraping across floorboards in Apartment 6B. Nobody's screaming, but it's keeping me awake.\"");
                                    _lblSupervisorBar.Text = "KRISHA: No threat to life reported. Now inspect the building records before deciding to dispatch.";
                                    UpdateDynamicChoices();
                                }
                            });
                            choices.Add(new ActionChoice
                            {
                                Tag = "ARCHIVE RECORDS",
                                Caption = "🗄 Check incident log for 21 West St (Without Hold)",
                                BorderColor = Color.FromArgb(110, 180, 220),
                                TagColor = Color.FromArgb(140, 210, 255),
                                IsRecommended = true,
                                OnClick = () =>
                                {
                                    call.Stage = CallStage.Resolution;
                                    AudioManager.Instance.PlayTypewriterKeystroke();
                                    StartTypewriter("INCIDENT ARCHIVE #1042: 21 WEST STREET, APT 6B\nRESIDENT: ROBERT COOPER. PRIOR VIOLATIONS: NONE.\nCLASSIFICATION: NON-EMERGENCY RESIDENTIAL NOISE.");
                                    _lblSupervisorBar.Text = "KRISHA: Robert Cooper has a clean record. Standard noise complaint. We don't need to burn a patrol car for this.";
                                    UpdateDynamicChoices();
                                }
                            });
                        }
                        break;

                    case CallStage.Resolution:
                        choices.Add(new ActionChoice
                        {
                            Tag = "LOG REPORT",
                            Caption = "📋 Record noise complaint (0 Patrol Units Used)",
                            BorderColor = Color.FromArgb(120, 220, 140),
                            TagColor = Color.FromArgb(160, 255, 180),
                            IsRecommended = true,
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Resolved;
                                call.Status = CallQueueStatus.Resolved;
                                State.CallsLogged++;
                                AudioManager.Instance.PlayInkStamp();
                                StartTypewriter("YOU: \"We've logged your complaint for building management and the morning shift, Ms. Vance.\"\n\nELEANOR: \"Thank you. Hopefully they'll quiet down soon.\"\n\n[ INCIDENT #1042 LOGGED AS ROUTINE DISTURBANCE. CALL CONCLUDED. ]\n[ PATROL UNITS PRESERVED: " + State.AvailableUnits + " / " + Scenario.InitialUnits + " ]");
                                _lblSupervisorBar.Text = "KRISHA: Exactly right. Simple noise doesn't justify burning our patrol capacity. We keep cars ready for real emergencies.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "DISPATCH",
                            Caption = "🚓 Dispatch patrol unit (1 Patrol Unit Used)",
                            BorderColor = Color.FromArgb(220, 180, 100),
                            TagColor = Color.FromArgb(255, 215, 130),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Resolved;
                                call.Status = CallQueueStatus.Resolved;
                                call.UnitDispatched = true;
                                if (State.AvailableUnits > 0) State.AvailableUnits--;
                                State.CallsLogged++;
                                AudioManager.Instance.PlayInkStamp();
                                StartTypewriter("YOU: \"A patrol car has been dispatched to 21 West Street.\"\n\n[ PATROL UNIT 12 DISPATCHED TO 21 WEST STREET. ETA: 8 MIN. ]\n[ PATROL UNITS REMAINING: " + State.AvailableUnits + " / " + Scenario.InitialUnits + " ]");
                                _lblSupervisorBar.Text = "KRISHA: Unit dispatched. Remember: for routine noise, logging the report is usually sufficient to preserve our limited units.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Resolved:
                        choices.Add(new ActionChoice
                        {
                            Tag = "INCOMING LINE 02",
                            Caption = "🔔 Answer Incoming Trunk Line 02 (High Priority Intrusion)",
                            BorderColor = Color.FromArgb(255, 200, 100),
                            TagColor = Color.FromArgb(255, 220, 130),
                            HoverBgColor = Color.FromArgb(220, 48, 36, 16),
                            IsRecommended = true,
                            OnClick = () => SelectCall(1)
                        });
                        break;
                }
            }
            else if (Scenario.ShiftNumber == 1 && _activeCallIndex == 1) // SHIFT 1 – CALL 02: Chloe Bautista (14 Bell Street)
            {
                switch (call.Stage)
                {
                    case CallStage.Ringing:
                        choices.Add(new ActionChoice
                        {
                            Tag = "URGENT INCOMING",
                            Caption = "📞 Answer Line 02 (High Priority Home Intrusion)",
                            BorderColor = Color.FromArgb(255, 140, 100),
                            TagColor = Color.FromArgb(255, 180, 140),
                            HoverBgColor = Color.FromArgb(220, 50, 20, 16),
                            IsRecommended = true,
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Inquiry;
                                call.Status = CallQueueStatus.Live;
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter(call.InitialDialogue);
                                _lblSupervisorBar.Text = "KRISHA: Active home intrusion! Keep her calm, identify who and where she is, and inspect the sector archive immediately!";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Inquiry:
                        if (!call.IdentityVerified)
                        {
                            choices.Add(new ActionChoice
                            {
                                Tag = "IDENTIFY",
                                Caption = "📍 Ask Name & Exact Address",
                                BorderColor = Color.FromArgb(255, 180, 120),
                                TagColor = Color.FromArgb(255, 205, 140),
                                IsRecommended = true,
                                OnClick = () =>
                                {
                                    call.IdentityVerified = true;
                                    call.AddressVerified = true;
                                    _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
                                    _lblLocation.Text = $"LOCATION: {call.DisplayLocation}";
                                    call.TrustPercentage = Math.Min(100, call.TrustPercentage + 15);
                                    call.PanicPercentage = Math.Max(20, call.PanicPercentage - 10);
                                    _lblTrust.Text = $"TRUST: {call.TrustPercentage}%";
                                    _lblPanic.Text = $"PANIC: {call.PanicPercentage}%";
                                    AudioManager.Instance.PlayTypewriterKeystroke();
                                    StartTypewriter("YOU: \"Ma'am, take a deep breath. I need your name and exact address so I can get officers moving.\"\n\nCALLER: \"Chloe! Chloe Bautista! I'm at 14 Bell Street! The back patio door glass was smashed downstairs!\"");
                                    _lblSupervisorBar.Text = "KRISHA: Chloe Bautista at 14 Bell Street. Keep her calm, instruct her to hide, and check the archive records!";
                                    UpdateQueueCards();
                                    UpdateDynamicChoices();
                                }
                            });
                        }
                        else
                        {
                            // Calming Action with 15s Cooldown
                            if (_reassuranceCooldownSeconds > 0)
                            {
                                choices.Add(new ActionChoice
                                {
                                    Tag = "REASSURE [COOLDOWN]",
                                    Caption = $"🛡 Instruct Chloe to stay hidden [ COOLDOWN: {_reassuranceCooldownSeconds}s ]",
                                    BorderColor = Color.FromArgb(110, 110, 110),
                                    TagColor = Color.FromArgb(150, 150, 150),
                                    OnClick = () =>
                                    {
                                        AudioManager.Instance.PlayFrequencyBlip();
                                        _lblSupervisorBar.Text = "KRISHA: Give her a moment to catch her breath before repeating yourself.";
                                    }
                                });
                            }
                            else
                            {
                                choices.Add(new ActionChoice
                                {
                                    Tag = "REASSURE",
                                    Caption = "🛡 Instruct Chloe to stay hidden and silent (-25% Panic)",
                                    BorderColor = Color.FromArgb(130, 220, 200),
                                    TagColor = Color.FromArgb(160, 255, 230),
                                    OnClick = () =>
                                    {
                                        call.PanicPercentage = Math.Max(10, call.PanicPercentage - 25);
                                        call.TrustPercentage = Math.Min(100, call.TrustPercentage + 20);
                                        _reassuranceCooldownSeconds = 15;
                                        _reassuranceCooldownTimer.Start();
                                        _lblPanic.Text = $"PANIC: {call.PanicPercentage}%";
                                        _lblTrust.Text = $"TRUST: {call.TrustPercentage}%";
                                        AudioManager.Instance.PlayKeyClick();
                                        StartTypewriter("YOU: \"Chloe, listen carefully. Stay hidden in the closet, keep your phone pressed close, and do not make a sound. We are dispatching help right now.\"\n\nCHLOE: \"(Whispering) I'm tucked behind the winter coats... I hear heavy steps on the stairs... please hurry...\"");
                                        _lblSupervisorBar.Text = "KRISHA: You calmed her down. Now check the incident archive before dispatching!";
                                        UpdateDynamicChoices();
                                    }
                                });
                            }

                            choices.Add(new ActionChoice
                            {
                                Tag = "LOG ARCHIVE",
                                Caption = "🗄 Check incident archive #2196 (Without Hold)",
                                BorderColor = Color.FromArgb(200, 120, 255),
                                TagColor = Color.FromArgb(230, 180, 255),
                                IsRecommended = true,
                                OnClick = () =>
                                {
                                    call.Stage = CallStage.DiscrepancyChoice;
                                    AudioManager.Instance.PlayFrequencyBlip();
                                    StartTypewriter("CRITICAL SYSTEM DISCREPANCY DETECTED\nSTATION RECORD #2196 MODIFIED: 01:42 AM.\nINCOMING CALL RECEIVED: 02:16 AM.\nDISCREPANCY: RECORD WAS MODIFIED 34 MINUTES BEFORE CALL WAS RECEIVED!\nREGISTERED ADDRESS: 41 BELL AVE // REPORTED LOCATION: 14 BELL ST.");
                                    _lblSupervisorBar.Text = "KRISHA: Wait a second... that record was modified thirty-four minutes before this call even rang! How is that possible?! Flag that discrepancy!";
                                    UpdateDynamicChoices();
                                }
                            });
                        }
                        break;

                    case CallStage.DiscrepancyChoice:
                        choices.Add(new ActionChoice
                        {
                            Tag = "FLAG PARADOX",
                            Caption = "🚩 Flag Address & Timestamp Paradox in Station Archive",
                            BorderColor = Color.FromArgb(220, 120, 255),
                            TagColor = Color.FromArgb(240, 190, 255),
                            HoverBgColor = Color.FromArgb(220, 48, 16, 56),
                            IsRecommended = true,
                            OnClick = () =>
                            {
                                call.DiscrepancyFlagged = true;
                                State.CluesFound++;
                                if (!Profile.AnomalyFlags.Contains("BellStreetTimestampParadox"))
                                    Profile.AnomalyFlags.Add("BellStreetTimestampParadox");
                                AudioManager.Instance.PlayInkStamp();
                                _lblClueBanner.Visible = true;
                                StartTypewriter("[ EVIDENCE FLAGGED: PARADOX RECORDED IN DISPATCH ARCHIVE. ]\n[ CLUE 01 LOGGED: BELL STREET 34-MINUTE TIMESTAMP PARADOX ]");
                                _lblSupervisorBar.Text = "KRISHA: Excellent catch! That timestamp mismatch is going straight into tonight's official debriefing. Now get units en route under Code 3!";
                                call.Stage = CallStage.Resolution;
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "BYPASS",
                            Caption = "⏩ Bypass Paradox & Prioritize Immediate Dispatch",
                            BorderColor = Color.FromArgb(160, 160, 160),
                            TagColor = Color.FromArgb(200, 200, 200),
                            OnClick = () =>
                            {
                                StartTypewriter("[ DISCREPANCY BYPASSED WITHOUT ARCHIVE FLAG. PROCEEDING TO IMMEDIATE DISPATCH. ]");
                                _lblSupervisorBar.Text = "KRISHA: Overlooking that discrepancy might hurt your official review, but let's get those units moving!";
                                call.Stage = CallStage.Resolution;
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Resolution:
                        choices.Add(new ActionChoice
                        {
                            Tag = "CODE 3 DISPATCH",
                            Caption = "🚓 Dispatch Patrol Units 08 & 14 (Code 3 Lights & Sirens - Fast ETA)",
                            BorderColor = Color.FromArgb(255, 120, 120),
                            TagColor = Color.FromArgb(255, 170, 170),
                            HoverBgColor = Color.FromArgb(220, 60, 20, 20),
                            IsRecommended = true,
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Resolved;
                                call.Status = CallQueueStatus.Resolved;
                                call.UnitDispatched = true;
                                State.AvailableUnits -= (State.AvailableUnits >= 2 ? 2 : 1);
                                State.CallsLogged++;
                                AudioManager.Instance.PlayInkStamp();
                                StartTypewriter(
                                    "YOU: \"Officers are responding with lights and sirens, Chloe. Stay down and keep quiet.\"\n\n" +
                                    "[ UNITS 08 & 14 DISPATCHED UNDER CODE 3 TO 14 BELL STREET. ETA: 6 SECONDS. ]\n\n" +
                                    "[ RADIO SQUELCH // UNIT 08 ON SCENE: \"Dispatch, we're at 14 Bell Street... but the house is completely dark. Front and back doors are bolted shut from the inside. Dust is thick on the windows... nobody has lived here in weeks.\" ]\n\n" +
                                    "[ CHLOE'S LINE CUTS SUDDENLY TO A HOLLOW DEAD DIAL TONE. ]\n\n" +
                                    "[ THE NIGHT DESK FALLS SILENT AS THE LINE HISSES WITH VACANT STATIC. ]"
                                );
                                _lblSupervisorBar.Text = "KRISHA: ...What did Unit 08 just say? Dust on the windows? But she was just talking to us...";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Resolved:
                        choices.Add(new ActionChoice
                        {
                            Tag = "INCOMING LINE 03",
                            Caption = "🔔 Answer Incoming Trunk Line 03 (Marcus Reed)",
                            BorderColor = Color.FromArgb(140, 200, 255),
                            TagColor = Color.FromArgb(180, 225, 255),
                            HoverBgColor = Color.FromArgb(220, 16, 36, 52),
                            IsRecommended = true,
                            OnClick = () => SelectCall(2)
                        });
                        break;
                }
            }
            else if (Scenario.ShiftNumber == 1 && _activeCallIndex == 2) // SHIFT 1 – CALL 03: Marcus Reed (88 Elm Ridge Road)
            {
                switch (call.Stage)
                {
                    case CallStage.Ringing:
                        choices.Add(new ActionChoice
                        {
                            Tag = "UNKNOWN TRUNK",
                            Caption = "📞 Answer Trunk Line 03 (88 Elm Ridge Road)",
                            BorderColor = Color.FromArgb(140, 200, 255),
                            TagColor = Color.FromArgb(180, 225, 255),
                            IsRecommended = true,
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Inquiry;
                                call.Status = CallQueueStatus.Live;
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter(call.InitialDialogue);
                                _lblSupervisorBar.Text = "KRISHA: Suspicious vehicle report. Verify details and determine if this warrants our limited patrol units.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Inquiry:
                        if (!call.IdentityVerified)
                        {
                            choices.Add(new ActionChoice
                            {
                                Tag = "IDENTIFY",
                                Caption = "📍 Ask Name & Address",
                                BorderColor = Color.FromArgb(160, 220, 140),
                                TagColor = Color.FromArgb(190, 245, 170),
                                IsRecommended = true,
                                OnClick = () =>
                                {
                                    call.IdentityVerified = true;
                                    call.AddressVerified = true;
                                    _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
                                    _lblLocation.Text = $"LOCATION: {call.DisplayLocation}";
                                    call.TrustPercentage = Math.Min(100, call.TrustPercentage + 10);
                                    _lblTrust.Text = $"TRUST: {call.TrustPercentage}%";
                                    AudioManager.Instance.PlayTypewriterKeystroke();
                                    StartTypewriter("YOU: \"This is emergency dispatch. Please state your name and location for the log.\"\n\nMARCUS: \"Marcus Reed. I live at 88 Elm Ridge Road. There's a car parked right outside my gate.\"");
                                    _lblSupervisorBar.Text = "KRISHA: Marcus Reed, 88 Elm Ridge Road. Now ask for a vehicle description or check sector logs.";
                                    UpdateQueueCards();
                                    UpdateDynamicChoices();
                                }
                            });
                        }
                        else
                        {
                            choices.Add(new ActionChoice
                            {
                                Tag = "VEHICLE",
                                Caption = "🚗 Ask for car description & occupants",
                                OnClick = () =>
                                {
                                    AudioManager.Instance.PlayKeyClick();
                                    StartTypewriter("YOU: \"Can you describe the vehicle, Mr. Reed?\"\n\nMARCUS: \"It's a dark sedan. Tinted glass, no lights on, engine idling softly for nearly two hours. I can't see who's in the driver's seat.\"");
                                    _lblSupervisorBar.Text = "KRISHA: Dark sedan idling without lights. Ask if they've engaged in suspicious activity or check the sector archive.";
                                    UpdateDynamicChoices();
                                }
                            });
                            choices.Add(new ActionChoice
                            {
                                Tag = "ACTIVITY",
                                Caption = "🔍 Ask if any suspicious activity was seen",
                                OnClick = () =>
                                {
                                    AudioManager.Instance.PlayKeyClick();
                                    StartTypewriter("YOU: \"Did you see anyone enter or exit the vehicle?\"\n\nMARCUS: \"About ten minutes ago, someone in a dark coat got out with a flashlight and walked up the gravel driveway to the empty farmhouse across the road.\"");
                                    _lblSupervisorBar.Text = "KRISHA: Walking toward an empty house. Check the sector log for 88 Elm Ridge Road.";
                                    UpdateDynamicChoices();
                                }
                            });
                            choices.Add(new ActionChoice
                            {
                                Tag = "SECTOR LOG",
                                Caption = "🗄 Check Elm Ridge sector archive #0882",
                                BorderColor = Color.FromArgb(140, 200, 255),
                                TagColor = Color.FromArgb(180, 225, 255),
                                IsRecommended = true,
                                OnClick = () =>
                                {
                                    call.Stage = CallStage.Resolution;
                                    AudioManager.Instance.PlayTypewriterKeystroke();
                                    StartTypewriter("STATION ARCHIVE #0882: 88 ELM RIDGE ROAD\nSECTOR: NORTH SUBURBAN PERIMETER.\nNOTE: RESIDENCE ACROSS THE ROAD (89 ELM RIDGE) HAS BEEN VACANT FOR SIX WEEKS. NO CURRENT OCCUPANCY PERMITS.");
                                    _lblSupervisorBar.Text = "KRISHA: An abandoned farmhouse across the road. Now decide: dispatch your remaining unit or log a suspicious vehicle report.";
                                    UpdateDynamicChoices();
                                }
                            });
                        }
                        break;

                    case CallStage.Resolution:
                        choices.Add(new ActionChoice
                        {
                            Tag = "DISPATCH UNIT",
                            Caption = "🚓 Dispatch Patrol Unit 04 (1 Patrol Unit Used)",
                            BorderColor = Color.FromArgb(220, 180, 100),
                            TagColor = Color.FromArgb(255, 215, 130),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Resolved;
                                call.Status = CallQueueStatus.Resolved;
                                call.UnitDispatched = true;
                                if (State.AvailableUnits > 0) State.AvailableUnits--;
                                State.CallsLogged++;
                                AudioManager.Instance.PlayInkStamp();
                                StartTypewriter("[ PATROL UNIT 04 DISPATCHED TO 88 ELM RIDGE ROAD. ]\n\n[ RADIO SQUELCH: \"Unit 04 arriving... dark sedan is gone. Fresh tire tracks lead north toward the highway.\" ]\n[ CALL RESOLVED. STATUS: ARCHIVED. ]");
                                _lblSupervisorBar.Text = "KRISHA: Unit 04 arrived, vehicle fled. You burned our last patrol car on a stationary vehicle—good community vigilance, but be careful with resource management.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "LOG REPORT",
                            Caption = "📋 Record Suspicious Vehicle Complaint (0 Patrol Units Used)",
                            BorderColor = Color.FromArgb(120, 220, 140),
                            TagColor = Color.FromArgb(160, 255, 180),
                            IsRecommended = true,
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Resolved;
                                call.Status = CallQueueStatus.Resolved;
                                State.CallsLogged++;
                                AudioManager.Instance.PlayInkStamp();
                                StartTypewriter("YOU: \"We've logged the dark sedan description for the morning sector sweep, Mr. Reed. Lock your doors and call us if they approach your property.\"\n\nMARCUS: \"Understood. Thank you, dispatcher.\"\n\n[ INCIDENT LOGGED AS SUSPICIOUS VEHICLE. PATROL CAPACITY PRESERVED: " + State.AvailableUnits + " / " + Scenario.InitialUnits + " ]\n[ CALL RESOLVED. STATUS: ARCHIVED. ]");
                                _lblSupervisorBar.Text = "KRISHA: Outstanding judgment. You preserved our patrol reserve while keeping the neighborhood monitored.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Resolved:
                        choices.Add(new ActionChoice
                        {
                            Tag = "SHIFT COMPLETE",
                            Caption = "📋 FINISH SHIFT 1 & OPEN SUPERVISOR PERSONNEL FILE",
                            BorderColor = Color.FromArgb(140, 255, 180),
                            TagColor = Color.FromArgb(180, 255, 210),
                            HoverBgColor = Color.FromArgb(240, 24, 60, 30),
                            IsRecommended = true,
                            OnClick = () => ShowDebriefModal()
                        });
                        break;
                }
            }

            // ══════════════════════════════════════════════════════════════
            // SHIFT 2 DYNAMIC CHOICES (mirrors the Shift 2 script)
            // ══════════════════════════════════════════════════════════════
            else if (Scenario.ShiftNumber == 2 && _activeCallIndex == 0) // SHIFT 2 – CALL 01: Stranded Caller (Old Water Tower Road)
            {
                switch (call.Stage)
                {
                    case CallStage.Ringing:
                        choices.Add(Shift2AnswerChoice(call, "INCOMING LINE 01", "📞 Answer call"));
                        break;

                    case CallStage.Inquiry:
                        // Protocol first: get the caller's name before anything else
                        if (!call.IdentityVerified)
                        {
                            choices.Add(Shift2Option("c1.name", "IDENTIFY", "📇 Ask for the caller's name",
                                "CALLER 1: My name? I— sorry, I can barely think straight.\nCALLER 1: Please, I just need someone to come out here.",
                                string.Empty,
                                () =>
                                {
                                    call.IdentityVerified = true;
                                    _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
                                    UpdateQueueCards();
                                }, recommended: true));
                            break;
                        }

                        // Per the script: the landmark and street-record branches converge on the caller
                        // asking if you're still there; the danger question loops back to the options.
                        string[] locate = { "c1.landmark", "c1.records" };
                        string LocateReply(string key, string reply) =>
                            locate.Where(k => k != key).All(Shift2Asked)
                                ? reply + "\n\nCALLER 1: Hello? Are you still there?\nCALLER 1: I haven't moved. I'm still beside the same sign."
                                : reply;
                        void AfterLocate()
                        {
                            if (locate.All(Shift2Asked)) call.Stage = CallStage.DiscrepancyChoice;
                        }

                        if (!Shift2Asked("c1.landmark"))
                        {
                            choices.Add(Shift2Option("c1.landmark", "LANDMARK", "📍 Ask for the nearest landmark",
                                LocateReply("c1.landmark", "CALLER 1: There's a sign for the old water tower. I passed it a few minutes ago.\nSYSTEM: LOCATION CHECK: 2 MILES FROM WATER TOWER."),
                                string.Empty,
                                () =>
                                {
                                    call.AddressVerified = true;
                                    _lblLocation.Text = $"LOCATION: {call.DisplayLocation}";
                                    UpdateQueueCards();
                                    AfterLocate();
                                }, recommended: true));
                        }
                        else if (!Shift2Asked("c1.recheck"))
                        {
                            // [If the player checks the location again.]
                            choices.Add(Shift2Option("c1.recheck", "RE-CHECK", "🔁 Check the location again",
                                "SYSTEM: LOCATION CHECK: 5 MILES FROM WATER TOWER.",
                                string.Empty));
                        }
                        if (!Shift2Asked("c1.danger"))
                        {
                            choices.Add(Shift2Option("c1.danger", "DANGER", "⚠ Ask about immediate danger",
                                "CALLER 1: I'm not sure what to do. It's dark out here, and I need help getting off the road.",
                                string.Empty));
                        }
                        if (!Shift2Asked("c1.records"))
                        {
                            choices.Add(Shift2Option("c1.records", "STREET RECORDS", "🗄 Check available street records",
                                LocateReply("c1.records", "SYSTEM: LOCATION COULD NOT BE FULLY VERIFIED."),
                                string.Empty,
                                AfterLocate));
                        }
                        break;

                    case CallStage.DiscrepancyChoice:
                        // Flag and dispatch are alternatives; both end on the caller drifting away
                        const string driftingAway = "\n\nCALLER 1: You said you could find me.\nCALLER 1: Why does it keep getting farther away?\n[The line crackles.]\nSYSTEM: SIGNAL UNSTABLE.";
                        choices.Add(new ActionChoice
                        {
                            Tag = "FLAG",
                            Caption = "🚩 Flag the inconsistent location data",
                            BorderColor = Color.FromArgb(220, 120, 255),
                            TagColor = Color.FromArgb(240, 190, 255),
                            HoverBgColor = Color.FromArgb(220, 48, 16, 56),
                            IsRecommended = true,
                            OnClick = () =>
                            {
                                call.DiscrepancyFlagged = true;
                                State.CluesFound++;
                                if (!Profile.AnomalyFlags.Contains("ChangingDistance"))
                                    Profile.AnomalyFlags.Add("ChangingDistance");
                                AudioManager.Instance.PlayInkStamp();
                                _lblClueBanner.Text = "⭐ EVIDENCE FLAGGED: ROUTE 9 — THE DISTANCE KEEPS CHANGING";
                                _lblClueBanner.Visible = true;
                                StartTypewriter(call.FlagResponse + driftingAway);
                                Shift2ResolveCall(call);
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "DISPATCH",
                            Caption = "🚓 Dispatch using last available information",
                            BorderColor = Color.FromArgb(220, 180, 100),
                            TagColor = Color.FromArgb(255, 215, 130),
                            OnClick = () =>
                            {
                                string units = Shift2DispatchUnit(call);
                                AudioManager.Instance.PlayInkStamp();
                                StartTypewriter(units + "\nSYSTEM: DISPATCH LOCATION BASED ON LAST AVAILABLE INFORMATION." + driftingAway);
                                Shift2ResolveCall(call);
                            }
                        });
                        break;

                    case CallStage.Resolved:
                        choices.Add(Shift2NextCallChoice(1, "INCOMING LINE 02"));
                        break;
                }
            }
            else if (Scenario.ShiftNumber == 2 && _activeCallIndex == 1) // SHIFT 2 – CALL 02: Bell Street door
            {
                switch (call.Stage)
                {
                    case CallStage.Ringing:
                        choices.Add(Shift2AnswerChoice(call, "URGENT INCOMING", "📞 Answer call"));
                        break;

                    case CallStage.Inquiry:
                        if (!Shift2Asked("c2.safe"))
                        {
                            choices.Add(Shift2Option("c2.safe", "SAFETY", "🛡 Ask whether the caller is somewhere safe",
                                "CALLER 2: I'm in my bedroom. I locked the door, too.\nCALLER 2: I can still hear someone moving around outside. Please, I don't think they know I'm here.",
                                string.Empty,
                                () =>
                                {
                                    call.PanicPercentage = Math.Max(20, call.PanicPercentage - 10);
                                    _lblPanic.Text = $"PANIC: {call.PanicPercentage}%";
                                }, recommended: true));
                        }
                        if (!Shift2Asked("c2.address"))
                        {
                            choices.Add(Shift2Option("c2.address", "ADDRESS", "📍 Ask for the address and nearby landmarks",
                                "CALLER 2: Fourteen Bell Street. It's the house with the broken streetlight near the corner.\n\n" +
                                "SYSTEM: ADDRESS RECEIVED: 14 BELL STREET.\nSYSTEM: ADDRESS STATUS: INCONSISTENT WITH RECENT RECORD.\n\n" +
                                "CALLER 2: Is something wrong? That's where I live. I've lived here for years.",
                                string.Empty,
                                () =>
                                {
                                    call.IdentityVerified = true;
                                    call.AddressVerified = true;
                                    _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
                                    _lblLocation.Text = $"LOCATION: {call.DisplayLocation}";
                                    UpdateQueueCards();
                                }, recommended: true));
                        }
                        if (!Shift2Asked("c2.entered"))
                        {
                            choices.Add(Shift2Option("c2.entered", "INTRUDER", "🚪 Ask whether the person has entered the house",
                                "CALLER 2: No. Not yet.\nCALLER 2: I can hear them moving around outside. Every time I get close to the door, they stop.\nCALLER 2: I don't know what they want. They haven't said anything.",
                                string.Empty));
                        }
                        if (!Shift2Asked("c2.log"))
                        {
                            choices.Add(Shift2Option("c2.log", "INCIDENT LOG", "🗄 Check the incident log",
                                "SYSTEM: ADDRESS RECEIVED.\nSYSTEM: ADDRESS STATUS: INCONSISTENT WITH RECENT RECORD.",
                                string.Empty));
                        }
                        // Per the script: "safe" and "address" loop back to the questions; the "entered"
                        // and "incident log" branches converge on the two ways to handle the call
                        if (Shift2Asked("c2.entered") && Shift2Asked("c2.log"))
                        {
                            choices.Add(new ActionChoice
                            {
                                Tag = "ASK CHARM",
                                Caption = "🎧 Ask Charm for assistance with the call",
                                BorderColor = Color.FromArgb(255, 160, 200),
                                TagColor = Color.FromArgb(255, 195, 220),
                                OnClick = () =>
                                {
                                    // The signal drops when "SYSTEM: CALLER DISCONNECTED." prints (see PlayNextSegment)
                                    AudioManager.Instance.PlayFrequencyBlip();
                                    StartTypewriter(
                                        "CHARM: Of course. You check the record; I'll take over the caller.\n[Charm puts on her headset.]\n" +
                                        "CHARM: Hello? I'm here. Can you tell me whether the door is still locked?\nCALLER 2: Yes.\n" +
                                        "CHARM: Good. Stay somewhere safe while we work out the details.\n\n[A few seconds pass.]\n" +
                                        "CHARM: Hm. The line's gone quiet.\nSYSTEM: CALLER DISCONNECTED.\n" +
                                        "CHARM: That's odd. Maybe the connection dropped.\n[Charm removes her headset and smiles reassuringly.]\n" +
                                        "CHARM: Don't look so worried. These old lines are unreliable.\n\n" +
                                        $"[{Profile.OperatorName} checks the call record.]\n" +
                                        "SYSTEM: CALL DISCONNECTED BEFORE RESPONSE CONFIRMATION.\nSYSTEM: OUTCOME UNCONFIRMED.\n\n" +
                                        "CHARM: You know, you don't have to do everything alone. We're coworkers.\n" +
                                        "CHARM: If something gets difficult, just ask. That's what I'm here for.\n[She returns to her console.]", charmAtConsole: true);
                                    Shift2ResolveCall(call);
                                }
                            });
                            choices.Add(new ActionChoice
                            {
                                Tag = "DISPATCH",
                                Caption = "🚓 Dispatch a unit using the best available verified information",
                                BorderColor = Color.FromArgb(255, 120, 120),
                                TagColor = Color.FromArgb(255, 170, 170),
                                HoverBgColor = Color.FromArgb(220, 60, 20, 20),
                                IsRecommended = true,
                                OnClick = () =>
                                {
                                    string units = Shift2DispatchUnit(call);
                                    AudioManager.Instance.PlayInkStamp();
                                    StartTypewriter(units + "\n\nCALLER 2: Okay. I'll stay away from the door.\n\n" +
                                        "SYSTEM: DISPATCH RECORDED.\nSYSTEM: ADDRESS DISCREPANCY REMAINS UNRESOLVED.\nSYSTEM: CALL RECORD SAVED.\nSYSTEM: CALL 2 COMPLETE.");
                                    Shift2ResolveCall(call);
                                }
                            });
                        }
                        break;

                    case CallStage.Resolved:
                        choices.Add(Shift2NextCallChoice(2, "INCOMING LINE 03"));
                        break;
                }
            }
            else if (Scenario.ShiftNumber == 2 && _activeCallIndex == 2) // SHIFT 2 – CALL 03: Chloe asking for Krisha
            {
                switch (call.Stage)
                {
                    case CallStage.Ringing:
                        choices.Add(Shift2AnswerChoice(call, "UNKNOWN TRUNK", "📞 Answer call"));
                        break;

                    case CallStage.Inquiry:
                        // Tier 1: who is calling and why. All three branches converge (per the script)
                        // on Chloe asking where Krisha is, and only then do the next questions open up.
                        string[] tier1 = { "c3.offshift", "c3.name", "c3.assist" };
                        bool tier1Done = tier1.All(Shift2Asked);
                        string Tier1Reply(string key, string reply) =>
                            tier1.Where(k => k != key).All(Shift2Asked)
                                ? reply + "\n\nCHLOE: You don't know where she is?\nCHLOE: She said someone would answer tonight. She told me to call back if it happened again."
                                : reply;

                        if (!Shift2Asked("c3.offshift"))
                        {
                            choices.Add(Shift2Option("c3.offshift", "KRISHA", "💬 Tell the caller Krisha is no longer on shift",
                                Tier1Reply("c3.offshift", "CHLOE: She's not there?\nCHLOE: But she said someone would answer tonight.\nCHLOE: She told me to call back if it happened again."),
                                string.Empty));
                        }
                        if (!Shift2Asked("c3.name"))
                        {
                            choices.Add(Shift2Option("c3.name", "IDENTIFY", "📍 Ask for the caller's name and reason for calling",
                                Tier1Reply("c3.name", "CHLOE: Chloe.\nCHLOE: I need to speak to Krisha. She knows about it.\nCHLOE: She told me to call back if it happened again."),
                                string.Empty,
                                () =>
                                {
                                    call.IdentityVerified = true;
                                    _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
                                    UpdateQueueCards();
                                }, recommended: true));
                        }
                        if (!Shift2Asked("c3.assist"))
                        {
                            choices.Add(Shift2Option("c3.assist", "ASSISTANCE", "⚠ Ask whether they need immediate assistance",
                                Tier1Reply("c3.assist", "CHLOE: I... I think so.\nCHLOE: I don't know if it's happening again.\nCHLOE: Krisha said someone would answer if I called."),
                                string.Empty));
                        }

                        // Tier 2: what happened, where, and what the records say
                        if (tier1Done)
                        {
                            if (!Shift2Asked("c3.what"))
                            {
                                choices.Add(Shift2Option("c3.what", "WHAT HAPPENED", "❓ Ask what happened",
                                    "CHLOE: I heard it again.\nCHLOE: At first, I thought it was outside. Then I heard it near the door.\nCHLOE: I called before, but nobody came.\nCHLOE: Krisha said she'd write everything down.",
                                    string.Empty));
                            }
                            if (!Shift2Asked("c3.address"))
                            {
                                choices.Add(Shift2Option("c3.address", "ADDRESS", "📍 Ask for the address",
                                    "CHLOE: Fourteen Bell Street.\n\nSYSTEM: ADDRESS NOT FOUND IN CURRENT DATABASE.\n\n" +
                                    "CHLOE: That's what it said last time.\nCHLOE: Krisha told me not to worry about it. She said she'd write everything down.",
                                    string.Empty,
                                    recommended: true));
                            }
                            if (!Shift2Asked("c3.records"))
                            {
                                choices.Add(Shift2Option("c3.records", "RECORDS", "🗄 Check the previous incident records",
                                    call.LogResponse,
                                    string.Empty));
                            }
                        }

                        // Tier 3: once all of tier 2 is asked. Both questions converge (per the script)
                        // on Charmee noticing you, which leads to the player action.
                        string[] tier3 = { "c3.danger", "c3.surroundings" };
                        bool tier3Done = tier3.All(Shift2Asked);
                        string Tier3Reply(string key, string reply) =>
                            tier3.Where(k => k != key).All(Shift2Asked)
                                ? reply + $"\n\nCHARM: Hey, are you okay?\n[Charm is looking at {Profile.OperatorName}, not at her own screen.]\nCHARM: You look like you've seen a ghost."
                                : reply;

                        bool tier2Done = new[] { "c3.what", "c3.address", "c3.records" }.All(Shift2Asked);
                        if (tier2Done)
                        {
                            if (!Shift2Asked("c3.danger"))
                            {
                                choices.Add(Shift2Option("c3.danger", "DANGER", "⚠ Ask whether Chloe is in immediate danger",
                                    Tier3Reply("c3.danger", "CHLOE: I don't know.\nCHLOE: I can hear something, but I can't tell where it's coming from.\nCHLOE: I'm inside.\nCHLOE: I'm trying to stay away from the door."),
                                    string.Empty));
                            }
                            if (!Shift2Asked("c3.surroundings"))
                            {
                                choices.Add(Shift2Option("c3.surroundings", "SURROUNDINGS", "👁 Ask her to describe her surroundings",
                                    Tier3Reply("c3.surroundings", call.TraceResponse),
                                    string.Empty,
                                    recommended: true));
                            }
                        }

                        // PLAYER ACTION: Show Charm the call record
                        if (tier3Done)
                        {
                            choices.Add(new ActionChoice
                            {
                                Tag = "PLAYER ACTION",
                                Caption = "📋 Show Charm the call record",
                                BorderColor = Color.FromArgb(220, 120, 255),
                                TagColor = Color.FromArgb(240, 190, 255),
                                HoverBgColor = Color.FromArgb(220, 48, 16, 56),
                                IsRecommended = true,
                                OnClick = () =>
                                {
                                    call.DiscrepancyFlagged = true;
                                    State.CluesFound++;
                                    if (!Profile.AnomalyFlags.Contains("ChloeKrishaConnection"))
                                        Profile.AnomalyFlags.Add("ChloeKrishaConnection");
                                    AudioManager.Instance.PlayInkStamp();
                                    _lblClueBanner.Text = "⭐ EVIDENCE FLAGGED: CHLOE REMEMBERS KRISHA — 14 BELL STREET";
                                    _lblClueBanner.Visible = true;
                                    StartTypewriter("CHARM: Fourteen Bell Street?\n[For a brief moment, Charm stops smiling.]\n" +
                                        "CHARM: Oh. That's the address from your first night, isn't it?\nCHARM: Weird coincidence.\n[She smiles again.]\n" +
                                        "CHARM: Well, if it isn't in the system, there probably isn't much we can do until we verify it. Right?\n\n" +
                                        call.FlagResponse);
                                    Shift2ResolveCall(call);
                                }
                            });
                        }
                        break;

                    case CallStage.Resolved:
                        if (!Shift2Asked("endshift"))
                        {
                            choices.Add(Shift2Option("endshift", "03:00 AM", "☕ End-of-shift conversation",
                                "[03:00 AM — Dispatch Center]\n\n" +
                                "CHARM: Well, that was fun.\n" +
                                "CHARM: A stranded driver whose location won't stay still, someone at a door, and a caller who knows your missing trainer.\n" +
                                "CHARM: You know, when I started here, my weirdest call was a man who reported his own car stolen while he was sitting inside it.\n" +
                                "[She laughs.]\nCHARM: I wish I were joking.\n\n" +
                                $"[{Profile.OperatorName} looks toward Ysa's empty console.]\n\n" +
                                "CHARM: Hey. We'll find out what happened eventually.\nCHARM: Until then, let's just do our jobs, okay?",
                                string.Empty, recommended: true));
                        }
                        else
                        {
                            choices.Add(new ActionChoice
                            {
                                Tag = "SHIFT COMPLETE",
                                Caption = "📋 FINISH SHIFT 2",
                                BorderColor = Color.FromArgb(140, 255, 180),
                                TagColor = Color.FromArgb(180, 255, 210),
                                HoverBgColor = Color.FromArgb(240, 24, 60, 30),
                                IsRecommended = true,
                                OnClick = () => ShowDebriefModal()
                            });
                        }
                        break;
                }
            }
            else if (Scenario.ShiftNumber == 3) // SHIFT 3 – see ShiftPlayForm.Shift3.cs
            {
                AddShift3Choices(call, choices);
            }

            SetDynamicActionChoices(choices);
            UpdatePhoneRing();
        }

        /// <summary>
        /// The phone rings only while the current line is ringing (not yet answered),
        /// and never under the end-of-shift report or for the locked Line 08.
        /// </summary>
        private void UpdatePhoneRing()
        {
            var call = CurrentCall;
            bool reportOpen = _pnlDebriefModal != null && _pnlDebriefModal.Visible;
            bool ringing = !reportOpen && !call.IsRestricted &&
                           call.Stage == CallStage.Ringing && call.Status != CallQueueStatus.Resolved &&
                           !Shift3BriefingPending && // Shift 3: the first call rings after the briefing
                           !ShiftBookOpen;           // nothing rings until the player clocks in

            if (ringing) AudioManager.Instance.StartPhoneRing();
            else AudioManager.Instance.StopPhoneRing();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            AudioManager.Instance.StopPhoneRing();
            base.OnFormClosed(e);
        }

        // ══════════════════════════════════════════════════════════════
        // SHIFT 2 HELPERS
        // ══════════════════════════════════════════════════════════════
        private readonly HashSet<string> _shift2Asked = new();

        private bool Shift2Asked(string key) => _shift2Asked.Contains(key);

        private ActionChoice Shift2AnswerChoice(EmergencyCall call, string tag, string caption) => new ActionChoice
        {
            Tag = tag,
            Caption = caption,
            BorderColor = Color.FromArgb(120, 220, 180),
            TagColor = Color.FromArgb(160, 255, 200),
            HoverBgColor = Color.FromArgb(220, 20, 50, 36),
            IsRecommended = true,
            OnClick = () =>
            {
                call.Stage = CallStage.Inquiry;
                call.Status = CallQueueStatus.Live;
                AudioManager.Instance.PlayKeyClick();
                StartTypewriter(call.InitialDialogue);
                if (call.SupervisorHint.Length > 0) _lblSupervisorBar.Text = call.SupervisorHint;
                UpdateQueueCards();
                UpdateDynamicChoices();
            }
        };

        private ActionChoice Shift2Option(string key, string tag, string caption, string response, string charmLine, Action? onAsked = null, bool recommended = false) => new ActionChoice
        {
            Tag = tag,
            Caption = caption,
            BorderColor = Color.FromArgb(255, 215, 120),
            TagColor = Color.FromArgb(255, 230, 140),
            IsRecommended = recommended,
            OnClick = () =>
            {
                _shift2Asked.Add(key);
                onAsked?.Invoke();
                AudioManager.Instance.PlayTypewriterKeystroke();
                StartTypewriter(response);
                if (charmLine.Length > 0) _lblSupervisorBar.Text = charmLine;
                UpdateDynamicChoices();
            }
        };

        private ActionChoice Shift2NextCallChoice(int nextIndex, string tag) => new ActionChoice
        {
            Tag = tag,
            Caption = "🔔 Wait for the next call to ring",
            BorderColor = Color.FromArgb(140, 200, 255),
            TagColor = Color.FromArgb(180, 225, 255),
            HoverBgColor = Color.FromArgb(220, 16, 36, 52),
            IsRecommended = true,
            OnClick = () => SelectCall(nextIndex)
        };

        private string Shift2DispatchUnit(EmergencyCall call)
        {
            call.UnitDispatched = true;
            if (State.AvailableUnits > 0) State.AvailableUnits--;
            return $"SYSTEM: UNIT ASSIGNED.\nSYSTEM: REMAINING UNITS: {State.AvailableUnits}.";
        }

        private void Shift2ResolveCall(EmergencyCall call)
        {
            call.Stage = CallStage.Resolved;
            call.Status = CallQueueStatus.Resolved;
            State.CallsLogged++;
            UpdateQueueCards();
            UpdateDynamicChoices();
        }

        private void UpdateTutorialGuidance()
        {
            // Guide arrows belong to the tutorial shift only
            if (!Scenario.HasTutorialGuidance)
            {
                _lblGuideArrow.Visible = false;
                return;
            }

            var call = CurrentCall;

            // 1. Live Protocol Checklist Across Top
            string step1 = (_activeCallIndex > 0 || call.Stage != CallStage.Ringing) ? "[✓]" : "[►]";
            string step2 = (call.Stage != CallStage.Ringing) ? "[✓]" : ((_activeCallIndex == 0) ? "[►]" : "[ ]");
            string step3 = (call.IdentityVerified) ? "[✓]" : ((call.Stage == CallStage.Inquiry && !call.IdentityVerified) ? "[►]" : "[ ]");
            string step4 = (call.Stage == CallStage.DiscrepancyChoice || call.Stage == CallStage.Resolution || call.Stage == CallStage.Resolved) ? "[✓]" : ((call.Stage == CallStage.Inquiry && call.IdentityVerified) ? "[►]" : "[ ]");
            string step5 = (call.Stage == CallStage.Resolved) ? "[✓]" : ((call.Stage == CallStage.Resolution) ? "[►]" : "[ ]");

            _lblProtocolChecklist.Text = $"{step1} 1. Select Line   {step2} 2. Open Audio   {step3} 3. Identify Caller   {step4} 4. Check Archive   {step5} 5. Resolve";

            // 2. Guide Arrow Pointer Placement
            if (call.Stage == CallStage.Ringing)
            {
                _lblGuideArrow.Text = "◄── [ANSWER TRUNK LINE]";
                if (_actionButtons[0].Visible)
                {
                    _lblGuideArrow.Location = new Point(_actionButtons[0].Right + 6, _actionButtons[0].Top + (_actionButtons[0].Height - _lblGuideArrow.Height) / 2);
                    _lblGuideArrow.Visible = true;
                    _lblGuideArrow.BringToFront();
                }
            }
            else if (call.Stage == CallStage.Inquiry && !call.IdentityVerified)
            {
                _lblGuideArrow.Text = "◄── [ASK NAME & ADDRESS FIRST]";
                if (_actionButtons[0].Visible)
                {
                    _lblGuideArrow.Location = new Point(_actionButtons[0].Right + 6, _actionButtons[0].Top + (_actionButtons[0].Height - _lblGuideArrow.Height) / 2);
                    _lblGuideArrow.Visible = true;
                    _lblGuideArrow.BringToFront();
                }
            }
            else if (call.Stage == CallStage.Inquiry && call.IdentityVerified)
            {
                _lblGuideArrow.Text = "◄── [CHECK INCIDENT ARCHIVE]";
                int targetIdx = -1;
                for (int i = 0; i < _currentChoices.Count; i++)
                {
                    if (_currentChoices[i].Tag.Contains("ARCHIVE") || _currentChoices[i].Tag.Contains("RECORDS") || _currentChoices[i].Tag.Contains("LOG"))
                    {
                        targetIdx = i;
                        break;
                    }
                }
                if (targetIdx >= 0 && targetIdx < _actionButtons.Length && _actionButtons[targetIdx].Visible)
                {
                    _lblGuideArrow.Location = new Point(_actionButtons[targetIdx].Right + 6, _actionButtons[targetIdx].Top + (_actionButtons[targetIdx].Height - _lblGuideArrow.Height) / 2);
                    _lblGuideArrow.Visible = true;
                    _lblGuideArrow.BringToFront();
                }
            }
            else if (call.Stage == CallStage.DiscrepancyChoice)
            {
                _lblGuideArrow.Text = "◄── [FLAG TIMESTAMP PARADOX]";
                if (_actionButtons[0].Visible)
                {
                    _lblGuideArrow.Location = new Point(_actionButtons[0].Right + 6, _actionButtons[0].Top + (_actionButtons[0].Height - _lblGuideArrow.Height) / 2);
                    _lblGuideArrow.Visible = true;
                    _lblGuideArrow.BringToFront();
                }
            }
            else if (call.Stage == CallStage.Resolution)
            {
                _lblGuideArrow.Text = "◄── [SELECT RESOLUTION]";
                if (_actionButtons[0].Visible)
                {
                    _lblGuideArrow.Location = new Point(_actionButtons[0].Right + 6, _actionButtons[0].Top + (_actionButtons[0].Height - _lblGuideArrow.Height) / 2);
                    _lblGuideArrow.Visible = true;
                    _lblGuideArrow.BringToFront();
                }
            }
            else if (call.Stage == CallStage.Resolved)
            {
                if (_activeCallIndex < 2)
                {
                    _lblGuideArrow.Text = "◄── [ANSWER NEXT INCOMING TRUNK]";
                }
                else
                {
                    _lblGuideArrow.Text = "◄── [SUBMIT INCIDENT LOGS / END SHIFT]";
                }
                if (_actionButtons[0].Visible)
                {
                    _lblGuideArrow.Location = new Point(_actionButtons[0].Right + 6, _actionButtons[0].Top + (_actionButtons[0].Height - _lblGuideArrow.Height) / 2);
                    _lblGuideArrow.Visible = true;
                    _lblGuideArrow.BringToFront();
                }
            }
            else
            {
                _lblGuideArrow.Visible = false;
            }

            // No room beside the button: the glowing [►] button already guides the player,
            // so hide the arrow rather than letting it spill off the CRT or cover the readouts
            if (_lblGuideArrow.Visible && _lblGuideArrow.Right > _pnlCrtScreen.ClientSize.Width - 4)
            {
                _lblGuideArrow.Visible = false;
            }
        }

        private void UpdatePanicDecay()
        {
            if (_activeCallIndex == 1) // Call 2: Chloe Bautista
            {
                var call = CurrentCall;
                if (call.Stage == CallStage.Inquiry || call.Stage == CallStage.DiscrepancyChoice)
                {
                    call.PanicPercentage = Math.Min(100, call.PanicPercentage + 4);
                    call.TrustPercentage = Math.Max(10, call.TrustPercentage - 1);
                    _lblPanic.Text = $"PANIC: {call.PanicPercentage}%";
                    _lblTrust.Text = $"TRUST: {call.TrustPercentage}%";
                    _lblPanic.ForeColor = (call.PanicPercentage >= 80)
                        ? Color.FromArgb(255, 90, 90)
                        : Color.FromArgb(255, 180, 120);
                }
            }
        }

        private void UpdateQueueCards()
        {
            for (int i = 0; i < _queueCards.Count; i++)
            {
                var card = _queueCards[i];
                var call = _calls[i];

                bool isActive = (i == _activeCallIndex);
                bool isResolved = (call.Status == CallQueueStatus.Resolved);
                bool isNextAvailable = (!isActive && !isResolved && CurrentCall.Status == CallQueueStatus.Resolved && i == _activeCallIndex + 1);
                bool isAccessible = isActive || isNextAvailable;

                card.Cursor = isAccessible ? Cursors.Hand : Cursors.Default;

                if (isActive)
                {
                    card.BackColor = Color.FromArgb(200, 18, 40, 22);
                }
                else if (isNextAvailable)
                {
                    card.BackColor = Color.FromArgb(180, 28, 38, 20);
                }
                else if (isResolved)
                {
                    card.BackColor = Color.FromArgb(130, 6, 12, 8);
                }
                else
                {
                    card.BackColor = Color.FromArgb(150, 8, 16, 10);
                }

                if (card.Controls.Count >= 3)
                {
                    if (card.Controls[0] is Label lblNum)
                    {
                        lblNum.ForeColor = isActive ? Color.FromArgb(200, 255, 180)
                                          : isNextAvailable ? Color.FromArgb(255, 230, 140)
                                          : isResolved ? Color.FromArgb(110, 140, 115)
                                          : Color.FromArgb(140, 175, 130);
                    }
                    if (card.Controls[1] is Label lblLine)
                    {
                        lblLine.Text = $"{call.CallId}\n{QueueLocationText(call)}";
                        lblLine.ForeColor = isActive ? Color.FromArgb(235, 250, 230)
                                          : isResolved ? Color.FromArgb(130, 150, 135)
                                          : Color.FromArgb(210, 225, 205);
                    }
                    if (card.Controls[2] is Label lblBadge)
                    {
                        if (call.IsRestricted)
                        {
                            lblBadge.Text = "RESTRICTED";
                            lblBadge.ForeColor = Color.FromArgb(255, 120, 120);
                        }
                        else if (isResolved)
                        {
                            lblBadge.Text = "RESOLVED";
                            lblBadge.ForeColor = Color.FromArgb(130, 180, 155);
                        }
                        else if (isActive)
                        {
                            if (call.Stage == CallStage.Ringing)
                            {
                                lblBadge.Text = "RINGING";
                                lblBadge.ForeColor = Color.FromArgb(255, 215, 110);
                            }
                            else
                            {
                                lblBadge.Text = "LIVE";
                                lblBadge.ForeColor = Color.FromArgb(120, 255, 160);
                            }
                        }
                        else if (isNextAvailable)
                        {
                            lblBadge.Text = "RINGING";
                            lblBadge.ForeColor = Color.FromArgb(255, 215, 110);
                        }
                        else
                        {
                            lblBadge.Text = "QUEUED";
                            lblBadge.ForeColor = Color.FromArgb(190, 170, 105);
                        }
                    }
                }
            }
        }

        private static bool IsCharmLine(string line)
        {
            string t = line.TrimStart();
            return t.StartsWith("CHARM:", StringComparison.OrdinalIgnoreCase) ||
                   t.StartsWith("CHARMEE:", StringComparison.OrdinalIgnoreCase);
        }

        // Stage directions about the room (not the phone line) belong with Charmee's pop-up
        private static bool IsRoomNarration(string line)
        {
            string t = line.Trim();
            return t.StartsWith("[") &&
                   (t.Contains("Charm", StringComparison.OrdinalIgnoreCase) ||
                    t.StartsWith("[She ", StringComparison.Ordinal) ||
                    t.StartsWith("[Her ", StringComparison.Ordinal) ||
                    t.StartsWith("[You ", StringComparison.Ordinal));
        }

        private Image? CharmPortraitFor(string line)
        {
            string l = line.ToLowerInvariant();
            string expression =
                (l.Contains("laugh") || l.Contains("joking") || l.Contains("fun") || l.Contains("stolen")) ? "giggle" :
                (l.Contains("strange") || l.Contains("odd") || l.Contains("quiet") || l.Contains("ghost") ||
                 l.Contains("weird") || l.Contains("fourteen bell") || l.Contains("since when") || l.Contains("hm.")) ? "serious" :
                (l.Contains("don't worry") || l.Contains("coffee") || l.Contains("here for") || l.Contains("coworkers") ||
                 l.Contains("okay?") || l.Contains("good.") || l.Contains("right here")) ? "smile" :
                "default";

            if (!_charmPortraits.TryGetValue(expression, out var img))
            {
                img = TryLoadImage($"charm_{expression}.png") ?? TryLoadImage("charm_default.png");
                _charmPortraits[expression] = img;
            }
            return img;
        }

        /// <summary>
        /// Splits a response into ordered segments: runs of caller/system lines for the CRT and
        /// runs of Charmee's lines (plus room narration around her) for her pop-up.
        /// </summary>
        private List<ScriptSegment> SplitScript(string text, bool charmAtConsole)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var segments = new List<ScriptSegment>();
            if (Scenario.HasTutorialGuidance || !lines.Any(IsCharmLine))
            {
                segments.Add(new ScriptSegment(text, null));
                return segments;
            }

            bool roomOnly = lines.All(l => string.IsNullOrWhiteSpace(l) || IsCharmLine(l) || l.TrimStart().StartsWith("["));
            List<string>? crt = null;
            List<(string, string)>? charm = null;

            void FlushCrt()
            {
                if (crt == null) return;
                while (crt.Count > 0 && string.IsNullOrWhiteSpace(crt[^1])) crt.RemoveAt(crt.Count - 1);
                if (crt.Count > 0) segments.Add(new ScriptSegment(string.Join("\n", crt), null));
                crt = null;
            }
            void FlushCharm()
            {
                if (charm == null) return;
                if (charm.Count > 0) segments.Add(new ScriptSegment(null, charm, charmAtConsole));
                charm = null;
            }

            foreach (var line in lines)
            {
                bool isCharm = IsCharmLine(line);
                bool isRoom = !isCharm && !string.IsNullOrWhiteSpace(line) &&
                              line.TrimStart().StartsWith("[") && (roomOnly || IsRoomNarration(line));

                if (isCharm || isRoom)
                {
                    FlushCrt();
                    charm ??= new List<(string, string)>();
                    if (isCharm)
                    {
                        string body = line.Substring(line.IndexOf(':') + 1).Trim();
                        _charmQueuedLines.Add(body);
                        charm.Add(("CHARMEE", body));
                    }
                    else
                    {
                        charm.Add((string.Empty, line.Trim()));
                    }
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // Keep paragraph breaks inside a CRT run only
                    if (crt != null && crt.Count > 0 && !string.IsNullOrWhiteSpace(crt[^1])) crt.Add(string.Empty);
                }
                else
                {
                    FlushCharm();
                    crt ??= new List<string>();
                    crt.Add(line);
                }
            }
            FlushCrt();
            FlushCharm();
            return segments;
        }

        private void SetCallSignal(string status)
        {
            CurrentCall.SignalStatus = status;
            _lblSignal.Text = $"SIGNAL: {status}";
            _lblCallSignalTag.Text = $"[ SIGNAL {status} ]";
        }

        /// <summary>Plays the next queued segment, once the CRT and Charmee are both idle.</summary>
        private void PlayNextSegment()
        {
            if (_typewriterTimer.Enabled || _charmDialogue.IsActive) return;
            if (_scriptSegments.Count == 0) return;

            var seg = _scriptSegments.Dequeue();
            if (seg.CrtText != null)
            {
                // The signal readout changes at the moment the transcript says so, not before
                if (seg.CrtText.Contains("SYSTEM: CALLER DISCONNECTED", StringComparison.Ordinal))
                    SetCallSignal("DISCONNECTED");
                else if (seg.CrtText.Contains("SYSTEM: SIGNAL UNSTABLE", StringComparison.Ordinal))
                    SetCallSignal("UNSTABLE");

                if (_freshTranscriptPending)
                {
                    _freshTranscriptPending = false;
                    _targetDialogue = seg.CrtText;
                    _displayedDialogue = string.Empty;
                    _typewriterCharIndex = 0;
                    _transcriptScrollY = 0;
                    _lblCallTranscript.Location = new Point(14, 8);
                }
                else
                {
                    // Continue the same transcript after Charmee's interjection
                    _targetDialogue += "\n\n" + seg.CrtText;
                }
                _typewriterTimer.Start();
            }
            else if (seg.CharmLines != null)
            {
                _charmDialogue.Docked = seg.Docked;
                LayoutCharmDialogue();
                foreach (var (speaker, line) in seg.CharmLines)
                {
                    _charmDialogue.Enqueue(speaker, line, speaker.Length > 0 ? CharmPortraitFor(line) : null);
                }
            }
        }

        /// <summary>
        /// Outside the tutorial, CHARM hint-bar lines are dropped (they are not in the script):
        /// the bar keeps showing the last system note instead.
        /// </summary>
        private void RouteCharmHint()
        {
            if (_charmDialogue == null || Scenario.HasTutorialGuidance) return;

            string text = _lblSupervisorBar.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                _lblSupervisorBar.Text = _lastSystemBarText;
                return;
            }
            if (!IsCharmLine(text))
            {
                _lastSystemBarText = text;
                return;
            }

            // Hint-bar lines are not part of the Shift script, so Charmee does not say them;
            // she only speaks the CHARM lines written into the call transcripts.
            _lblSupervisorBar.Text = _lastSystemBarText;
        }

        /// <param name="charmAtConsole">
        /// Charmee's lines show in the compact strip under the computer (she is working the line
        /// with you) instead of the full-screen room scene.
        /// </param>
        private void StartTypewriter(string text, bool charmAtConsole = false)
        {
            // A new response supersedes whatever was still waiting to play; finish printing the
            // current reply first so it isn't left cut off mid-word
            if (_typewriterTimer.Enabled)
            {
                _displayedDialogue = _targetDialogue;
                _typewriterCharIndex = _targetDialogue.Length;
                _lblCallTranscript.Text = _displayedDialogue;
            }
            _typewriterTimer.Stop();
            _scriptSegments.Clear();
            _charmQueuedLines.Clear();
            _charmDialogue.Clear();

            foreach (var seg in SplitScript(text, charmAtConsole)) _scriptSegments.Enqueue(seg);

            // The transcript is replaced by the first CRT segment; if only Charmee speaks, it stays as is
            _freshTranscriptPending = true;
            PlayNextSegment();
        }

        private void UpdateTypewriter()
        {
            if (_typewriterCharIndex < _targetDialogue.Length)
            {
                if (SettingsManager.Instance.Current.GetDelayPerCharacterMs() == 0)
                {
                    _displayedDialogue += _targetDialogue.Substring(_typewriterCharIndex);
                    _typewriterCharIndex = _targetDialogue.Length;
                }
                else
                {
                    _displayedDialogue += _targetDialogue[_typewriterCharIndex];
                    _typewriterCharIndex++;
                }
                _lblCallTranscript.Text = _displayedDialogue;

                // Follow typewriter downwards so the latest typed line is visible
                int visibleH = _pnlTranscriptScroll.ClientSize.Height;
                int totalH = _lblCallTranscript.Height + 16;
                int maxScroll = Math.Max(0, totalH - visibleH);
                _transcriptScrollY = maxScroll;
                _lblCallTranscript.Location = new Point(14, 8 - _transcriptScrollY);

                if (_typewriterCharIndex % 5 == 0)
                {
                    AudioManager.Instance.PlayTypewriterKeystroke();
                }
            }
            else
            {
                _typewriterTimer.Stop();
                int visibleH = _pnlTranscriptScroll.ClientSize.Height;
                int totalH = _lblCallTranscript.Height + 16;
                int maxScroll = Math.Max(0, totalH - visibleH);
                _transcriptScrollY = maxScroll;
                _lblCallTranscript.Location = new Point(14, 8 - _transcriptScrollY);

                // The caller has finished: Charmee may speak now
                PlayNextSegment();
            }
        }

        private void UpdateStopwatch()
        {
            var elapsed = DateTime.Now - _shiftStartTime;
            _lblCrtHeaderTimer.Text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            _lblCrtHeaderLeft.Text = $"NIGHT DESK // SHIFT 0{Scenario.ShiftNumber}   [ UNITS: {State.AvailableUnits}/{Scenario.InitialUnits} ]";
        }

        private void PnlCrtScreen_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rc = _pnlCrtScreen.ClientRectangle;

            // 1. CRT Curved Phosphor Vignette
            using (var path = new GraphicsPath())
            {
                path.AddRectangle(rc);
                using var pgb = new PathGradientBrush(path);
                pgb.CenterColor = Color.FromArgb(0, 0, 0, 0);
                pgb.SurroundColors = new[] { Color.FromArgb(140, 2, 6, 2) };
                pgb.FocusScales = new PointF(0.75f, 0.70f);
                g.FillRectangle(pgb, rc);
            }

            // 2. Analog Green Scanlines
            using var scanPen = new Pen(Color.FromArgb(18, 0, 0, 0), 1f);
            for (int y = 0; y < rc.Height; y += 3)
            {
                g.DrawLine(scanPen, 0, y, rc.Width, y);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            var rc = ClientRectangle;

            if (_currentView == ViewState.DeskRoom && _imgDeskRoom != null)
            {
                // Draw Desk Room Background Cover-Fit
                DrawImageCoverFit(g, _imgDeskRoom, rc);
            }
            else if (_currentView == ViewState.ComputerTerminal && _imgComputerDesk != null)
            {
                // Draw Computer Terminal Mockup Background Contain-Fit (preserves monitor, desk, note, mug)
                DrawImageContainFit(g, _imgComputerDesk, rc);
            }
        }

        private static Rectangle GetCoverFitRect(Image img, Rectangle targetRect)
        {
            float imgRatio = (float)img.Width / img.Height;
            float targetRatio = (float)targetRect.Width / targetRect.Height;

            int drawW, drawH, drawX, drawY;
            if (targetRatio > imgRatio)
            {
                drawW = targetRect.Width;
                drawH = (int)(targetRect.Width / imgRatio);
                drawX = targetRect.X;
                drawY = targetRect.Y + (targetRect.Height - drawH) / 2;
            }
            else
            {
                drawH = targetRect.Height;
                drawW = (int)(targetRect.Height * imgRatio);
                drawX = targetRect.X + (targetRect.Width - drawW) / 2;
                drawY = targetRect.Y;
            }

            return new Rectangle(drawX, drawY, drawW, drawH);
        }

        private static Rectangle GetContainFitRect(Image img, Rectangle targetRect)
        {
            float imgRatio = (float)img.Width / img.Height;
            float targetRatio = (float)targetRect.Width / targetRect.Height;

            int drawW, drawH, drawX, drawY;
            if (targetRatio > imgRatio)
            {
                // Fit to height
                drawH = targetRect.Height;
                drawW = (int)(targetRect.Height * imgRatio);
                drawX = targetRect.X + (targetRect.Width - drawW) / 2;
                drawY = targetRect.Y;
            }
            else
            {
                // Fit to width
                drawW = targetRect.Width;
                drawH = (int)(targetRect.Width / imgRatio);
                drawX = targetRect.X;
                drawY = targetRect.Y + (targetRect.Height - drawH) / 2;
            }

            return new Rectangle(drawX, drawY, drawW, drawH);
        }

        private static void DrawImageCoverFit(Graphics g, Image img, Rectangle targetRect)
        {
            var rc = GetCoverFitRect(img, targetRect);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, rc);
        }

        private static void DrawImageContainFit(Graphics g, Image img, Rectangle targetRect)
        {
            var rc = GetContainFitRect(img, targetRect);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, rc);
        }

        /// <summary>
        /// Full-screen room scene by default; a strip under the computer when docked.
        /// </summary>
        private void LayoutCharmDialogue()
        {
            int W = ClientSize.Width, H = ClientSize.Height;
            if (W <= 0 || H <= 0) return;

            if (_charmDialogue.Docked)
            {
                int dlgH = Math.Clamp((int)(H * 0.21f), 120, 300);
                int dlgW = Math.Min(W - (int)(W * 0.08f), (int)(dlgH * 6.2f));
                _charmDialogue.Bounds = new Rectangle((W - dlgW) / 2, H - dlgH - (int)(H * 0.025f), dlgW, dlgH);
            }
            else
            {
                _charmDialogue.Bounds = ClientRectangle;
            }
            if (_charmDialogue.Visible) _charmDialogue.BringToFront();
            _charmDialogue.Invalidate();
        }

        private void LayoutDeskHeader(float deskScale, bool fontsChanged)
        {
            float s = Math.Clamp(deskScale, 0.6f, 2f);
            _pnlDeskHeader.Height = Math.Max(40, (int)(60 * s));
            if (fontsChanged) _lblDeskHeaderTitle.Font = FontManager.GetSmallCapsFont(15f * s, FontStyle.Bold);
            _lblDeskHeaderTitle.Padding = new Padding((int)(24 * s), 0, 0, 0);

            // Right-hand buttons sized to their text so they never wrap onto two lines
            int pad = (int)(28 * s);
            foreach (var (btn, size, style) in new[]
            {
                (_btnDeskSettings, 13f, FontStyle.Regular),
                (_btnDeskManual, 13f, FontStyle.Bold),
                (_btnDeskEndShift, 13f, FontStyle.Bold)
            })
            {
                if (fontsChanged) btn.Font = FontManager.GetSmallCapsFont(size * s, style);
                btn.Width = TextRenderer.MeasureText(btn.Text, btn.Font).Width + pad;
            }

            int buttonsW = _btnDeskSettings.Width + _btnDeskManual.Width + _btnDeskEndShift.Width;
            int titleW = Math.Max(100, ClientSize.Width - buttonsW - (int)(12 * s));
            _lblDeskHeaderTitle.Width = titleW;

            // Shrink the title until it sits on a single line beside the buttons
            int textRoom = titleW - _lblDeskHeaderTitle.Padding.Horizontal - 4;
            float titleSize = 15f * s;
            while (titleSize > 9f * s &&
                   TextRenderer.MeasureText(_lblDeskHeaderTitle.Text, _lblDeskHeaderTitle.Font).Width > textRoom)
            {
                titleSize -= 0.5f;
                _lblDeskHeaderTitle.Font = FontManager.GetSmallCapsFont(titleSize, FontStyle.Bold);
            }
        }

        private void ApplyResponsiveLayout()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            if (_pnlDebriefModal != null && _pnlDebriefModal.Visible)
            {
                _pnlDebriefModal.Bounds = ClientRectangle;
                _pnlDebriefModal.BringToFront();
                _pnlCrtScreen.Visible = false;
                _pnlDeskHeader.Visible = false;
                _lblDeskHotspot.Visible = false;
                _btnBackToDesk.Visible = false;
                _btnTerminalSettings.Visible = false;
                _btnTerminalEndShift.Visible = false;
                if (_lblDeskStickyNote != null) _lblDeskStickyNote.Visible = false;
                return;
            }

            if (_pnlManualModal != null && _pnlManualModal.Visible)
            {
                _pnlManualModal.Bounds = ClientRectangle;
                _pnlManualModal.BringToFront();
                return;
            }

            // The Shift Briefing book covers the desk until the player clocks in
            if (_pnlShiftBook != null)
            {
                _pnlShiftBook.Bounds = ClientRectangle;
                _pnlShiftBook.BringToFront();
                return;
            }

            int W = ClientSize.Width;
            int H = ClientSize.Height;
            float deskScale = H / 1080f;

            if (_charmDialogue != null)
            {
                _charmDialogue.SceneBackground ??= _imgDeskRoom;
                LayoutCharmDialogue();
            }

            bool deskFontsChanged = Math.Abs(deskScale - _deskFontScale) > 0.01f;
            _deskFontScale = deskScale;
            LayoutDeskHeader(deskScale, deskFontsChanged);

            // 1. Desk Hotspot Placement (over the glowing terminal desk in maingameplay_bg.png)
            if (deskFontsChanged) _lblDeskHotspot.Font = FontManager.GetSmallCapsFont(14f * deskScale, FontStyle.Bold);
            var spotText = TextRenderer.MeasureText(_lblDeskHotspot.Text, _lblDeskHotspot.Font);
            int spotW = Math.Min(W - 32, Math.Max((int)(540 * deskScale), spotText.Width + (int)(48 * deskScale)));
            int spotH = Math.Max((int)(76 * deskScale), spotText.Height + (int)(24 * deskScale));
            _lblDeskHotspot.Size = new Size(spotW, spotH);
            _lblDeskHotspot.Location = new Point((W - spotW) / 2, (int)(H * 0.72f));

            if (_lblDeskBookHotspot != null)
            {
                if (deskFontsChanged) _lblDeskBookHotspot.Font = FontManager.GetSmallCapsFont(12.5f * deskScale, FontStyle.Bold);
                var bookText = TextRenderer.MeasureText(_lblDeskBookHotspot.Text, _lblDeskBookHotspot.Font);
                int bookW = Math.Min(W - 32, Math.Max((int)(460 * deskScale), bookText.Width + (int)(40 * deskScale)));
                int bookH = Math.Max((int)(44 * deskScale), bookText.Height + (int)(16 * deskScale));
                _lblDeskBookHotspot.Size = new Size(bookW, bookH);
                _lblDeskBookHotspot.Location = new Point((W - bookW) / 2, _lblDeskHotspot.Bottom + (int)(10 * deskScale));
            }

            // 2. CRT Screen Bounds inside Computer Terminal View (exact contain-fit alignment)
            int crtX, crtY, crtW, crtH;
            float crtScale;
            if (_imgComputerDesk != null)
            {
                var imgRect = GetContainFitRect(_imgComputerDesk, ClientRectangle);
                float factor = (float)imgRect.Width / (float)_imgComputerDesk.Width;

                if (_imgComputerDesk.Width == 657)
                {
                    // computerscreenforcall.png (657x506 native)
                    // Inner CRT display bounds: Left=28, Top=44, Width=597, Height=326
                    crtX = imgRect.X + (int)(28 * factor);
                    crtY = imgRect.Y + (int)(44 * factor);
                    crtW = (int)(597 * factor);
                    crtH = (int)(326 * factor);
                    crtScale = crtH / 630f;

                    int btnH = Math.Clamp((int)(24 * factor), 22, 34);
                    int topY = Math.Max(16, imgRect.Y + (int)(16 * factor));

                    _btnBackToDesk.Location = new Point(imgRect.X + (int)(575 * factor), topY);
                    _btnBackToDesk.Size = new Size((int)(55 * factor), btnH);

                    _btnTerminalSettings.Location = new Point(_btnBackToDesk.Left - (int)(105 * factor), topY);
                    _btnTerminalSettings.Size = new Size((int)(98 * factor), btnH);

                    _btnTerminalEndShift.Location = new Point(_btnTerminalSettings.Left - (int)(115 * factor), topY);
                    _btnTerminalEndShift.Size = new Size((int)(108 * factor), btnH);

                    // Position tactile sticky note over the drawn note in the bottom right of the desk
                    if (_lblDeskStickyNote != null)
                    {
                        if (deskFontsChanged) _lblDeskStickyNote.Font = FontManager.GetSmallCapsFont(Math.Max(6f, 3.6f * factor), FontStyle.Bold);
                        _lblDeskStickyNote.Location = new Point(imgRect.X + (int)(524 * factor), imgRect.Y + (int)(424 * factor));
                        _lblDeskStickyNote.Size = new Size((int)(64 * factor), (int)(34 * factor));
                    }
                }
                else
                {
                    // 1024x832 fallback (computer_desk_ui.jpg)
                    crtX = imgRect.X + (int)(55 * factor);
                    crtY = imgRect.Y + (int)(114 * factor);
                    crtW = (int)(909 * factor);
                    crtH = (int)(630 * factor);
                    crtScale = crtH / 630f;

                    _btnBackToDesk.Location = new Point(imgRect.X + (int)(920 * factor), imgRect.Y + (int)(78 * factor));
                    _btnBackToDesk.Size = new Size((int)(66 * factor), (int)(32 * factor));

                    _btnTerminalSettings.Location = new Point(_btnBackToDesk.Left - (int)(116 * factor), imgRect.Y + (int)(78 * factor));
                    _btnTerminalSettings.Size = new Size((int)(110 * factor), (int)(32 * factor));

                    _btnTerminalEndShift.Location = new Point(_btnTerminalSettings.Left - (int)(122 * factor), imgRect.Y + (int)(78 * factor));
                    _btnTerminalEndShift.Size = new Size((int)(116 * factor), (int)(32 * factor));
                }
            }
            else
            {
                crtX = (int)(W * 0.058f);
                crtY = (int)(H * 0.165f);
                crtW = (int)(W * 0.884f);
                crtH = (int)(H * 0.690f);
                crtScale = crtH / 630f;

                _btnBackToDesk.Location = new Point(W - 120, 20);
                _btnBackToDesk.Size = new Size(100, 36);

                _btnTerminalSettings.Location = new Point(_btnBackToDesk.Left - 130, 20);
                _btnTerminalSettings.Size = new Size(120, 36);

                _btnTerminalEndShift.Location = new Point(_btnTerminalSettings.Left - 140, 20);
                _btnTerminalEndShift.Size = new Size(130, 36);
            }

            _pnlCrtScreen.Location = new Point(crtX, crtY);
            _pnlCrtScreen.Size = new Size(crtW, crtH);

            float scale = crtScale;
            ApplyCrtFontScale(Math.Clamp(crtH / 695f, 0.6f, 2f));

            // Controls inside CRT Screen
            int pad = (int)(16 * scale);
            _lblCrtHeaderLeft.Location = new Point(pad, pad);
            _lblCrtHeaderCenter.Location = new Point((crtW - _lblCrtHeaderCenter.Width) / 2, pad);
            _lblCrtHeaderTimer.Location = new Point(crtW - _lblCrtHeaderTimer.Width - pad, pad);

            // Tutorial Protocol Checklist Strip (Across CRT under Header)
            int checkY = _lblCrtHeaderCenter.Bottom + (int)(4 * scale);
            int checkH = (int)(22 * scale);
            _lblProtocolChecklist.Location = new Point(pad, checkY);
            _lblProtocolChecklist.Size = new Size(crtW - pad * 2, checkH);

            // Left Queue Panel vs Right Active Console
            int queueW = (int)(crtW * 0.235f);
            int callX = pad + queueW + (int)(12 * scale);
            int callW = crtW - callX - pad;

            // Content Vertical Span: from topContentY down to bottomContentY (crtH - pad)
            int topContentY = _lblProtocolChecklist.Bottom + (int)(6 * scale);
            int bottomContentY = crtH - pad;

            // 1. Supervisor Teletype Container (Anchored at bottom of CRT display)
            int supH = Math.Clamp((int)(64 * scale), 54, 80);
            int supY = bottomContentY - supH;
            _pnlSupervisorContainer.Location = new Point(callX, supY);
            _pnlSupervisorContainer.Size = new Size(callW, supH);
            _pnlSupervisorContainer.BringToFront();

            int supScrollW = Math.Max(100, _pnlSupervisorScroll.ClientSize.Width > 0 ? _pnlSupervisorScroll.ClientSize.Width - 20 : callW - 46);
            _lblSupervisorBar.MaximumSize = new Size(supScrollW, 0);

            // 2. Action Choice Buttons (Anchored directly above supervisor bar)
            int btnAreaBottom = supY - (int)(8 * scale);
            int count = _currentChoices.Count;
            int actBtnH;
            int btnAreaH;
            int btnAreaY;

            if (count <= 3)
            {
                actBtnH = Math.Clamp((int)(72 * scale), 58, 88);
                btnAreaH = actBtnH;
                btnAreaY = btnAreaBottom - btnAreaH;

                if (count == 1)
                {
                    _actionButtons[0].Location = new Point(callX, btnAreaY);
                    _actionButtons[0].Size = new Size(callW, actBtnH);
                }
                else if (count == 2)
                {
                    int gap = (int)(12 * scale);
                    int btnW = (callW - gap) / 2;
                    _actionButtons[0].Location = new Point(callX, btnAreaY);
                    _actionButtons[0].Size = new Size(btnW, actBtnH);
                    _actionButtons[1].Location = new Point(callX + btnW + gap, btnAreaY);
                    _actionButtons[1].Size = new Size(btnW, actBtnH);
                }
                else if (count == 3)
                {
                    int gap = (int)(8 * scale);
                    int btnW = (callW - gap * 2) / 3;
                    for (int i = 0; i < 3; i++)
                    {
                        _actionButtons[i].Location = new Point(callX + i * (btnW + gap), btnAreaY);
                        _actionButtons[i].Size = new Size(btnW, actBtnH);
                    }
                }
            }
            else
            {
                actBtnH = Math.Clamp((int)(66 * scale), 52, 80);
                int btnGapY = (int)(6 * scale);
                btnAreaH = actBtnH * 2 + btnGapY;
                btnAreaY = btnAreaBottom - btnAreaH;

                int btnGapX = (int)(8 * scale);
                int btnW = (callW - btnGapX * 2) / 3;

                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int btnIdx = r * 3 + c;
                        _actionButtons[btnIdx].Location = new Point(callX + c * (btnW + btnGapX), btnAreaY + r * (actBtnH + btnGapY));
                        _actionButtons[btnIdx].Size = new Size(btnW, actBtnH);
                    }
                }
            }

            // 3. Telemetry Row (Anchored directly above action buttons)
            int telemH = Math.Clamp((int)(24 * scale), 20, 30);
            int telemY = btnAreaY - (int)(8 * scale) - telemH;
            _pnlTelemetry.Location = new Point(callX, telemY);
            _pnlTelemetry.Size = new Size(callW, telemH);

            int locW = (int)(callW * 0.44f);
            int sigW = (int)(callW * 0.24f);
            int truW = (int)(callW * 0.16f);
            int panW = callW - locW - sigW - truW;

            _lblLocation.Location = new Point(0, 0);
            _lblLocation.Size = new Size(locW, telemH);
            _lblLocation.AutoEllipsis = true;

            _lblSignal.Location = new Point(locW + (int)(4 * scale), 0);
            _lblSignal.Size = new Size(sigW, telemH);
            _lblSignal.AutoEllipsis = true;

            _lblTrust.Location = new Point(locW + sigW + (int)(4 * scale), 0);
            _lblTrust.Size = new Size(truW, telemH);

            _lblPanic.Location = new Point(locW + sigW + truW + (int)(4 * scale), 0);
            _lblPanic.Size = new Size(panW - (int)(4 * scale), telemH);

            // 4. Clue Banner & Active Call Box (Fully stretches to fill all upper vertical space)
            int callBoxTopY = topContentY;
            if (_lblClueBanner.Visible)
            {
                int bannerH = (int)(28 * scale);
                _lblClueBanner.Location = new Point(callX, callBoxTopY);
                _lblClueBanner.Size = new Size(callW, bannerH);
                callBoxTopY += bannerH + (int)(6 * scale);
            }
            else
            {
                _lblClueBanner.Size = Size.Empty;
            }

            int callH = Math.Max(120, telemY - (int)(8 * scale) - callBoxTopY);
            _pnlCallBox.Location = new Point(callX, callBoxTopY);
            _pnlCallBox.Size = new Size(callW, callH);

            int transW = Math.Max(100, _pnlTranscriptScroll.ClientSize.Width > 0 ? _pnlTranscriptScroll.ClientSize.Width - 28 : callW - 32);
            _lblCallTranscript.MaximumSize = new Size(transW, 0);

            // 5. Left Queue Panel (Fully stretches from topContentY all the way to bottomContentY)
            int queueH = bottomContentY - topContentY;
            _pnlQueueContainer.Location = new Point(pad, topContentY);
            _pnlQueueContainer.Size = new Size(queueW, queueH);

            // Queue Cards within Queue Panel (Evenly fills queue area)
            int headerH = _lblQueueHeader.Height;
            int footerH = _lblQueueFooter.Height;
            int availableCardsH = queueH - headerH - footerH - 14;
            int cardSpacing = Math.Clamp((int)(8 * scale), 4, 12);
            int cardCount = Math.Max(1, _queueCards.Count);
            int cardH = Math.Clamp((availableCardsH - cardSpacing * (cardCount - 1)) / cardCount, (int)(54 * scale), (int)(85 * scale));
            int cardStartY = headerH + 6;

            for (int i = 0; i < _queueCards.Count; i++)
            {
                var card = _queueCards[i];
                card.Location = new Point(4, cardStartY + i * (cardH + cardSpacing));
                card.Size = new Size(queueW - 8, cardH);
                if (card.Controls.Count >= 3)
                {
                    // Number and badge are sized from their text so "01" and "RESTRICTED" never wrap or clip
                    int numW = TextRenderer.MeasureText("00", card.Controls[0].Font).Width + (int)(6 * scale);
                    int badgeW = TextRenderer.MeasureText("RESTRICTED", card.Controls[2].Font).Width + 4;
                    int badgeH = card.Controls[2].Font.Height + 4;
                    card.Controls[0].Location = new Point(2, 0);
                    card.Controls[0].Size = new Size(numW, cardH - 2);
                    card.Controls[1].Location = new Point(numW + 4, (int)(6 * scale));
                    card.Controls[1].Size = new Size(Math.Max(20, card.Width - numW - badgeW - 14), cardH - (int)(10 * scale));
                    card.Controls[2].Location = new Point(card.Width - badgeW - 6, (cardH - badgeH) / 2);
                    card.Controls[2].Size = new Size(badgeW, badgeH);
                }
            }

            UpdateTutorialGuidance();
            Invalidate();
        }

        public void ShowDebriefModal()
        {
            AudioManager.Instance.StopPhoneRing();
            _scriptSegments.Clear();
            _charmDialogue.Clear();
            _pnlCrtScreen.Visible = false;
            _pnlDeskHeader.Visible = false;
            _lblDeskHotspot.Visible = false;
            _btnBackToDesk.Visible = false;
            _btnTerminalSettings.Visible = false;
            _btnTerminalEndShift.Visible = false;
            if (_lblDeskStickyNote != null) _lblDeskStickyNote.Visible = false;
            _shiftTimer.Stop();
            _panicDecayTimer.Stop();
            _typewriterTimer.Stop();
            _tutorialPulseTimer.Stop();

            if (_pnlDebriefModal != null)
            {
                _pnlDebriefModal.Bounds = ClientRectangle;
                _pnlDebriefModal.BringToFront();
                _pnlDebriefModal.Visible = true;
                _pnlDebriefModal.Invalidate();
                return;
            }

            AudioManager.Instance.PlayInkStamp();

            // The modal container fills the entire screen so that endshiftsummary_bg.png forms the background
            _pnlDebriefModal = new Panel
            {
                Bounds = ClientRectangle,
                BackColor = Color.FromArgb(20, 10, 10)
            };
            EnableDoubleBuffering(_pnlDebriefModal);

            // Calculate metrics
            int avgTrust = Math.Max(70, (_calls[0].TrustPercentage + _calls[1].TrustPercentage + _calls[2].TrustPercentage) / 3);
            int callsHandled = Math.Min(3, State.CallsLogged);
            if (callsHandled < 3 && _calls[0].Stage == CallStage.Resolved && _calls[1].Stage == CallStage.Resolved && _calls[2].Stage == CallStage.Resolved)
            {
                callsHandled = 3;
            }

            // Paint the background and physical wooden clipboard
            _pnlDebriefModal.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                var rc = _pnlDebriefModal.ClientRectangle;
                if (rc.Width <= 0 || rc.Height <= 0) return;

                // 1. Draw endshiftsummary_bg.png background
                if (_imgEndShiftBg != null)
                {
                    DrawImageCoverFit(g, _imgEndShiftBg, rc);
                }
                else if (_imgDeskRoom != null)
                {
                    DrawImageCoverFit(g, _imgDeskRoom, rc);
                }

                // Subtle atmospheric vignette wash
                using (var washBrush = new SolidBrush(Color.FromArgb(85, 6, 8, 6)))
                {
                    g.FillRectangle(washBrush, rc);
                }

                float scale = (float)rc.Height / 1080f;
                float drawW = 1920f * scale;
                float drawH = 1080f * scale;
                float offX = (rc.Width - drawW) / 2f;
                float offY = (rc.Height - drawH) / 2f;

                int paperX, paperW, paperTop, paperBottom;

                if (_imgEndShiftBoard != null)
                {
                    // Draw the photorealistic wooden clipboard directly under the overhead light
                    g.DrawImage(_imgEndShiftBoard, offX, offY, drawW, drawH);

                    // Parchment area inside the 1920x1080 board asset (Left=640, Width=600, Top=225, Bottom=980)
                    paperX = (int)(offX + 640 * scale);
                    paperW = (int)(600 * scale);
                    paperTop = (int)(offY + 225 * scale);
                    paperBottom = (int)(offY + 980 * scale);
                }
                else
                {
                    // Fallback procedural board
                    int clipW = Math.Clamp((int)(rc.Width * 0.50f), 540, 680);
                    int clipH = Math.Clamp((int)(rc.Height * 0.88f), 600, 780);
                    int clipX = (rc.Width - clipW) / 2;
                    int clipY = (rc.Height - clipH) / 2 + 10;
                    var clipRect = new Rectangle(clipX, clipY, clipW, clipH);

                    using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                    {
                        g.FillRectangle(shadowBrush, clipX + 8, clipY + 8, clipW, clipH);
                    }

                    using (var woodBrush = new SolidBrush(Color.FromArgb(48, 30, 20)))
                    using (var woodBorderPen = new Pen(Color.FromArgb(28, 16, 10), 3f))
                    {
                        g.FillRectangle(woodBrush, clipRect);
                        g.DrawRectangle(woodBorderPen, clipRect);
                    }

                    int paperMarginX = 20;
                    int paperMarginY = 38;
                    paperX = clipX + paperMarginX;
                    paperW = clipW - paperMarginX * 2;
                    paperTop = clipY + paperMarginY;
                    paperBottom = clipY + clipH - 24;
                    var paperRect = new Rectangle(paperX, paperTop, paperW, paperBottom - paperTop);

                    using (var paperBrush = new SolidBrush(Color.FromArgb(238, 226, 198)))
                    using (var paperBorderPen = new Pen(Color.FromArgb(195, 180, 150), 1.5f))
                    {
                        g.FillRectangle(paperBrush, paperRect);
                        g.DrawRectangle(paperBorderPen, paperRect);
                    }

                    int clipWidth = (int)(clipW * 0.38f);
                    int clipHeight = 36;
                    int clipPosX = clipX + (clipW - clipWidth) / 2;
                    int clipPosY = clipY - 14;
                    var metalRect = new Rectangle(clipPosX, clipPosY, clipWidth, clipHeight);

                    using (var clipShadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                    {
                        g.FillRectangle(clipShadowBrush, clipPosX + 3, clipPosY + 3, clipWidth, clipHeight);
                    }
                    using (var metalBrush = new LinearGradientBrush(metalRect, Color.FromArgb(180, 184, 188), Color.FromArgb(90, 94, 98), LinearGradientMode.Vertical))
                    using (var metalPen = new Pen(Color.FromArgb(50, 52, 54), 1.5f))
                    {
                        g.FillRectangle(metalBrush, metalRect);
                        g.DrawRectangle(metalPen, metalRect);
                    }
                }

                // Header Typography on Parchment
                int contentX = paperX + (int)(24 * scale);
                int contentW = paperW - (int)(48 * scale);
                int currY = paperTop + (int)(8 * scale);

                // Confidential Subheading
                using var fontSub = FontManager.GetSmallCapsFont(10.5f * scale, FontStyle.Regular);
                var subCol = Color.FromArgb(105, 90, 75);
                var subRect = new Rectangle(contentX, currY, contentW, (int)(20 * scale));
                TextRenderer.DrawText(g, "Municipal Emergency Services · Confidential", fontSub, subRect, subCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
                currY += (int)(22 * scale);

                // "END OF SHIFT" Main Title
                using var fontTitle = FontManager.GetSmallCapsFont(23f * scale, FontStyle.Bold);
                var titleCol = Color.FromArgb(24, 20, 16);
                var titleRect = new Rectangle(contentX, currY, contentW, (int)(38 * scale));
                TextRenderer.DrawText(g, "END OF SHIFT REPORT", fontTitle, titleRect, titleCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
                currY += (int)(40 * scale);

                // Employee & Date Subheader
                using var fontEmp = FontManager.GetSmallCapsFont(11.5f * scale, FontStyle.Regular);
                var empCol = Color.FromArgb(80, 70, 60);
                var empRect = new Rectangle(contentX, currY, contentW, (int)(22 * scale));
                TextRenderer.DrawText(g, $"Employee: {Profile.OperatorName.ToUpperInvariant()} · November 9, 1998 · Station 04", fontEmp, empRect, empCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
                currY += (int)(30 * scale);

                // The 2x3 Metric Table Grid
                int gridX = contentX;
                int gridY = currY;
                int gridW = contentW;
                int gridH = (int)(138 * scale);
                int colW = gridW / 2;
                int rowH = gridH / 3;

                using var gridPen = new Pen(Color.FromArgb(150, 130, 110), 1.5f * scale);
                using var cellBgBrush = new SolidBrush(Color.FromArgb(110, 245, 237, 218));
                g.FillRectangle(cellBgBrush, gridX, gridY, gridW, gridH);
                g.DrawRectangle(gridPen, gridX, gridY, gridW, gridH);

                // Grid lines
                g.DrawLine(gridPen, gridX + colW, gridY, gridX + colW, gridY + gridH);
                g.DrawLine(gridPen, gridX, gridY + rowH, gridX + gridW, gridY + rowH);
                g.DrawLine(gridPen, gridX, gridY + rowH * 2, gridX + gridW, gridY + rowH * 2);

                using var fontMetricLabel = FontManager.GetSmallCapsFont(9.5f * scale, FontStyle.Regular);
                using var fontMetricVal = FontManager.GetSmallCapsFont(14.5f * scale, FontStyle.Bold);
                var labelCol = Color.FromArgb(110, 95, 80);
                var valCol = Color.FromArgb(24, 20, 16);

                void DrawCell(int r, int c, string label, string val)
                {
                    int cx = gridX + c * colW + (int)(12 * scale);
                    int cy = gridY + r * rowH + (int)(5 * scale);
                    var rLabel = new Rectangle(cx, cy, colW - (int)(24 * scale), (int)(16 * scale));
                    var rVal = new Rectangle(cx, cy + (int)(17 * scale), colW - (int)(24 * scale), (int)(22 * scale));
                    TextRenderer.DrawText(g, label, fontMetricLabel, rLabel, labelCol, TextFormatFlags.Left);
                    TextRenderer.DrawText(g, val, fontMetricVal, rVal, valCol, TextFormatFlags.Left);
                }

                int totalClues = 0;
                foreach (var c in _calls) { if (c.HasDiscrepancy) totalClues++; }
                if (totalClues == 0) totalClues = 1;

                DrawCell(0, 0, "Calls handled", $"{callsHandled:D2} / {_calls.Count:D2}");
                DrawCell(0, 1, "Correct emergency", $"{callsHandled:D2} / {_calls.Count:D2}");
                DrawCell(1, 0, "Protocol errors", $"{State.Errors:D2}");
                DrawCell(1, 1, "Units remaining", $"{State.AvailableUnits:D2} / {Scenario.InitialUnits:D2}");
                DrawCell(2, 0, "Caller trust", $"{avgTrust}%");
                DrawCell(2, 1, "Evidence found", $"{State.CluesFound:D2} / {totalClues:D2}");

                currY += gridH + (int)(20 * scale);

                // Supervisor Note Title & Stamp Row
                using var fontNoteHeader = FontManager.GetSmallCapsFont(11.5f * scale, FontStyle.Bold);
                var noteHeadCol = Color.FromArgb(46, 38, 30);
                var rNoteHead = new Rectangle(contentX, currY, (int)(contentW * 0.60f), (int)(20 * scale));
                TextRenderer.DrawText(g, "Supervisor note", fontNoteHeader, rNoteHead, noteHeadCol, TextFormatFlags.Left);

                // Angled Red Rubber Stamp (renders cleanly via GDI+ DrawString)
                int stampW = (int)(168 * scale);
                int stampH = (int)(46 * scale);
                int stampX = contentX + contentW - stampW - (int)(6 * scale);
                int stampY = currY - (int)(4 * scale);

                var stampState = g.Save();
                g.TranslateTransform(stampX + stampW / 2f, stampY + stampH / 2f);
                g.RotateTransform(-6f);
                var rStampLocal = new Rectangle(-stampW / 2, -stampH / 2, stampW, stampH);

                using var stampPen = new Pen(Color.FromArgb(195, 36, 36), 2.5f * scale);
                g.DrawRectangle(stampPen, rStampLocal);

                var rStampInner = new Rectangle(-stampW / 2 + (int)(3 * scale), -stampH / 2 + (int)(3 * scale), stampW - (int)(6 * scale), stampH - (int)(6 * scale));
                using var innerPen = new Pen(Color.FromArgb(130, 195, 36, 36), 1.2f * scale);
                g.DrawRectangle(innerPen, rStampInner);

                using var fontStamp = FontManager.GetSmallCapsFont(12.5f * scale, FontStyle.Bold);
                using var stampBrush = new SolidBrush(Color.FromArgb(210, 195, 36, 36));
                using var sfStamp = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                string stampText = callsHandled >= 3 ? "VERIFIED & PASSED" : "UNDER REVIEW";
                g.DrawString(stampText, fontStamp, stampBrush, rStampLocal, sfStamp);
                g.Restore(stampState);

                currY += (int)(22 * scale);

                // Supervisor Note Body
                using var fontNoteBody = FontManager.GetItalicFont(12.5f * scale);
                var noteBodyCol = Color.FromArgb(34, 26, 20);
                int noteW = contentW - stampW - (int)(16 * scale);
                string supervisorNote = Scenario.GetDebriefMessage(State);
                int noteH = Math.Max((int)(78 * scale), (int)Math.Ceiling(g.MeasureString(supervisorNote, fontNoteBody, noteW).Height) + (int)(4 * scale));
                var rNoteBody = new Rectangle(contentX, currY, noteW, noteH);
                using var noteBrush = new SolidBrush(noteBodyCol);
                using var noteSf = new StringFormat { Trimming = StringTrimming.Word };
                g.DrawString(supervisorNote, fontNoteBody, noteBrush, rNoteBody, noteSf);

                currY += noteH + (int)(12 * scale);

                // Incident Disposition Ledger (Utilizes paper body with atmospheric log details)
                using var fontLedgerHead = FontManager.GetSmallCapsFont(10.5f * scale, FontStyle.Bold);
                var rLedgerHead = new Rectangle(contentX, currY, contentW, (int)(18 * scale));
                TextRenderer.DrawText(g, "EMERGENCY TRUNK LOG & DISPOSITION SUMMARY", fontLedgerHead, rLedgerHead, Color.FromArgb(75, 62, 50), TextFormatFlags.Left);
                currY += (int)(18 * scale);

                using var divPen = new Pen(Color.FromArgb(160, 140, 120), 1.2f * scale);
                g.DrawLine(divPen, contentX, currY, contentX + contentW, currY);
                currY += (int)(4 * scale);

                using var fontLedgerRow = FontManager.GetSmallCapsFont(9.5f * scale, FontStyle.Regular);
                using var fontLedgerBold = FontManager.GetSmallCapsFont(9.5f * scale, FontStyle.Bold);
                int rowH2 = (int)(19 * scale);

                for (int i = 0; i < _calls.Count; i++)
                {
                    var c = _calls[i];
                    int ry = currY + i * rowH2;
                    var rRow = new Rectangle(contentX + (int)(4 * scale), ry, contentW - (int)(8 * scale), rowH2);

                    string place = c.IsRestricted ? "SOURCE CLASSIFIED" : c.AddressVerified ? c.Location : "LOCATION UNVERIFIED";
                    string linePrefix = c.IsRestricted
                        ? $"LINE {(i + 1):D2}: {c.CallerName}"
                        : $"LINE {(i + 1):D2}: {c.CallerName} — {place}";
                    string lineOutcome;
                    Color outcomeCol;

                    if (c.IsRestricted)
                    {
                        lineOutcome = "[ CIVIL DEFENSE RESTRICTION ]";
                        outcomeCol = Color.FromArgb(175, 45, 45);
                    }
                    else if (c.Stage == CallStage.Resolved || c.Status == CallQueueStatus.Resolved)
                    {
                        if (c.UnitDispatched)
                        {
                            lineOutcome = "[ CODE 3 PATROL DISPATCHED ]";
                            outcomeCol = Color.FromArgb(45, 115, 55);
                        }
                        else
                        {
                            lineOutcome = "[ INCIDENT LOGGED / RESOLVED ]";
                            outcomeCol = Color.FromArgb(45, 115, 55);
                        }
                    }
                    else
                    {
                        lineOutcome = "[ PENDING / UNRESOLVED ]";
                        outcomeCol = Color.FromArgb(160, 110, 30);
                    }

                    int outcomeW = TextRenderer.MeasureText(lineOutcome, fontLedgerBold).Width;
                    var rPrefix = new Rectangle(rRow.X, rRow.Y, Math.Max(10, rRow.Width - outcomeW - (int)(10 * scale)), rRow.Height);
                    TextRenderer.DrawText(g, linePrefix, fontLedgerRow, rPrefix, Color.FromArgb(50, 42, 35), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    TextRenderer.DrawText(g, lineOutcome, fontLedgerBold, rRow, outcomeCol, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                }

                currY += _calls.Count * rowH2 + (int)(8 * scale);

                // Verifying Supervisor Signature & Confidential Notice
                g.DrawLine(divPen, contentX, currY, contentX + contentW, currY);
                currY += (int)(6 * scale);

                using var fontNotice = FontManager.GetItalicFont(9f * scale);
                var rNotice = new Rectangle(contentX, currY, (int)(contentW * 0.58f), (int)(34 * scale));
                string noticeText = "NOTICE: Official property of Municipal Emergency Services.\nUnauthorized disclosure of audio logs is prohibited under Code 402.";
                using var noticeBrush = new SolidBrush(Color.FromArgb(115, 100, 85));
                g.DrawString(noticeText, fontNotice, noticeBrush, rNotice);

                int sigX = contentX + (int)(contentW * 0.60f);
                int sigW = contentW - (int)(contentW * 0.60f);
                using var fontSigTitle = FontManager.GetSmallCapsFont(8.5f * scale, FontStyle.Regular);
                TextRenderer.DrawText(g, "VERIFYING SUPERVISOR:", fontSigTitle, new Rectangle(sigX, currY, sigW, (int)(14 * scale)), Color.FromArgb(115, 100, 85), TextFormatFlags.Right);

                using var fontSig = FontManager.GetItalicFont(14.5f * scale);
                string supervisorSignature = $"{System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Scenario.SupervisorName.ToLowerInvariant())}, Supv.";
                TextRenderer.DrawText(g, supervisorSignature, fontSig, new Rectangle(sigX, currY + (int)(14 * scale), sigW, (int)(22 * scale)), Color.FromArgb(30, 22, 55), TextFormatFlags.Right);
            };

            // Action Button: [ Complete Shift ]
            var btnComplete = new Label
            {
                Text = "[ COMPLETE SHIFT ]",
                Cursor = Cursors.Hand
            };

            // Secondary Button: [ Return to Terminal ]
            var btnReturn = new Label
            {
                Text = "[ RETURN TO TERMINAL ]",
                Cursor = Cursors.Hand
            };

            btnComplete.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rc = new Rectangle(0, 0, btnComplete.Width - 1, btnComplete.Height - 1);
                bool isHovered = btnComplete.ClientRectangle.Contains(btnComplete.PointToClient(Cursor.Position));
                using var bgBrush = new SolidBrush(isHovered ? Color.FromArgb(45, 36, 26) : Color.FromArgb(235, 222, 196));
                using var pen = new Pen(isHovered ? Color.FromArgb(20, 14, 10) : Color.FromArgb(120, 100, 80), 1.5f);
                e.Graphics.FillRectangle(bgBrush, rc);
                e.Graphics.DrawRectangle(pen, rc);

                float scale = (float)_pnlDebriefModal.Height / 1080f;
                using var font = FontManager.GetSmallCapsFont(11f * scale, FontStyle.Bold);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var textBrush = new SolidBrush(isHovered ? Color.FromArgb(245, 235, 215) : Color.FromArgb(28, 24, 20));
                e.Graphics.DrawString("[ COMPLETE SHIFT ]", font, textBrush, rc, sf);
            };

            btnReturn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rc = new Rectangle(0, 0, btnReturn.Width - 1, btnReturn.Height - 1);
                bool isHovered = btnReturn.ClientRectangle.Contains(btnReturn.PointToClient(Cursor.Position));
                using var bgBrush = new SolidBrush(isHovered ? Color.FromArgb(60, 50, 40) : Color.FromArgb(220, 208, 184));
                using var pen = new Pen(isHovered ? Color.FromArgb(30, 22, 16) : Color.FromArgb(135, 118, 98), 1.5f);
                e.Graphics.FillRectangle(bgBrush, rc);
                e.Graphics.DrawRectangle(pen, rc);

                float scale = (float)_pnlDebriefModal.Height / 1080f;
                using var font = FontManager.GetSmallCapsFont(10.5f * scale, FontStyle.Bold);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var textBrush = new SolidBrush(isHovered ? Color.FromArgb(245, 235, 215) : Color.FromArgb(65, 55, 45));
                e.Graphics.DrawString("[ RETURN TO TERMINAL ]", font, textBrush, rc, sf);
            };

            void PositionButtons()
            {
                var rc = _pnlDebriefModal.ClientRectangle;
                if (rc.Width <= 0 || rc.Height <= 0) return;

                float scale = (float)rc.Height / 1080f;
                float drawW = 1920f * scale;
                float drawH = 1080f * scale;
                float offX = (rc.Width - drawW) / 2f;
                float offY = (rc.Height - drawH) / 2f;

                int paperX, paperW, paperBottom;
                if (_imgEndShiftBoard != null)
                {
                    paperX = (int)(offX + 640 * scale);
                    paperW = (int)(600 * scale);
                    paperBottom = (int)(offY + 980 * scale);
                }
                else
                {
                    int clipW = Math.Clamp((int)(rc.Width * 0.50f), 540, 680);
                    int clipH = Math.Clamp((int)(rc.Height * 0.88f), 600, 780);
                    int clipX = (rc.Width - clipW) / 2;
                    int clipY = (rc.Height - clipH) / 2 + 10;
                    paperX = clipX + 20;
                    paperW = clipW - 40;
                    paperBottom = clipY + clipH - 24;
                }

                int contentX = paperX + (int)(24 * scale);
                int contentW = paperW - (int)(48 * scale);
                int gap = (int)(14 * scale);
                int btnW = (contentW - gap) / 2;
                int btnH = Math.Clamp((int)(44 * scale), 38, 48);
                int btnY = paperBottom - btnH - (int)(16 * scale);

                btnComplete.Location = new Point(contentX, btnY);
                btnComplete.Size = new Size(btnW, btnH);

                btnReturn.Location = new Point(contentX + btnW + gap, btnY);
                btnReturn.Size = new Size(btnW, btnH);
            }

            PositionButtons();
            _pnlDebriefModal.Resize += (s, e) =>
            {
                PositionButtons();
                _pnlDebriefModal.Invalidate();
            };

            btnComplete.MouseEnter += (s, e) =>
            {
                btnComplete.Invalidate();
                AudioManager.Instance.PlayPaperRustle();
            };
            btnComplete.MouseLeave += (s, e) => btnComplete.Invalidate();

            btnComplete.Click += (s, e) =>
            {
                if (callsHandled < 3)
                {
                    bool endEarly = ParchmentNoticeDialog.Confirm(
                        this,
                        "MUNICIPAL EMERGENCY SERVICES // EARLY SUBMISSION",
                        "End your shift early?",
                        "You still have pending emergency trunk lines on the switchboard. Submitting an incomplete report will log every unhandled line as an infraction.",
                        "SUBMIT REPORT",
                        "KEEP WORKING");

                    if (!endEarly) return;
                }

                Profile.AdvanceShift(newCalls: State.CallsLogged, errors: State.Errors);
                Profile.CurrentShift = Scenario.ShiftNumber + 1;
                Profile.TutorialCompleted = true;
                SaveManager.Instance.SaveProfile(Profile);
                AudioManager.Instance.PlayInkStamp();

                ParchmentNoticeDialog.ShowNotice(
                    this,
                    "NIGHT DESK PERSONNEL LEDGER",
                    $"Shift {Scenario.ShiftNumber:D2} filed.",
                    $"Your assignment to Shift {Scenario.ShiftNumber + 1:D2} has been registered in the station ledger.",
                    "CLOCK OUT");

                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            btnReturn.MouseEnter += (s, e) =>
            {
                btnReturn.Invalidate();
                AudioManager.Instance.PlayPaperRustle();
            };
            btnReturn.MouseLeave += (s, e) => btnReturn.Invalidate();

            btnReturn.Click += (s, e) =>
            {
                _pnlDebriefModal.Visible = false;
                _shiftTimer.Start();
                if (_activeCallIndex == 1) _panicDecayTimer.Start();
                if (Scenario.HasTutorialGuidance) _tutorialPulseTimer.Start();
                SwitchView(ViewState.ComputerTerminal);
            };

            _pnlDebriefModal.Controls.Add(btnComplete);
            _pnlDebriefModal.Controls.Add(btnReturn);
            Controls.Add(_pnlDebriefModal);
            _pnlDebriefModal.BringToFront();
        }

        private static void EnableDoubleBuffering(Control control)
        {
            try
            {
                typeof(Control).InvokeMember(
                    "DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic,
                    null, control, new object[] { true });
            }
            catch { }
        }

        private const TextFormatFlags BookTextFlags = TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        private static readonly Color BookInkDark = Color.FromArgb(28, 22, 16);
        private static readonly Color BookInkSub = Color.FromArgb(78, 62, 46);

        /// <summary>Draws (or just measures) wrapped text and returns the Y just below it.</summary>
        private static int DrawBookText(Graphics g, string text, Font font, Color color, int x, int y, int width, bool draw)
        {
            var size = TextRenderer.MeasureText(g, text, font, new Size(width, int.MaxValue), BookTextFlags);
            if (draw)
                TextRenderer.DrawText(g, text, font, new Rectangle(x, y, width, size.Height + 2), color, BookTextFlags);
            return y + size.Height;
        }

        private static int DrawDirectiveLeftPage(Graphics g, Rectangle page, float s, bool draw)
        {
            using var fontTitle = FontManager.GetSmallCapsFont(25f * s, FontStyle.Bold);
            using var fontSub = FontManager.GetItalicFont(15f * s);
            using var fontSection = FontManager.GetSmallCapsFont(18f * s, FontStyle.Bold);
            using var fontBody = FontManager.GetSmallCapsFont(15.5f * s, FontStyle.Regular);

            int x = page.X, w = page.Width, indent = (int)(14 * s);
            int y = page.Y;

            y = DrawBookText(g, "MUNICIPAL DISPATCH PROTOCOLS", fontTitle, BookInkDark, x, y, w, draw);
            y = DrawBookText(g, "Civil Defense Communications Branch · Field Reference Directive", fontSub, BookInkSub, x, y, w, draw);
            y += (int)(22 * s);

            y = DrawBookText(g, "SECTION A: STANDARD RADIO 10-CODES", fontSection, BookInkDark, x, y, w, draw);
            y += (int)(6 * s);
            string[] codes = {
                "10-04  Acknowledgment / Directive Received",
                "10-07  Unit Out of Service (Mechanical / Transit)",
                "10-14  Escort / Patrol Reinforcement Required",
                "10-20  Transmit Exact Grid Coordinates / Location",
                "10-33  Emergency Traffic Only (Channel Clearance)",
                "10-99  Officer / Civilian Life-Threat Disturbance"
            };
            foreach (var code in codes)
            {
                y = DrawBookText(g, code, fontBody, BookInkDark, x + indent, y, w - indent, draw);
                y += (int)(5 * s);
            }

            y += (int)(20 * s);
            y = DrawBookText(g, "SECTION B: PRECINCT SECTORS & DEPLOYMENT", fontSection, BookInkDark, x, y, w, draw);
            y += (int)(6 * s);
            string[] sectors = {
                "SECTOR ALPHA  (West Residential / Bell St)  ·  Units 01, 02",
                "SECTOR BETA  (Commercial Route 9 Corridor)  ·  Unit 03",
                "SECTOR GAMMA  (Industrial Basin & River)  ·  Unit 04"
            };
            foreach (var sec in sectors)
            {
                y = DrawBookText(g, sec, fontBody, BookInkDark, x + indent, y, w - indent, draw);
                y += (int)(5 * s);
            }

            y += (int)(20 * s);
            y = DrawBookText(g, "PRIORITY DEPLOYMENT ORDER", fontSection, BookInkDark, x, y, w, draw);
            y += (int)(4 * s);
            y = DrawBookText(g, "Immediate unit deployment is authorized ONLY upon address and incident confirmation. False dispatches deplete limited night vehicles.",
                fontSub, BookInkSub, x + indent, y, w - indent, draw);
            return y;
        }

        private int DrawDirectiveRightPage(Graphics g, Rectangle page, float s, bool draw)
        {
            using var fontTitle = FontManager.GetSmallCapsFont(25f * s, FontStyle.Bold);
            using var fontSub = FontManager.GetItalicFont(15f * s);
            using var fontRule = FontManager.GetSmallCapsFont(16.5f * s, FontStyle.Bold);
            using var fontRuleBody = FontManager.GetItalicFont(14.5f * s);

            int x = page.X, w = page.Width, indent = (int)(14 * s);
            int y = page.Y;

            y = DrawBookText(g, "NIGHT DESK ORDERS & MANDATES", fontTitle, BookInkDark, x, y, w, draw);
            y = DrawBookText(g, $"Station Supervisor: {Scenario.SupervisorName} · Terminal 04 Directive", fontSub, BookInkSub, x, y, w, draw);
            y += (int)(22 * s);

            string[] rules = {
                "RULE 01: IDENTITY & RESIDENCE VERIFICATION",
                "Always confirm caller name and street address prior to unit deployment. Unverified dispatches count as protocol infractions.",
                "RULE 02: ARCHIVE CROSS-EXAMINATION",
                "Consult station log archives on all incoming lines. Check prior incident records and notes for recurring disturbances.",
                "RULE 03: TRUNK LINE 08 BAN (CRITICAL WARNING)",
                "Under NO circumstances may an operator answer, hold, or trace Trunk Line 08. That channel is sealed. Report any activity directly to your supervisor.",
                "RULE 04: DISTRESS DE-ESCALATION",
                "When telemetry indicates severe caller panic (>75%), maintain verbal reassurance protocol before issuing movement orders.",
                "RULE 05: SHIFT QUARANTINE PROTOCOL",
                "Do not discuss unconfirmed night anomalies or purged records with oncoming day shift operators. What occurs on night desk stays on night desk."
            };

            for (int i = 0; i < rules.Length; i += 2)
            {
                y = DrawBookText(g, rules[i], fontRule, BookInkDark, x, y, w, draw);
                y += (int)(2 * s);
                y = DrawBookText(g, rules[i + 1], fontRuleBody, BookInkSub, x + indent, y, w - indent, draw);
                y += (int)(16 * s);
            }

            // Official verification stamp, sized to its text
            y += (int)(8 * s);
            using var fontStamp = FontManager.GetSmallCapsFont(14f * s, FontStyle.Bold);
            string stampText = $"OFFICIAL DIRECTIVE — CIVIL DEFENSE // {Scenario.SupervisorName}";
            var stampSize = TextRenderer.MeasureText(g, stampText, fontStamp, Size.Empty, TextFormatFlags.NoPrefix);
            int stampW = Math.Min(w, stampSize.Width + (int)(28 * s));
            int stampH = stampSize.Height + (int)(14 * s);
            if (draw)
            {
                var stampRect = new Rectangle(x + (w - stampW) / 2, y, stampW, stampH);
                var state = g.Save();
                g.TranslateTransform(stampRect.X + stampW / 2f, stampRect.Y + stampH / 2f);
                g.RotateTransform(-2f);
                var local = new Rectangle(-stampW / 2, -stampH / 2, stampW, stampH);
                using var stampPen = new Pen(Color.FromArgb(160, 40, 40), Math.Max(1.5f, 2f * s));
                g.DrawRectangle(stampPen, local);
                // GDI+ DrawString honours the rotation (TextRenderer ignores world transforms)
                using var stampBrush = new SolidBrush(Color.FromArgb(160, 40, 40));
                using var stampFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(stampText, fontStamp, stampBrush, local, stampFormat);
                g.Restore(state);
            }
            return y + stampH;
        }

        private void ShowDirectiveManualModal()
        {
            if (_pnlManualModal != null)
            {
                _pnlManualModal.Visible = true;
                _pnlManualModal.BringToFront();
                return;
            }

            AudioManager.Instance.PlayPaperRustle();

            _pnlManualModal = new Panel
            {
                Bounds = ClientRectangle,
                BackColor = Color.FromArgb(20, 10, 8)
            };
            EnableDoubleBuffering(_pnlManualModal);

            _pnlManualModal.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rc = _pnlManualModal.ClientRectangle;
                if (rc.Width <= 0 || rc.Height <= 0) return;

                if (_imgDeskRoom != null)
                {
                    DrawImageCoverFit(g, _imgDeskRoom, rc);
                }

                // Dark atmospheric wash
                using (var washBrush = new SolidBrush(Color.FromArgb(170, 4, 8, 4)))
                {
                    g.FillRectangle(washBrush, rc);
                }

                if (_imgShiftBriefingBook != null)
                {
                    var bookRect = GetContainFitRect(_imgShiftBriefingBook, rc);
                    g.DrawImage(_imgShiftBriefingBook, bookRect);

                    float scale = (float)bookRect.Width / 1920f;

                    // Pages inside the open binder, in 1920x1080 book-image units (inside the paper margins)
                    var leftPage = new Rectangle(bookRect.X + (int)(380 * scale), bookRect.Y + (int)(150 * scale), (int)(580 * scale), (int)(770 * scale));
                    var rightPage = new Rectangle(bookRect.X + (int)(1045 * scale), bookRect.Y + (int)(150 * scale), (int)(560 * scale), (int)(770 * scale));

                    // Lay the pages out at a comfortable reading size, shrinking only if a page would overflow
                    float textScale = 1f;
                    while (textScale > 0.6f &&
                           (DrawDirectiveLeftPage(g, leftPage, scale * textScale, draw: false) > leftPage.Bottom ||
                            DrawDirectiveRightPage(g, rightPage, scale * textScale, draw: false) > rightPage.Bottom))
                    {
                        textScale -= 0.05f;
                    }

                    DrawDirectiveLeftPage(g, leftPage, scale * textScale, draw: true);
                    DrawDirectiveRightPage(g, rightPage, scale * textScale, draw: true);
                }
            };

            // Close button: [ Close Directive Manual ]
            var btnCloseManual = new Label
            {
                Text = "[ CLOSE DIRECTIVE MANUAL / ESC ]",
                ForeColor = Color.FromArgb(28, 22, 16),
                BackColor = Color.FromArgb(235, 220, 190),
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontManager.GetSmallCapsFont(12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(300, 46),
                Cursor = Cursors.Hand
            };
            btnCloseManual.MouseEnter += (s, e) =>
            {
                btnCloseManual.BackColor = Color.FromArgb(48, 36, 26);
                btnCloseManual.ForeColor = Color.FromArgb(245, 235, 215);
                AudioManager.Instance.PlayKeyClick();
            };
            btnCloseManual.MouseLeave += (s, e) =>
            {
                btnCloseManual.BackColor = Color.FromArgb(235, 220, 190);
                btnCloseManual.ForeColor = Color.FromArgb(28, 22, 16);
            };
            btnCloseManual.Click += (s, e) =>
            {
                AudioManager.Instance.PlayPaperRustle();
                _pnlManualModal.Visible = false;
            };

            void PositionManualButton()
            {
                if (_pnlManualModal.Width <= 0 || _pnlManualModal.Height <= 0) return;
                float scale = _pnlManualModal.Height / 1080f;
                btnCloseManual.Font = FontManager.GetSmallCapsFont(Math.Max(9f, 11.5f * scale), FontStyle.Bold);
                var textSize = TextRenderer.MeasureText(btnCloseManual.Text, btnCloseManual.Font);
                int bW = Math.Max((int)(300 * scale), textSize.Width + (int)(40 * scale));
                int bH = Math.Max((int)(46 * scale), textSize.Height + (int)(18 * scale));
                btnCloseManual.Size = new Size(bW, bH);
                btnCloseManual.Location = new Point((_pnlManualModal.Width - bW) / 2, _pnlManualModal.Height - bH - (int)(18 * scale));
            }

            PositionManualButton();
            _pnlManualModal.Resize += (s, e) => PositionManualButton();
            _pnlManualModal.Controls.Add(btnCloseManual);

            Controls.Add(_pnlManualModal);
            _pnlManualModal.BringToFront();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) && _charmDialogue.IsActive)
            {
                _charmDialogue.Advance();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Escape)
            {
                if (_pnlManualModal != null && _pnlManualModal.Visible)
                {
                    AudioManager.Instance.PlayPaperRustle();
                    _pnlManualModal.Visible = false;
                    e.Handled = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        protected override void OnShiftStarted()
        {
            base.OnShiftStarted();
            // Start in Desk Room View so the player sees their station, then clicks into computer
            SwitchView(ViewState.DeskRoom);
            UpdatePhoneRing(); // the first line is already ringing while you sit at the desk
            BeginShiftBriefing(); // optional opening scene, then the Shift Briefing book (CLOCK IN starts the shift)
        }
    }

    /// <summary>
    /// Backward-compatibility alias so any existing references to Shift1Form instantiate ShiftPlayForm seamlessly.
    /// </summary>
    public class Shift1Form : ShiftPlayForm
    {
        public Shift1Form(OperatorProfile profile) : base(profile, new Shift1TutorialScenario())
        {
        }
    }
}
