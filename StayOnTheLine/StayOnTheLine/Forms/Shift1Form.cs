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
    public class Shift1Form : BaseShiftForm
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
        private Image? _imgEndShiftBg;

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

        // UI Controls for Desk Room View
        private Panel _pnlDeskHeader = null!;
        private Label _lblDeskHeaderTitle = null!;
        private Label _lblDeskHotspot = null!;
        private Label _btnDeskSettings = null!;
        private Label _btnDeskEndShift = null!;

        // UI Controls for Computer Terminal View
        private Panel _pnlCrtScreen = null!;
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
        private Label _lblCallTranscript = null!;
        private Label _lblCallSignalTag = null!;

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

        // Clue 01 Notification Banner & Supervisor Teletype Bar
        private Label _lblClueBanner = null!;
        private Label _lblSupervisorBar = null!;

        public Shift1Form(OperatorProfile profile) : base(profile, new Shift1TutorialScenario())
        {
            _calls = Scenario.GenerateCalls();
            LoadAssets();
            InitializeCustomUI();
            ApplyResponsiveLayout();

            Resize += (s, e) => ApplyResponsiveLayout();
        }

        private void LoadAssets()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string assetsDir = Path.Combine(baseDir, "Assets");
            if (!Directory.Exists(assetsDir))
            {
                assetsDir = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            }

            // 1. Desk Room Background
            string pathDesk = Path.Combine(assetsDir, "maingameplay_bg.png");
            if (File.Exists(pathDesk))
            {
                try { _imgDeskRoom = Image.FromFile(pathDesk); } catch { }
            }

            // 2. Computer Terminal Mockup Background
            string pathComputer = Path.Combine(assetsDir, "computer_desk_ui.jpg");
            if (File.Exists(pathComputer))
            {
                try { _imgComputerDesk = Image.FromFile(pathComputer); } catch { }
            }

            // 3. End of Shift Clipboard Background
            string pathEndShift = Path.Combine(assetsDir, "endshiftsummary_bg.png");
            if (File.Exists(pathEndShift))
            {
                try { _imgEndShiftBg = Image.FromFile(pathEndShift); } catch { }
            }
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
                Text = $"CIVIL DEFENSE NIGHT DESK  //  TERMINAL 04   ·   OPERATOR: {Profile.OperatorName.ToUpperInvariant()}   ·   {Scenario.ShiftTitle}",
                ForeColor = Color.FromArgb(220, 235, 215),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(15f, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0),
                AutoSize = true
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
                Text = "NIGHT DESK // SHIFT 01",
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

            // Tutorial Protocol Checklist Strip (Across CRT under Header)
            _lblProtocolChecklist = new Label
            {
                Text = "[✓] 1. Select Line   [►] 2. Open Audio   [ ] 3. Identify Caller   [ ] 4. Check Archive   [ ] 5. Resolve",
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

            _lblCallTranscript = new Label
            {
                Text = string.Empty,
                ForeColor = Color.FromArgb(245, 245, 235),
                BackColor = Color.Transparent,
                Font = FontManager.GetItalicFont(18f),
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 10)
            };

            _lblCallSignalTag = new Label
            {
                Text = "[ SIGNAL STABLE ]",
                ForeColor = Color.FromArgb(130, 200, 120),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Regular),
                Dock = DockStyle.Bottom,
                Height = 26,
                Padding = new Padding(12, 0, 0, 4)
            };

            _pnlCallBox.Controls.Add(_lblCallTranscript);
            _pnlCallBox.Controls.Add(_lblCallerName);
            _pnlCallBox.Controls.Add(_lblCallSignalTag);
            _pnlCrtScreen.Controls.Add(_pnlCallBox);

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

            // Supervisor Teletype Bar
            _lblSupervisorBar = new Label
            {
                Text = "KRISHA: Rule number one trainee: Always verify the caller's name and exact address before taking action.",
                ForeColor = Color.FromArgb(140, 240, 160),
                BackColor = Color.FromArgb(200, 3, 8, 4),
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontManager.GetItalicFont(13f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 12, 0)
            };
            _pnlCrtScreen.Controls.Add(_lblSupervisorBar);

            // Timers
            _shiftTimer.Interval = 50;
            _shiftTimer.Tick += (s, e) => UpdateStopwatch();
            _shiftTimer.Start();

            _typewriterTimer.Interval = SettingsManager.Instance.Current.GetDelayPerCharacterMs();
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
            _tutorialPulseTimer.Start();
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
                    Text = $"{call.CallId}\n{call.DisplayLocation}",
                    ForeColor = Color.FromArgb(210, 225, 205),
                    BackColor = Color.Transparent,
                    Font = FontManager.GetSmallCapsFont(9f, FontStyle.Regular),
                    Size = new Size(114, 38),
                    Location = new Point(44, 8)
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

                // Wire click to select call
                Action selectAction = () => SelectCall(callIdx);
                card.Click += (s, e) => selectAction();
                lblNum.Click += (s, e) => selectAction();
                lblLine.Click += (s, e) => selectAction();
                lblBadge.Click += (s, e) => selectAction();

                card.MouseEnter += (s, e) =>
                {
                    if (callIdx != _activeCallIndex)
                        card.BackColor = Color.FromArgb(200, 14, 28, 16);
                };
                card.MouseLeave += (s, e) =>
                {
                    if (callIdx != _activeCallIndex)
                        card.BackColor = Color.FromArgb(160, 8, 16, 10);
                };

                _queueCards.Add(card);
                _pnlQueueContainer.Controls.Add(card);
            }
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
                var tagFont = FontManager.GetSmallCapsFont(9f, FontStyle.Bold);
                var tagRect = new Rectangle(0, 4, lbl.Width, 16);
                string tagText = choice.IsRecommended ? $"[►] {choice.Tag}" : choice.Tag;
                Color tagCol = (choice.IsRecommended && _pulseHighlight) ? Color.FromArgb(255, 235, 140) : choice.TagColor;
                TextRenderer.DrawText(e.Graphics, tagText, tagFont, tagRect, tagCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);

                // Main Caption
                var captionFont = FontManager.GetItalicFont(12.5f);
                var capRect = new Rectangle(6, 18, lbl.Width - 12, lbl.Height - 20);
                Color capCol = isHovered ? Color.White : Color.FromArgb(220, 235, 215);
                TextRenderer.DrawText(e.Graphics, choice.Caption, captionFont, capRect, capCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
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
            bool isTerminal = (_currentView == ViewState.ComputerTerminal);

            _pnlDeskHeader.Visible = !isTerminal;
            _lblDeskHotspot.Visible = !isTerminal;

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
                        _lblSupervisorBar.Text = "KRISHA: Look at the yellow note on your desk: 'Line 08 Do not answer.' That channel has been dead for twenty years.";
                        AudioManager.Instance.PlayFrequencyBlip();
                    }
                });
            }
            else if (_activeCallIndex == 0) // CALL 01: Eleanor Vance (21 West Street, Apt 5B)
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
            else if (_activeCallIndex == 1) // CALL 02: Chloe Bautista (14 Bell Street)
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
                                    "[ ...CONSOLE 03 OPERATOR CHARM REMAINS COMPLETELY SILENT, STARING AT HER SCREEN. ]"
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
            else if (_activeCallIndex == 2) // CALL 03: Marcus Reed (88 Elm Ridge Road)
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

            SetDynamicActionChoices(choices);
        }

        private void UpdateTutorialGuidance()
        {
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

                card.BackColor = (i == _activeCallIndex)
                    ? Color.FromArgb(200, 18, 40, 22)
                    : Color.FromArgb(160, 8, 16, 10);

                if (card.Controls.Count >= 3)
                {
                    if (card.Controls[0] is Label lblNum)
                    {
                        lblNum.ForeColor = (i == _activeCallIndex)
                            ? Color.FromArgb(200, 255, 180)
                            : Color.FromArgb(140, 175, 130);
                    }
                    if (card.Controls[1] is Label lblLine)
                    {
                        lblLine.Text = $"{call.CallId}\n{call.DisplayLocation}";
                    }
                    if (card.Controls[2] is Label lblBadge)
                    {
                        if (call.Stage == CallStage.Ringing && call.Status != CallQueueStatus.Resolved)
                        {
                            lblBadge.Text = "RINGING";
                            lblBadge.ForeColor = Color.FromArgb(255, 215, 110);
                        }
                        else
                        {
                            lblBadge.Text = call.Status.ToString().ToUpperInvariant();
                            lblBadge.ForeColor = call.Status switch
                            {
                                CallQueueStatus.Live => Color.FromArgb(120, 255, 160),
                                CallQueueStatus.Waiting => Color.FromArgb(255, 215, 110),
                                CallQueueStatus.OnHold => Color.FromArgb(130, 200, 255),
                                CallQueueStatus.Restricted => Color.FromArgb(255, 120, 120),
                                CallQueueStatus.Resolved => Color.FromArgb(160, 210, 180),
                                _ => Color.FromArgb(180, 180, 180)
                            };
                        }
                    }
                }
            }
        }

        private void StartTypewriter(string text)
        {
            _targetDialogue = text;
            _displayedDialogue = string.Empty;
            _typewriterCharIndex = 0;
            _typewriterTimer.Start();
        }

        private void UpdateTypewriter()
        {
            if (_typewriterCharIndex < _targetDialogue.Length)
            {
                _displayedDialogue += _targetDialogue[_typewriterCharIndex];
                _typewriterCharIndex++;
                _lblCallTranscript.Text = _displayedDialogue;

                if (_typewriterCharIndex % 3 == 0)
                {
                    AudioManager.Instance.PlayTypewriterKeystroke();
                }
            }
            else
            {
                _typewriterTimer.Stop();
            }
        }

        private void UpdateStopwatch()
        {
            var elapsed = DateTime.Now - _shiftStartTime;
            _lblCrtHeaderTimer.Text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            _lblCrtHeaderLeft.Text = $"NIGHT DESK // SHIFT 01   [ UNITS: {State.AvailableUnits}/{Scenario.InitialUnits} ]";
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

        private void ApplyResponsiveLayout()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

            int W = ClientSize.Width;
            int H = ClientSize.Height;
            float deskScale = H / 1080f;

            // 1. Desk Hotspot Placement (over the glowing terminal desk in maingameplay_bg.png)
            int spotW = (int)(540 * deskScale);
            int spotH = (int)(90 * deskScale);
            _lblDeskHotspot.Size = new Size(spotW, spotH);
            _lblDeskHotspot.Location = new Point((W - spotW) / 2, (int)(H * 0.76f));

            // 2. CRT Screen Bounds inside Computer Terminal View (exact contain-fit alignment)
            int crtX, crtY, crtW, crtH;
            float crtScale;
            if (_imgComputerDesk != null)
            {
                var imgRect = GetContainFitRect(_imgComputerDesk, ClientRectangle);
                float factor = (float)imgRect.Width / 1024f;

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

            // Left Queue Panel
            int queueW = (int)(crtW * 0.225f);
            int callX = pad + queueW + (int)(12 * scale);
            int callW = crtW - callX - pad;

            // Clue Banner & Active Call Panel Placement
            int callTopY = _lblProtocolChecklist.Bottom + (int)(6 * scale);
            if (_lblClueBanner.Visible)
            {
                int bannerH = (int)(28 * scale);
                _lblClueBanner.Location = new Point(callX, callTopY);
                _lblClueBanner.Size = new Size(callW, bannerH);
                callTopY += bannerH + (int)(6 * scale);
            }
            else
            {
                _lblClueBanner.Size = Size.Empty;
            }

            int callH = _lblClueBanner.Visible ? (int)(crtH * 0.30f) : (int)(crtH * 0.35f);
            _pnlCallBox.Location = new Point(callX, callTopY);
            _pnlCallBox.Size = new Size(callW, callH);

            // Telemetry Row (4 Columns: Location, Signal, Trust, Panic)
            int telemY = _pnlCallBox.Bottom + (int)(6 * scale);
            _pnlTelemetry.Location = new Point(callX, telemY);
            _pnlTelemetry.Size = new Size(callW, (int)(22 * scale));

            int telemColW = callW / 4;
            _lblLocation.Location = new Point(0, 0);
            _lblLocation.Size = new Size(telemColW + (int)(20 * scale), (int)(22 * scale));
            _lblSignal.Location = new Point(telemColW + (int)(20 * scale), 0);
            _lblSignal.Size = new Size(telemColW - (int)(10 * scale), (int)(22 * scale));
            _lblTrust.Location = new Point(telemColW * 2 + (int)(10 * scale), 0);
            _lblTrust.Size = new Size(telemColW - (int)(10 * scale), (int)(22 * scale));
            _lblPanic.Location = new Point(telemColW * 3, 0);
            _lblPanic.Size = new Size(telemColW, (int)(22 * scale));

            // Dynamic Action Choice Buttons Layout (Adaptive)
            int btnAreaY = _pnlTelemetry.Bottom + (int)(8 * scale);
            int barY;

            int count = _currentChoices.Count;
            if (count == 1)
            {
                int btnH = (int)(70 * scale);
                _actionButtons[0].Location = new Point(callX, btnAreaY + (int)(4 * scale));
                _actionButtons[0].Size = new Size(callW, btnH);
                barY = _actionButtons[0].Bottom + (int)(10 * scale);
            }
            else if (count == 2)
            {
                int gap = (int)(12 * scale);
                int btnW = (callW - gap) / 2;
                int btnH = (int)(70 * scale);
                _actionButtons[0].Location = new Point(callX, btnAreaY + (int)(4 * scale));
                _actionButtons[0].Size = new Size(btnW, btnH);
                _actionButtons[1].Location = new Point(callX + btnW + gap, btnAreaY + (int)(4 * scale));
                _actionButtons[1].Size = new Size(btnW, btnH);
                barY = _actionButtons[0].Bottom + (int)(10 * scale);
            }
            else if (count == 3)
            {
                int gap = (int)(8 * scale);
                int btnW = (callW - gap * 2) / 3;
                int btnH = (int)(70 * scale);
                for (int i = 0; i < 3; i++)
                {
                    _actionButtons[i].Location = new Point(callX + i * (btnW + gap), btnAreaY + (int)(4 * scale));
                    _actionButtons[i].Size = new Size(btnW, btnH);
                }
                barY = _actionButtons[0].Bottom + (int)(10 * scale);
            }
            else
            {
                int btnW = (callW - (int)(16 * scale)) / 3;
                int btnH = (int)(60 * scale);
                int btnGapX = (int)(8 * scale);
                int btnGapY = (int)(6 * scale);

                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int btnIdx = r * 3 + c;
                        _actionButtons[btnIdx].Location = new Point(callX + c * (btnW + btnGapX), btnAreaY + r * (btnH + btnGapY));
                        _actionButtons[btnIdx].Size = new Size(btnW, btnH);
                    }
                }
                barY = btnAreaY + 2 * (btnH + btnGapY) + (int)(8 * scale);
            }

            // Supervisor Teletype Bar
            _lblSupervisorBar.Location = new Point(callX, barY);
            _lblSupervisorBar.Size = new Size(callW, (int)(38 * scale));
            _lblSupervisorBar.BringToFront();

            // Set Left Queue Panel bounds matching supervisor bar bottom
            int queueTopY = _lblProtocolChecklist.Bottom + (int)(6 * scale);
            int queueH = _lblSupervisorBar.Bottom - queueTopY;
            _pnlQueueContainer.Location = new Point(pad, queueTopY);
            _pnlQueueContainer.Size = new Size(queueW, queueH);

            // Queue Cards within Queue Panel
            int cardH = (int)(54 * scale);
            int cardSpacing = (int)(6 * scale);
            int cardY = _lblQueueHeader.Bottom + 4;
            for (int i = 0; i < _queueCards.Count; i++)
            {
                var card = _queueCards[i];
                card.Location = new Point(4, cardY + i * (cardH + cardSpacing));
                card.Size = new Size(queueW - 8, cardH);
                if (card.Controls.Count >= 3)
                {
                    card.Controls[0].Size = new Size((int)(36 * scale), cardH);
                    card.Controls[1].Location = new Point((int)(40 * scale), (int)(6 * scale));
                    card.Controls[1].Size = new Size(card.Width - (int)(105 * scale), cardH - (int)(10 * scale));
                    card.Controls[2].Location = new Point(card.Width - (int)(62 * scale), (int)(14 * scale));
                    card.Controls[2].Size = new Size((int)(58 * scale), (int)(20 * scale));
                }
            }

            UpdateTutorialGuidance();
            Invalidate();
        }

        public void ShowDebriefModal()
        {
            if (_pnlDebriefModal != null)
            {
                _pnlDebriefModal.BringToFront();
                _pnlDebriefModal.Visible = true;
                return;
            }

            AudioManager.Instance.PlayInkStamp();

            // The modal container fills the entire screen so that endshiftsummary_bg.png forms the background
            _pnlDebriefModal = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 10, 10)
            };

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
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                var rc = _pnlDebriefModal.ClientRectangle;

                // 1. Draw endshiftsummary_bg.png background
                if (_imgEndShiftBg != null)
                {
                    DrawImageCoverFit(g, _imgEndShiftBg, rc);
                }
                else if (_imgDeskRoom != null)
                {
                    DrawImageCoverFit(g, _imgDeskRoom, rc);
                }

                // Dark atmospheric vignette wash
                using (var washBrush = new SolidBrush(Color.FromArgb(130, 8, 10, 8)))
                {
                    g.FillRectangle(washBrush, rc);
                }

                // 2. Clipboard Dimensions & Placement
                int clipW = Math.Clamp((int)(rc.Width * 0.50f), 540, 680);
                int clipH = Math.Clamp((int)(rc.Height * 0.88f), 600, 780);
                int clipX = (rc.Width - clipW) / 2;
                int clipY = (rc.Height - clipH) / 2 + 10;
                var clipRect = new Rectangle(clipX, clipY, clipW, clipH);

                // Soft drop shadow around clipboard
                using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                {
                    g.FillRectangle(shadowBrush, clipX + 8, clipY + 8, clipW, clipH);
                }

                // Clipboard Wood Base (Rich Mahogany / Walnut)
                using (var woodBrush = new SolidBrush(Color.FromArgb(48, 30, 20)))
                using (var woodBorderPen = new Pen(Color.FromArgb(28, 16, 10), 3f))
                {
                    g.FillRectangle(woodBrush, clipRect);
                    g.DrawRectangle(woodBorderPen, clipRect);
                }

                // 3. Aged Parchment Paper
                int paperMarginX = 20;
                int paperMarginY = 38;
                var paperRect = new Rectangle(clipX + paperMarginX, clipY + paperMarginY, clipW - paperMarginX * 2, clipH - paperMarginY - 24);

                using (var paperBrush = new SolidBrush(Color.FromArgb(238, 226, 198))) // #EEE2C6 aged parchment
                using (var paperBorderPen = new Pen(Color.FromArgb(195, 180, 150), 1.5f))
                {
                    g.FillRectangle(paperBrush, paperRect);
                    g.DrawRectangle(paperBorderPen, paperRect);
                }

                // Fine archival inner border
                using (var innerPen = new Pen(Color.FromArgb(215, 200, 172), 1f))
                {
                    g.DrawRectangle(innerPen, paperRect.X + 4, paperRect.Y + 4, paperRect.Width - 8, paperRect.Height - 8);
                }

                // 4. Metal Clip at Top of Clipboard
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
                // Clip rivets
                using (var rivetBrush = new SolidBrush(Color.FromArgb(60, 62, 66)))
                {
                    g.FillEllipse(rivetBrush, clipPosX + 14, clipPosY + 12, 10, 10);
                    g.FillEllipse(rivetBrush, clipPosX + clipWidth - 24, clipPosY + 12, 10, 10);
                }

                // 5. Header Typography on Parchment
                int contentX = paperRect.X + 32;
                int contentW = paperRect.Width - 64;
                int currY = paperRect.Y + 22;

                // Confidential Subheading
                using var fontSub = FontManager.GetSmallCapsFont(10f, FontStyle.Regular);
                var subCol = Color.FromArgb(110, 95, 80);
                var subRect = new Rectangle(contentX, currY, contentW, 18);
                TextRenderer.DrawText(g, "Municipal Emergency Services · Confidential", fontSub, subRect, subCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
                currY += 24;

                // "END OF SHIFT" Main Title
                using var fontTitle = FontManager.GetSmallCapsFont(22f, FontStyle.Bold);
                var titleCol = Color.FromArgb(28, 24, 20);
                var titleRect = new Rectangle(contentX, currY, contentW, 36);
                TextRenderer.DrawText(g, "END OF SHIFT", fontTitle, titleRect, titleCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
                currY += 40;

                // Employee & Date Subheader
                using var fontEmp = FontManager.GetSmallCapsFont(11f, FontStyle.Regular);
                var empCol = Color.FromArgb(85, 75, 65);
                var empRect = new Rectangle(contentX, currY, contentW, 20);
                TextRenderer.DrawText(g, $"Employee: {Profile.OperatorName.ToUpperInvariant()} · November 9, 1998", fontEmp, empRect, empCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
                currY += 32;

                // 6. The 2x3 Metric Table Grid
                int gridX = contentX;
                int gridY = currY;
                int gridW = contentW;
                int gridH = 150;
                int colW = gridW / 2;
                int rowH = gridH / 3;

                using var gridPen = new Pen(Color.FromArgb(140, 125, 110), 1.5f);
                using var cellBgBrush = new SolidBrush(Color.FromArgb(244, 235, 215));
                g.FillRectangle(cellBgBrush, gridX, gridY, gridW, gridH);
                g.DrawRectangle(gridPen, gridX, gridY, gridW, gridH);

                // Grid lines
                g.DrawLine(gridPen, gridX + colW, gridY, gridX + colW, gridY + gridH); // Vertical middle
                g.DrawLine(gridPen, gridX, gridY + rowH, gridX + gridW, gridY + rowH); // Row 1
                g.DrawLine(gridPen, gridX, gridY + rowH * 2, gridX + gridW, gridY + rowH * 2); // Row 2

                using var fontMetricLabel = FontManager.GetSmallCapsFont(9.5f, FontStyle.Regular);
                using var fontMetricVal = FontManager.GetSmallCapsFont(15f, FontStyle.Bold);
                var labelCol = Color.FromArgb(115, 100, 85);
                var valCol = Color.FromArgb(28, 24, 20);

                void DrawCell(int r, int c, string label, string val)
                {
                    int cx = gridX + c * colW + 12;
                    int cy = gridY + r * rowH + 6;
                    var rLabel = new Rectangle(cx, cy, colW - 24, 16);
                    var rVal = new Rectangle(cx, cy + 18, colW - 24, 24);
                    TextRenderer.DrawText(g, label, fontMetricLabel, rLabel, labelCol, TextFormatFlags.Left);
                    TextRenderer.DrawText(g, val, fontMetricVal, rVal, valCol, TextFormatFlags.Left);
                }

                DrawCell(0, 0, "Calls handled", $"0{callsHandled} / 03");
                DrawCell(0, 1, "Correct emergency", "02 / 02");
                DrawCell(1, 0, "Protocol errors", $"0{State.Errors}");
                DrawCell(1, 1, "Units remaining", $"0{State.AvailableUnits} / 0{Scenario.InitialUnits}");
                DrawCell(2, 0, "Caller trust", $"{avgTrust}%");
                DrawCell(2, 1, "Evidence found", $"0{State.CluesFound} / 01");

                currY += gridH + 28;

                // 7. Supervisor Note Title
                using var fontNoteHeader = FontManager.GetSmallCapsFont(11.5f, FontStyle.Bold);
                var noteHeadCol = Color.FromArgb(50, 42, 34);
                var rNoteHead = new Rectangle(contentX, currY, (int)(contentW * 0.65f), 20);
                TextRenderer.DrawText(g, "Supervisor note", fontNoteHeader, rNoteHead, noteHeadCol, TextFormatFlags.Left);
                currY += 26;

                // Supervisor Note Body
                using var fontNoteBody = FontManager.GetItalicFont(13.5f);
                var noteBodyCol = Color.FromArgb(38, 30, 24);
                var rNoteBody = new Rectangle(contentX, currY, (int)(contentW * 0.62f), 70);
                TextRenderer.DrawText(g, "Your report contains an incident that did not occur.\n\nDo not speak to the day shift.",
                    fontNoteBody, rNoteBody, noteBodyCol, TextFormatFlags.Left | TextFormatFlags.WordBreak);

                // 8. Angled Distressed Red Rubber Stamp: [ UNDER REVIEW ]
                int stampW = 160;
                int stampH = 46;
                int stampX = contentX + contentW - stampW - 10;
                int stampY = currY - 6;

                var stampState = g.Save();
                g.TranslateTransform(stampX + stampW / 2f, stampY + stampH / 2f);
                g.RotateTransform(-6f); // Authentic ink tilt
                var rStampLocal = new Rectangle(-stampW / 2, -stampH / 2, stampW, stampH);

                using var stampPen = new Pen(Color.FromArgb(195, 36, 36), 2.5f);
                g.DrawRectangle(stampPen, rStampLocal);

                using var fontStamp = FontManager.GetSmallCapsFont(14f, FontStyle.Bold);
                var stampTextCol = Color.FromArgb(195, 36, 36);
                TextRenderer.DrawText(g, "UNDER REVIEW", fontStamp, rStampLocal, stampTextCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                g.Restore(stampState);
            };

            // Action Button: [ Close personnel file ]
            var btnClose = new Label
            {
                Text = "[ Close personnel file ]",
                ForeColor = Color.FromArgb(28, 24, 20),
                BackColor = Color.FromArgb(230, 216, 186),
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontManager.GetSmallCapsFont(12.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(230, 44),
                Cursor = Cursors.Hand
            };

            void PositionCloseBtn()
            {
                int clipW = Math.Clamp((int)(_pnlDebriefModal.Width * 0.50f), 540, 680);
                int clipH = Math.Clamp((int)(_pnlDebriefModal.Height * 0.88f), 600, 780);
                int clipX = (_pnlDebriefModal.Width - clipW) / 2;
                int clipY = (_pnlDebriefModal.Height - clipH) / 2 + 10;
                int paperMarginX = 20;
                int paperBottom = clipY + clipH - 24;

                btnClose.Location = new Point(clipX + paperMarginX + 32, paperBottom - 64);
            }

            PositionCloseBtn();
            _pnlDebriefModal.Resize += (s, e) => PositionCloseBtn();

            btnClose.MouseEnter += (s, e) =>
            {
                btnClose.BackColor = Color.FromArgb(48, 36, 26);
                btnClose.ForeColor = Color.FromArgb(245, 235, 215);
                AudioManager.Instance.PlayPaperRustle();
            };
            btnClose.MouseLeave += (s, e) =>
            {
                btnClose.BackColor = Color.FromArgb(230, 216, 186);
                btnClose.ForeColor = Color.FromArgb(28, 24, 20);
            };

            btnClose.Click += (s, e) =>
            {
                Profile.AdvanceShift(newCalls: State.CallsLogged, errors: State.Errors);
                Profile.CurrentShift = 2;
                Profile.TutorialCompleted = true;
                SaveManager.Instance.SaveProfile(Profile);
                AudioManager.Instance.PlayInkStamp();

                MessageBox.Show(
                    this,
                    "SHIFT 01 COMPLETED WITH DISTINCTION.\n\nYour promotion to Shift 02 has been officially registered in the station ledger.",
                    "Night Desk Personnel Ledger",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            _pnlDebriefModal.Controls.Add(btnClose);
            Controls.Add(_pnlDebriefModal);
            _pnlDebriefModal.BringToFront();
        }

        protected override void OnShiftStarted()
        {
            base.OnShiftStarted();
            // Start in Desk Room View so the player sees their station, then clicks into computer
            SwitchView(ViewState.DeskRoom);
        }
    }
}
