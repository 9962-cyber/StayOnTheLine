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
    ///    CRT scanline rendering, teletype typewriter animation, and queue mechanics.
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
                Text = "▸ ELEANOR VANCE",
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

            // Telemetry Row (Location / Signal / Trust)
            _pnlTelemetry = new Panel
            {
                BackColor = Color.Transparent
            };
            _lblLocation = new Label
            {
                Text = "LOCATION: 21 WEST STREET",
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
                Text = "TRUST: 88%",
                ForeColor = Color.FromArgb(160, 215, 150),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Regular),
                AutoSize = true
            };
            _pnlTelemetry.Controls.AddRange(new Control[] { _lblLocation, _lblSignal, _lblTrust });
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
                Text = "KRISHA: Rule number one: verify the address before taking action whenever it's safe to do so.",
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
                    Text = $"{call.CallId}\n{call.Location}",
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

                using var borderPen = new Pen(borderCol, 1.5f);
                e.Graphics.DrawRectangle(borderPen, rc);

                // Top Tag
                var tagFont = FontManager.GetSmallCapsFont(9f, FontStyle.Bold);
                var tagRect = new Rectangle(0, 4, lbl.Width, 16);
                TextRenderer.DrawText(e.Graphics, choice.Tag, tagFont, tagRect, choice.TagColor,
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

            _lblCallerName.Text = $"▸ {call.CallerName} ({call.CallId})";
            _lblCallSignalTag.Text = $"[ SIGNAL {call.SignalStatus} ]";

            _lblLocation.Text = $"LOCATION: {call.Location}";
            _lblSignal.Text = $"SIGNAL: {call.SignalStatus}";
            _lblTrust.Text = $"TRUST: {call.TrustPercentage}%";

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
            else if (_activeCallIndex == 0) // CALL 01: Eleanor Vance (21 West Street)
            {
                switch (call.Stage)
                {
                    case CallStage.Ringing:
                        choices.Add(new ActionChoice
                        {
                            Tag = "INCOMING LINE 01",
                            Caption = "📞 Answer Trunk Line 01 (21 West Street)",
                            BorderColor = Color.FromArgb(120, 255, 160),
                            TagColor = Color.FromArgb(160, 255, 180),
                            HoverBgColor = Color.FromArgb(220, 20, 50, 26),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Inquiry;
                                call.Status = CallQueueStatus.Live;
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter(call.InitialDialogue);
                                _lblSupervisorBar.Text = "KRISHA: Rule number one: verify the address and ask details before taking action.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Inquiry:
                        choices.Add(new ActionChoice
                        {
                            Tag = "INQUIRE",
                            Caption = "🗣 Ask for more details on noise",
                            OnClick = () =>
                            {
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter("YOU: \"Can you describe what kind of noise you're hearing?\"\n\nELEANOR: \"It sounds like heavy furniture scraping the floor in Apartment 6B. I don't think anyone's in danger, but I just want it to stop.\"");
                                _lblSupervisorBar.Text = "KRISHA: Good. You confirmed no immediate danger. Now check the building records.";
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "VERIFY",
                            Caption = "📍 Confirm exact apartment number",
                            OnClick = () =>
                            {
                                call.AddressVerified = true;
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter("YOU: \"Can you confirm your exact building and apartment number?\"\n\nELEANOR: \"Yes, 21 West Street, Apartment 5B. The noise is directly above me in Apartment 6B.\"");
                                _lblSupervisorBar.Text = "KRISHA: Address verified: 21 West Street, Apt 5B/6B. Now check the incident records.";
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "RECORDS",
                            Caption = "🗄 Check incident log for 21 West St",
                            BorderColor = Color.FromArgb(110, 180, 220),
                            TagColor = Color.FromArgb(140, 210, 255),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Resolution;
                                AudioManager.Instance.PlayTypewriterKeystroke();
                                StartTypewriter("INCIDENT ARCHIVE #1042: 21 WEST STREET, APT 6B\nRESIDENT: ROBERT COOPER. PRIOR VIOLATIONS: NONE.\nCLASSIFICATION: NON-EMERGENCY RESIDENTIAL NOISE.");
                                _lblSupervisorBar.Text = "KRISHA: Robert Cooper has a clean record. Standard noise complaint. We don't need to burn a patrol car for this.";
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Resolution:
                        choices.Add(new ActionChoice
                        {
                            Tag = "LOG REPORT",
                            Caption = "📋 Record noise complaint (0 Patrol Units Used)",
                            BorderColor = Color.FromArgb(120, 220, 140),
                            TagColor = Color.FromArgb(160, 255, 180),
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
                            Caption = "🔔 Answer Incoming Trunk Line 02 (High Priority)",
                            BorderColor = Color.FromArgb(255, 200, 100),
                            TagColor = Color.FromArgb(255, 220, 130),
                            HoverBgColor = Color.FromArgb(220, 48, 36, 16),
                            OnClick = () => SelectCall(1)
                        });
                        break;
                }
            }
            else if (_activeCallIndex == 1) // CALL 02: Mara Lang (14 Bell Street)
            {
                switch (call.Stage)
                {
                    case CallStage.Ringing:
                        choices.Add(new ActionChoice
                        {
                            Tag = "URGENT INCOMING",
                            Caption = "📞 Answer Line 02 (High Priority Break-In)",
                            BorderColor = Color.FromArgb(255, 140, 100),
                            TagColor = Color.FromArgb(255, 180, 140),
                            HoverBgColor = Color.FromArgb(220, 50, 20, 16),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Inquiry;
                                call.Status = CallQueueStatus.Live;
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter(call.InitialDialogue);
                                _lblSupervisorBar.Text = "KRISHA: Active home intrusion! Stay calm, verify her safety, and inspect the sector log immediately!";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Inquiry:
                        choices.Add(new ActionChoice
                        {
                            Tag = "SAFETY",
                            Caption = "🛡 Instruct caller to stay hidden and silent",
                            OnClick = () =>
                            {
                                AudioManager.Instance.PlayKeyClick();
                                call.TrustPercentage = 85;
                                _lblTrust.Text = "TRUST: 85%";
                                StartTypewriter("YOU: \"Stay hidden, keep your voice down, and keep the door locked. We are on the line with you.\"\n\nMARA: \"I'm locked in the second floor bedroom! I can hear footsteps downstairs moving toward the hallway! Please hurry!\"");
                                _lblSupervisorBar.Text = "KRISHA: Keep her calm. Now inspect the incident log before dispatching!";
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "VERIFY",
                            Caption = "📍 Confirm exact address & entry point",
                            OnClick = () =>
                            {
                                call.AddressVerified = true;
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter("YOU: \"Mara, confirm your exact address and where they entered.\"\n\nMARA: \"14 Bell Street! The white two-story house! The back patio glass was smashed!\"");
                                _lblSupervisorBar.Text = "KRISHA: 14 Bell Street noted. Check the incident log now!";
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "LOG ARCHIVE",
                            Caption = "🗄 Check incident log for 14 Bell Street",
                            BorderColor = Color.FromArgb(200, 120, 255),
                            TagColor = Color.FromArgb(230, 180, 255),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.DiscrepancyChoice;
                                AudioManager.Instance.PlayFrequencyBlip();
                                StartTypewriter("CRITICAL SYSTEM DISCREPANCY DETECTED\nSTATION RECORD #2196 MODIFIED: 01:42 AM.\nINCOMING CALL RECEIVED: 02:16 AM.\nDISCREPANCY: RECORD WAS MODIFIED 34 MINUTES BEFORE CALL WAS RECEIVED!\nREGISTERED ADDRESS: 41 BELL AVE // REPORTED LOCATION: 14 BELL ST.");
                                _lblSupervisorBar.Text = "KRISHA: Wait a second... that record was modified thirty-four minutes before this call even rang! How is that possible?!";
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.DiscrepancyChoice:
                        choices.Add(new ActionChoice
                        {
                            Tag = "FLAG PARADOX",
                            Caption = "🚩 Flag address & timestamp paradox in station archive",
                            BorderColor = Color.FromArgb(220, 120, 255),
                            TagColor = Color.FromArgb(240, 190, 255),
                            HoverBgColor = Color.FromArgb(220, 48, 16, 56),
                            OnClick = () =>
                            {
                                call.DiscrepancyFlagged = true;
                                State.CluesFound++;
                                if (!Profile.AnomalyFlags.Contains("BellStreetTimestampParadox"))
                                    Profile.AnomalyFlags.Add("BellStreetTimestampParadox");
                                AudioManager.Instance.PlayInkStamp();
                                _lblClueBanner.Visible = true;
                                StartTypewriter("[ EVIDENCE FLAGGED: PARADOX RECORDED IN DISPATCH ARCHIVE. ]\n[ CLUE 01 LOGGED: BELL STREET 34-MINUTE TIMESTAMP PARADOX ]");
                                _lblSupervisorBar.Text = "KRISHA: Excellent catch! That timestamp mismatch is going straight into tonight's official debriefing. Now get units en route!";
                                call.Stage = CallStage.Resolution;
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "BYPASS",
                            Caption = "⏩ Ignore discrepancy & prioritize immediate dispatch",
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
                            Caption = "🚓 Dispatch Patrol Units 08 & 14 (Code 3 Lights & Sirens)",
                            BorderColor = Color.FromArgb(255, 120, 120),
                            TagColor = Color.FromArgb(255, 170, 170),
                            HoverBgColor = Color.FromArgb(220, 60, 20, 20),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Resolved;
                                call.Status = CallQueueStatus.Resolved;
                                call.UnitDispatched = true;
                                State.AvailableUnits -= (State.AvailableUnits >= 2 ? 2 : 1);
                                State.CallsLogged++;
                                AudioManager.Instance.PlayInkStamp();
                                StartTypewriter("YOU: \"Officers are responding with lights and sirens, Mara. Stay down and keep quiet.\"\n\n[ UNITS 08 & 14 DISPATCHED TO 14 BELL STREET UNDER CODE 3. ]\n\nMARA: \"I hear sirens down the avenue... thank you, God, thank you...\"\n\n[ CALL TRANSFERRED TO ON-SCENE PATROL FREQUENCY. STATUS: RESOLVED. ]\n[ PATROL UNITS REMAINING: " + State.AvailableUnits + " / " + Scenario.InitialUnits + " ]");
                                _lblSupervisorBar.Text = "KRISHA: Rapid response units are on scene. Both intruder and caller accounted for. Outstanding dispatch work.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Resolved:
                        choices.Add(new ActionChoice
                        {
                            Tag = "INCOMING LINE 03",
                            Caption = "🔔 Answer Incoming Trunk Line 03 (Old Quarry Road)",
                            BorderColor = Color.FromArgb(140, 200, 255),
                            TagColor = Color.FromArgb(180, 225, 255),
                            HoverBgColor = Color.FromArgb(220, 16, 36, 52),
                            OnClick = () => SelectCall(2)
                        });
                        break;
                }
            }
            else if (_activeCallIndex == 2) // CALL 03: Old Quarry Road (Unknown Caller)
            {
                switch (call.Stage)
                {
                    case CallStage.Ringing:
                        choices.Add(new ActionChoice
                        {
                            Tag = "UNKNOWN ORIGIN",
                            Caption = "📞 Answer Trunk Line 03 (Old Quarry Road)",
                            BorderColor = Color.FromArgb(140, 200, 255),
                            TagColor = Color.FromArgb(180, 225, 255),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Inquiry;
                                call.Status = CallQueueStatus.Live;
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter(call.InitialDialogue);
                                _lblSupervisorBar.Text = "KRISHA: Stay calm. Focus on verifying who this is. Do NOT look out the window.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Inquiry:
                        choices.Add(new ActionChoice
                        {
                            Tag = "QUESTION",
                            Caption = "🗣 Demand caller identity and intent",
                            OnClick = () =>
                            {
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter("YOU: \"Identify yourself. This is an emergency dispatch frequency.\"\n\nCALLER: \"You're the only lights on for miles. Who is that standing on the access road by your perimeter fence?\"");
                                _lblSupervisorBar.Text = "KRISHA: Keep your voice steady. Don't let them know you're rattled.";
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "INSPECT",
                            Caption = "🔍 Ask what they see on the access road",
                            OnClick = () =>
                            {
                                AudioManager.Instance.PlayKeyClick();
                                StartTypewriter("YOU: \"What are you reporting on the access road?\"\n\nCALLER: \"Someone wearing an old dispatcher jacket. They're just standing there under the dead streetlight, looking straight up at your console window.\"");
                                _lblSupervisorBar.Text = "KRISHA: Check the sector records for Old Quarry Road immediately.";
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "SECTOR LOG",
                            Caption = "🗄 Check Old Quarry Road sector log",
                            BorderColor = Color.FromArgb(140, 200, 255),
                            TagColor = Color.FromArgb(180, 225, 255),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Resolution;
                                AudioManager.Instance.PlayTypewriterKeystroke();
                                StartTypewriter("SECTOR LOG: OLD QUARRY ROAD TRANSMITTER\nFACILITY: CIVIL DEFENSE BROADCAST RELAY. DECOMMISSIONED: NOVEMBER 1996.\nPERSONNEL: ZERO ACTIVE CIVILIANS OR PERSONNEL REGISTERED.");
                                _lblSupervisorBar.Text = "KRISHA: Old Quarry Road was abandoned in '96. Nobody should be out there.";
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Resolution:
                        choices.Add(new ActionChoice
                        {
                            Tag = "LOCKDOWN",
                            Caption = "🔒 Engage station perimeter lockdown & archive audio (0 Units)",
                            BorderColor = Color.FromArgb(120, 240, 160),
                            TagColor = Color.FromArgb(160, 255, 190),
                            OnClick = () =>
                            {
                                call.Stage = CallStage.Resolved;
                                call.Status = CallQueueStatus.Resolved;
                                State.CallsLogged++;
                                AudioManager.Instance.PlayInkStamp();
                                StartTypewriter("YOU: \"Security protocol engaged. External perimeter gate locked.\"\n\n[ PERIMETER LOCKDOWN ENGAGED. AUDIO TRANSMISSION SAVED TO TAPE. ]\n[ CALLER DISCONNECTED WITH LOW-FREQUENCY DIAL TONE. ]\n[ CALL RESOLVED. STATUS: ARCHIVED. ]");
                                _lblSupervisorBar.Text = "KRISHA: Smart move. Keeping ourselves secure is always priority when a caller describes the building itself.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        choices.Add(new ActionChoice
                        {
                            Tag = "PATROL",
                            Caption = "🚓 Dispatch patrol unit to perimeter fence (1 Unit)",
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
                                StartTypewriter("[ PATROL UNIT DISPATCHED TO OLD QUARRY ACCESS ROAD. ]\n\n[ RADIO SQUEAL: \"Unit 04 arriving... gates are swinging open in the fog. No vehicle or person in sight.\" ]\n[ CALL RESOLVED. STATUS: ARCHIVED. ]");
                                _lblSupervisorBar.Text = "KRISHA: Unit found nothing, but good caution.";
                                UpdateQueueCards();
                                UpdateDynamicChoices();
                            }
                        });
                        break;

                    case CallStage.Resolved:
                        choices.Add(new ActionChoice
                        {
                            Tag = "SHIFT COMPLETE",
                            Caption = "📋 FINISH SHIFT 1 & REVIEW SUPERVISOR DEBRIEF",
                            BorderColor = Color.FromArgb(140, 255, 180),
                            TagColor = Color.FromArgb(180, 255, 210),
                            HoverBgColor = Color.FromArgb(240, 24, 60, 30),
                            OnClick = () => ShowDebriefModal()
                        });
                        break;
                }
            }

            SetDynamicActionChoices(choices);
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

            // Left Queue Panel (width calculated first, height set to match bottom of supervisor bar)
            int queueW = (int)(crtW * 0.225f);
            int callX = pad + queueW + (int)(12 * scale);
            int callW = crtW - callX - pad;

            // Clue Banner & Active Call Panel Placement
            int callTopY = (int)(46 * scale);
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

            int callH = _lblClueBanner.Visible ? (int)(crtH * 0.32f) : (int)(crtH * 0.37f);
            _pnlCallBox.Location = new Point(callX, callTopY);
            _pnlCallBox.Size = new Size(callW, callH);

            // Telemetry Row
            int telemY = _pnlCallBox.Bottom + (int)(8 * scale);
            _pnlTelemetry.Location = new Point(callX, telemY);
            _pnlTelemetry.Size = new Size(callW, (int)(22 * scale));

            int telemColW = callW / 3;
            _lblLocation.Location = new Point(0, 0);
            _lblSignal.Location = new Point(telemColW, 0);
            _lblTrust.Location = new Point(telemColW * 2, 0);

            // Dynamic Action Choice Buttons Layout (Adaptive: 1 Hero button, 2 Side-by-Side, 3 Side-by-Side, or 2x3 Grid)
            int btnAreaY = _pnlTelemetry.Bottom + (int)(10 * scale);
            int barY;

            int count = _currentChoices.Count;
            if (count == 1)
            {
                int btnH = (int)(72 * scale);
                _actionButtons[0].Location = new Point(callX, btnAreaY + (int)(4 * scale));
                _actionButtons[0].Size = new Size(callW, btnH);
                barY = _actionButtons[0].Bottom + (int)(12 * scale);
            }
            else if (count == 2)
            {
                int gap = (int)(12 * scale);
                int btnW = (callW - gap) / 2;
                int btnH = (int)(72 * scale);
                _actionButtons[0].Location = new Point(callX, btnAreaY + (int)(4 * scale));
                _actionButtons[0].Size = new Size(btnW, btnH);
                _actionButtons[1].Location = new Point(callX + btnW + gap, btnAreaY + (int)(4 * scale));
                _actionButtons[1].Size = new Size(btnW, btnH);
                barY = _actionButtons[0].Bottom + (int)(12 * scale);
            }
            else if (count == 3)
            {
                int gap = (int)(8 * scale);
                int btnW = (callW - gap * 2) / 3;
                int btnH = (int)(72 * scale);
                for (int i = 0; i < 3; i++)
                {
                    _actionButtons[i].Location = new Point(callX + i * (btnW + gap), btnAreaY + (int)(4 * scale));
                    _actionButtons[i].Size = new Size(btnW, btnH);
                }
                barY = _actionButtons[0].Bottom + (int)(12 * scale);
            }
            else
            {
                int btnW = (callW - (int)(16 * scale)) / 3;
                int btnH = (int)(62 * scale);
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
                barY = btnAreaY + 2 * (btnH + btnGapY) + (int)(10 * scale);
            }

            // Supervisor Teletype Bar
            _lblSupervisorBar.Location = new Point(callX, barY);
            _lblSupervisorBar.Size = new Size(callW, (int)(38 * scale));
            _lblSupervisorBar.BringToFront();

            // Set Left Queue Panel bounds matching supervisor bar bottom
            int queueH = _lblSupervisorBar.Bottom - (int)(46 * scale);
            _pnlQueueContainer.Location = new Point(pad, (int)(46 * scale));
            _pnlQueueContainer.Size = new Size(queueW, queueH);

            // Queue Cards within Queue Panel
            int cardH = (int)(56 * scale);
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

            // Re-center Debrief Modal if active
            if (_pnlDebriefModal != null && _pnlDebriefModal.Visible)
            {
                int modalW = Math.Min(880, (int)(ClientSize.Width * 0.75f));
                int modalH = Math.Min(620, (int)(ClientSize.Height * 0.80f));
                _pnlDebriefModal.Size = new Size(modalW, modalH);
                _pnlDebriefModal.Location = new Point((ClientSize.Width - modalW) / 2, (ClientSize.Height - modalH) / 2);
            }

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

            int modalW = Math.Min(880, (int)(ClientSize.Width * 0.75f));
            int modalH = Math.Min(620, (int)(ClientSize.Height * 0.80f));

            _pnlDebriefModal = new Panel
            {
                Size = new Size(modalW, modalH),
                Location = new Point((ClientSize.Width - modalW) / 2, (ClientSize.Height - modalH) / 2),
                BackColor = Color.FromArgb(245, 6, 14, 8),
                BorderStyle = BorderStyle.FixedSingle
            };

            _pnlDebriefModal.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var borderPen = new Pen(Color.FromArgb(140, 255, 180), 2f);
                e.Graphics.DrawRectangle(borderPen, 1, 1, _pnlDebriefModal.Width - 3, _pnlDebriefModal.Height - 3);

                using var scanPen = new Pen(Color.FromArgb(15, 0, 0, 0), 1f);
                for (int y = 0; y < _pnlDebriefModal.Height; y += 3)
                {
                    e.Graphics.DrawLine(scanPen, 0, y, _pnlDebriefModal.Width, y);
                }
            };

            // Modal Header
            var lblTitle = new Label
            {
                Text = "CIVIL DEFENSE NIGHT DESK  //  SHIFT 01 DEBRIEFING",
                ForeColor = Color.FromArgb(140, 255, 180),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(18f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 16),
                Size = new Size(modalW, 36)
            };
            _pnlDebriefModal.Controls.Add(lblTitle);

            // Subtitle
            var lblSub = new Label
            {
                Text = $"SUPERVISOR: {Scenario.SupervisorName}   ·   OPERATOR: {Profile.OperatorName.ToUpperInvariant()}   ·   {DateTime.Now:MMM dd, yyyy}",
                ForeColor = Color.FromArgb(190, 215, 185),
                BackColor = Color.Transparent,
                Font = FontManager.GetSmallCapsFont(11f, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 54),
                Size = new Size(modalW, 24)
            };
            _pnlDebriefModal.Controls.Add(lblSub);

            // Metrics Row Panel
            var pnlMetrics = new Panel
            {
                Location = new Point(20, 84),
                Size = new Size(modalW - 40, 72),
                BackColor = Color.FromArgb(160, 10, 22, 12)
            };

            int colW = (modalW - 60) / 3;
            var lblMetric1 = new Label
            {
                Text = $"CALLS LOGGED\n{State.CallsLogged} / 2",
                ForeColor = Color.FromArgb(220, 240, 220),
                Font = FontManager.GetSmallCapsFont(12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(colW, 64),
                Location = new Point(0, 4),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblMetric2 = new Label
            {
                Text = $"ANOMALIES FLAGGED\n{State.CluesFound} / 2",
                ForeColor = Color.FromArgb(255, 225, 120),
                Font = FontManager.GetSmallCapsFont(12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(colW, 64),
                Location = new Point(colW + 10, 4),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lblMetric3 = new Label
            {
                Text = $"PATROL UNITS AVAILABLE\n{State.AvailableUnits} / {Scenario.InitialUnits}",
                ForeColor = Color.FromArgb(130, 210, 255),
                Font = FontManager.GetSmallCapsFont(12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(colW, 64),
                Location = new Point((colW + 10) * 2, 4),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlMetrics.Controls.AddRange(new Control[] { lblMetric1, lblMetric2, lblMetric3 });
            _pnlDebriefModal.Controls.Add(pnlMetrics);

            // Evaluation Box
            var pnlEval = new Panel
            {
                Location = new Point(20, 166),
                Size = new Size(modalW - 40, modalH - 246),
                BackColor = Color.Transparent
            };

            var lblEval = new Label
            {
                Text = Scenario.GetDebriefMessage(State),
                ForeColor = Color.FromArgb(210, 235, 210),
                BackColor = Color.FromArgb(180, 8, 18, 10),
                Font = FontManager.GetItalicFont(14f),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlEval.Controls.Add(lblEval);
            _pnlDebriefModal.Controls.Add(pnlEval);

            // Button Action Row
            var pnlButtons = new Panel
            {
                Location = new Point(0, modalH - 72),
                Size = new Size(modalW, 64),
                BackColor = Color.FromArgb(180, 6, 12, 6)
            };

            int btnW = 210;
            int btnH = 44;
            int totalBtnW = btnW * 3 + 30;
            int startX = (modalW - totalBtnW) / 2;

            var btnAdvance = new Label
            {
                Text = "ADVANCE TO SHIFT 2 ▶",
                ForeColor = Color.FromArgb(140, 255, 180),
                BackColor = Color.FromArgb(200, 16, 40, 20),
                Font = FontManager.GetSmallCapsFont(12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(btnW, btnH),
                Location = new Point(startX, 12),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            btnAdvance.Click += (s, e) =>
            {
                Profile.AdvanceShift(newCalls: State.CallsLogged, errors: State.Errors);
                Profile.CurrentShift = 2;
                Profile.TutorialCompleted = true;
                SaveManager.Instance.SaveProfile(Profile);
                AudioManager.Instance.PlayInkStamp();

                MessageBox.Show(
                    this,
                    "SHIFT 01 COMPLETED WITH DISTINCTION.\n\nYour promotion to Shift 02 has been officially registered in the station ledger.",
                    "Night Desk Promotion",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            btnAdvance.MouseEnter += (s, e) => btnAdvance.BackColor = Color.FromArgb(240, 24, 60, 30);
            btnAdvance.MouseLeave += (s, e) => btnAdvance.BackColor = Color.FromArgb(200, 16, 40, 20);

            var btnMenu = new Label
            {
                Text = "SAVE & MAIN MENU",
                ForeColor = Color.FromArgb(210, 220, 205),
                BackColor = Color.FromArgb(180, 26, 32, 24),
                Font = FontManager.GetSmallCapsFont(12f, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(btnW, btnH),
                Location = new Point(startX + btnW + 15, 12),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            btnMenu.Click += (s, e) =>
            {
                Profile.TutorialCompleted = true;
                SaveManager.Instance.SaveProfile(Profile);
                AudioManager.Instance.PlayKeyClick();
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            btnMenu.MouseEnter += (s, e) => btnMenu.BackColor = Color.FromArgb(220, 42, 50, 40);
            btnMenu.MouseLeave += (s, e) => btnMenu.BackColor = Color.FromArgb(180, 26, 32, 24);

            var btnBack = new Label
            {
                Text = "RETURN TO DESK",
                ForeColor = Color.FromArgb(180, 195, 180),
                BackColor = Color.FromArgb(180, 24, 28, 22),
                Font = FontManager.GetSmallCapsFont(12f, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(btnW, btnH),
                Location = new Point(startX + (btnW + 15) * 2, 12),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            btnBack.Click += (s, e) =>
            {
                _pnlDebriefModal.Visible = false;
                AudioManager.Instance.PlayPaperRustle();
            };
            btnBack.MouseEnter += (s, e) => btnBack.BackColor = Color.FromArgb(220, 36, 42, 34);
            btnBack.MouseLeave += (s, e) => btnBack.BackColor = Color.FromArgb(180, 24, 28, 22);

            pnlButtons.Controls.AddRange(new Control[] { btnAdvance, btnMenu, btnBack });
            _pnlDebriefModal.Controls.Add(pnlButtons);

            Controls.Add(_pnlDebriefModal);
            _pnlDebriefModal.BringToFront();
        }

        protected override void OnShiftStarted()
        {
            base.OnShiftStarted();
            // Start in Desk Room View so the player sees their station, then can click into the computer!
            SwitchView(ViewState.DeskRoom);
        }
    }
}
