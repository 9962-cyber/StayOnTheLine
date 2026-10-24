using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using StayOnTheLine.Controls;
using StayOnTheLine.Controls.Messenger;
using StayOnTheLine.Models;
using StayOnTheLine.Models.Dialogue;
using StayOnTheLine.Systems;
using StayOnTheLine.Systems.Dialogue;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principles Demonstrated:
    /// 1. INHERITANCE: Inherits BaseGameForm (borderless fullscreen, composited, black).
    /// 2. COMPOSITION: Built from self-contained controls - MessengerWindow (thread + responses),
    ///    AuditLogWindow, two CharacterPortraits and a RoomCaption.
    /// 3. SEPARATION OF CONCERNS: The DialogueRunner decides what happens next; this form only
    ///    decides when and how it is shown (typing delays, slides, cues, sounds).
    ///
    /// Shift 3 "Stef scene": plays one records-terminal thread (a <see cref="ConversationScript"/>).
    /// Mid-shift, Stef asks about strange timestamps, shares an audit log tied to the player's own
    /// login, and is interrupted by Charm; at the end of the shift she reports an authorized account
    /// and the connection drops. Closes with DialogResult.OK when the player finishes the thread;
    /// the evidence flags are written to the profile's AnomalyFlags.
    /// </summary>
    public class StefSceneForm : BaseGameForm
    {
        // Monitor art and the inner CRT rectangle in its native pixels (same art as the shift terminal)
        private static readonly (string File, Rectangle Screen)[] MonitorArt =
        {
            ("computerscreenforcall.png", new Rectangle(28, 44, 597, 326)),
            ("computer_desk_ui.jpg", new Rectangle(55, 114, 909, 630))
        };

        private readonly OperatorProfile? _profile;
        private readonly ConversationScript _script;
        private readonly SceneClock _clock;
        private readonly DialogueRunner _runner;
        private readonly Dictionary<string, Image?> _sprites = new();
        private readonly Random _rng = new();

        // Scene controls
        private readonly CrtPanel _crt = new();
        private readonly MessengerWindow _messenger = new();
        private readonly AuditLogWindow _audit = new() { Visible = false };
        private readonly CharacterPortrait _stef = new() { Visible = false };
        private readonly CharacterPortrait _charm = new() { Visible = false };
        private readonly RoomCaption _caption = new() { Visible = false };
        private readonly BevelButton _btnBack = new() { Text = "‹ BACK" };
        private readonly List<RoomChoiceButton> _roomChoices = new();

        // Timers: message pump (typing delays), animations, terminal clock
        private readonly System.Windows.Forms.Timer _pump = new();
        private readonly System.Windows.Forms.Timer _anim = new() { Interval = 16 };
        private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 500 };

        private Image? _imgDesk;
        private Rectangle _imgScreen;
        private Rectangle _imgRect;
        private MessengerTheme? _theme;

        // Animated scene state (0 = hidden/closed, 1 = shown/open)
        private float _stefT, _stefTarget;
        private float _charmT, _charmTarget;
        private float _auditT, _auditTarget;

        private bool _holdForAudit;
        private int _extraDelay;
        private int _clockGlitchTicks;

        private string OperatorName => _profile?.OperatorName ?? "OPERATOR";

        public IReadOnlyCollection<string> EvidenceFlags => _runner.EvidenceFlags;

        public StefSceneForm(OperatorProfile? profile = null, ConversationScript? script = null)
        {
            _profile = profile;
            _script = script ?? StefConversation.MidShift();
            _clock = new SceneClock(DateTime.Today.Add(_script.ClockStart));
            _runner = new DialogueRunner(_script.Nodes, () => _clock.Now);

            Text = "Stay On The Line - Shift 03: Records Archive";
            KeyPreview = true;

            LoadAssets();
            BuildScene();
            WireEvents();
        }

        // -----------------------------------------------------------------
        // Setup
        // -----------------------------------------------------------------

        private void LoadAssets()
        {
            foreach (var (file, screen) in MonitorArt)
            {
                _imgDesk = TryLoadImage(file);
                if (_imgDesk != null)
                {
                    _imgScreen = screen;
                    break;
                }
            }
            _stef.Sprite = GetSprite("stef_neutral");
            _charm.Sprite = GetSprite("charm_default");
        }

        private void BuildScene()
        {
            _crt.Controls.Add(_messenger);
            _crt.Controls.Add(_audit);
            Controls.AddRange(new Control[] { _crt, _stef, _charm, _caption, _btnBack });

            // Back to front: CRT, portraits, caption, back button
            _stef.BringToFront();
            _charm.BringToFront();
            _caption.BringToFront();
            _btnBack.BringToFront();
        }

        private void WireEvents()
        {
            var response = _messenger.Response;
            response.ChoiceSelected += OnChoiceSelected;
            response.ActionInvoked += (s, e) => OnPlayerAction();
            response.SaveLogRequested += (s, e) => OnSaveLog();
            response.AuditLogRequested += (s, e) => OnAuditButton();

            _messenger.Thread.Click += (s, e) => SkipWait();
            _caption.Click += (s, e) => SkipWait();

            _audit.RevealCompleted += (s, e) =>
            {
                if (!_holdForAudit) return;
                _holdForAudit = false;
                _extraDelay = 500;
                ScheduleNext();
            };
            _audit.CloseRequested += (s, e) => CloseAuditLog();

            _btnBack.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            _crt.Paint += PaintCrt;
            _pump.Tick += (s, e) => DeliverNext();
            _anim.Tick += (s, e) => Animate();
            _clockTimer.Tick += (s, e) =>
            {
                if (_clockGlitchTicks > 0) _clockGlitchTicks--;
                if (_theme != null) _crt.Invalidate(new Rectangle(0, 0, _crt.Width, _theme.S(40)));
            };

            Resize += (s, e) => LayoutScene();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LayoutScene();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AudioManager.Instance.PlayAmbientLoop();
            _clockTimer.Start();

            // Stef slides in while the first line is on its way
            _stefTarget = 1f;
            _anim.Start();
            _runner.Start(_script.StartNodeId);
            _extraDelay = 900;
            ScheduleNext();
        }

        // -----------------------------------------------------------------
        // Message pump: one line at a time with a realistic delay before each
        // -----------------------------------------------------------------

        private void ScheduleNext()
        {
            if (_holdForAudit || IsDisposed) return;

            var next = _runner.PeekNext();
            if (next == null)
            {
                PresentPlayerTurn();
                return;
            }

            bool stefTyping = next.Sender == ChatSender.Stef;
            _messenger.Response.ShowWaiting(next.Channel == ChatChannel.Room ? "SOMEONE IS AT YOUR DESK"
                : stefTyping ? "STEF IS TYPING" : "TRANSMITTING");
            _messenger.Thread.SetTyping(stefTyping);

            _pump.Interval = Math.Max(1, DelayBefore(next) + _extraDelay);
            _extraDelay = 0;
            _pump.Start();
        }

        private static int DelayBefore(ChatMessage line) => line.Sender switch
        {
            ChatSender.Stef => Math.Clamp(650 + line.Text.Length * 22, 900, 2800),
            ChatSender.Player => 380,
            ChatSender.System => 520,
            ChatSender.Narration => 1000,
            ChatSender.Charm => 700,
            _ => 600
        };

        private void DeliverNext()
        {
            _pump.Stop();
            _messenger.Thread.SetTyping(false);

            var line = _runner.DeliverNext();
            if (line != null)
            {
                Render(line);
                HandleCue(line.Cue);
            }
            ScheduleNext();
        }

        /// <summary>Space, Enter or a click hurries the current wait along.</summary>
        private void SkipWait()
        {
            if (_caption.Visible && _caption.IsTyping) _caption.CompleteTyping();
            if (_pump.Enabled && !_holdForAudit) DeliverNext();
        }

        private void Render(ChatMessage line)
        {
            if (line.Portrait != null)
            {
                var sprite = GetSprite(line.Portrait);
                if (line.Portrait.StartsWith("charm", StringComparison.OrdinalIgnoreCase)) _charm.Sprite = sprite;
                else _stef.Sprite = sprite;
            }

            if (line.Channel == ChatChannel.Room)
            {
                _caption.ShowLine(line.Sender == ChatSender.Narration ? string.Empty : line.SpeakerName, line.Text);
                _caption.BringToFront();
                _btnBack.BringToFront();
                _extraDelay = 600 + line.Text.Length * 38; // time to read the spoken line
                return;
            }

            _messenger.Thread.Append(line);
            switch (line.Sender)
            {
                case ChatSender.Stef:
                    AudioManager.Instance.PlayFrequencyBlip();
                    break;
                case ChatSender.System when line.IsAlert:
                    AudioManager.Instance.PlayBurnHiss();
                    break;
            }
        }

        private void HandleCue(SceneCue cue)
        {
            if (cue == SceneCue.None) return;

            if ((cue & SceneCue.AuditNotification) != 0)
            {
                _messenger.Response.AuditLogAvailable = true;
                _messenger.Response.PulseAuditButton();
                AudioManager.Instance.PlayTypewriterBell();
            }
            if ((cue & SceneCue.SignalGlitch) != 0)
            {
                _messenger.TriggerGlitch();
                _clockGlitchTicks = 6;
            }
            if ((cue & SceneCue.CloseAuditLog) != 0) CloseAuditLog();
            if ((cue & SceneCue.CharmEnters) != 0)
            {
                _charmTarget = 1f;
                _stefTarget = 0f;
            }
            if ((cue & SceneCue.CharmLeaves) != 0) _charmTarget = 0f;
            if ((cue & SceneCue.StefReturns) != 0)
            {
                _stefTarget = 1f;
                _caption.Visible = false;
            }
            if ((cue & SceneCue.ConnectionLost) != 0)
            {
                // Stef drops off the line: her status dies, the screen stutters and she slides away
                _messenger.SetStatus("USER UNAVAILABLE", alert: true);
                _messenger.TriggerGlitch();
                _clockGlitchTicks = 8;
                _stefTarget = 0f;
            }
            _anim.Start();
        }

        /// <summary>Queue is empty: show choices, the required action, or the end card.</summary>
        private void PresentPlayerTurn()
        {
            _messenger.Thread.SetTyping(false);
            var node = _runner.CurrentNode;
            if (node == null) return;

            var response = _messenger.Response;
            if (_runner.IsAwaitingChoice && node.ChoicesInRoom)
            {
                // Answered out loud: buttons under the speaker's dialogue box, messenger stays locked
                response.ShowWaiting("RESPOND OUT LOUD");
                ShowRoomChoices(node.Choices);
            }
            else if (_runner.IsAwaitingChoice)
            {
                response.ShowChoices(node.ChoicePrompt, node.Choices);
            }
            else if (_runner.IsAwaitingAction)
            {
                var action = node.Action!;
                response.ShowAction(action.Title, action.Description, action.ButtonText, "ACTION REQUIRED");
            }
            else if (_runner.IsFinished)
            {
                if (_script.EndStatus != null) _messenger.SetStatus(_script.EndStatus, alert: false);
                response.ShowAction(_script.EndTitle, _script.EndBody, _script.EndButton, _messenger.Status);
            }
        }

        // -----------------------------------------------------------------
        // Player input
        // -----------------------------------------------------------------

        private void OnChoiceSelected(object? sender, int index)
        {
            if (!_runner.IsAwaitingChoice) return;
            ClearRoomChoices();
            _runner.Choose(index);
            ScheduleNext();
        }

        private void ShowRoomChoices(IReadOnlyList<DialogueChoice> choices)
        {
            ClearRoomChoices();
            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                var button = new RoomChoiceButton { Text = choices[i].Text };
                button.Click += (s, e) => OnChoiceSelected(this, index);
                _roomChoices.Add(button);
                Controls.Add(button);
                button.BringToFront();
            }
            _caption.CompleteTyping();
            LayoutScene();
        }

        private void ClearRoomChoices()
        {
            if (_roomChoices.Count == 0) return;
            foreach (var button in _roomChoices)
            {
                Controls.Remove(button);
                button.Dispose();
            }
            _roomChoices.Clear();
            LayoutScene();
        }

        private void OnPlayerAction()
        {
            if (_runner.IsFinished)
            {
                FinishScene();
                return;
            }
            if (!_runner.IsAwaitingAction) return;

            var action = _runner.CurrentNode!.Action!;
            _runner.CompleteAction();

            if ((action.Cue & SceneCue.OpenAuditLog) != 0)
            {
                _messenger.Response.ShowWaiting("REVIEWING AUDIT LOG");
                OpenAuditLog();
                _holdForAudit = true; // Stef waits until the log has finished printing
            }
            HandleCue(action.Cue & ~SceneCue.OpenAuditLog);
            ScheduleNext();
        }

        private bool AwaitingAuditAction =>
            _runner.IsAwaitingAction && (_runner.CurrentNode!.Action!.Cue & SceneCue.OpenAuditLog) != 0;

        private void OnAuditButton()
        {
            if (AwaitingAuditAction) OnPlayerAction();
            else if (_auditTarget > 0f) CloseAuditLog();
            else OpenAuditLog();
        }

        private void OnSaveLog()
        {
            ChatMessage confirmation;
            try
            {
                string path = TranscriptArchiver.Save(_runner.History, _profile);
                _runner.AddFlag(StefConversation.FlagThreadPreserved);
                confirmation = new ChatMessage(ChatSender.System, $"LOCAL COPY SAVED: {Path.GetFileName(path)}");
                AudioManager.Instance.PlayInkStamp();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                confirmation = new ChatMessage(ChatSender.System, "COPY FAILED: ARCHIVE IS WRITE-PROTECTED.") { IsAlert = true };
            }
            _messenger.Thread.Append(_runner.Record(confirmation));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int option = e.KeyCode switch
            {
                Keys.D1 or Keys.NumPad1 => 0,
                Keys.D2 or Keys.NumPad2 => 1,
                Keys.D3 or Keys.NumPad3 => 2,
                _ => -1
            };

            if (option >= 0)
            {
                if (option < _roomChoices.Count)
                {
                    OnChoiceSelected(this, option);
                    e.Handled = true;
                }
                else
                {
                    e.Handled = _messenger.Response.TrySelect(option);
                }
            }
            else if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                if (!_messenger.Response.TryInvokeAction()) SkipWait();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void FinishScene()
        {
            if (_profile != null)
            {
                foreach (string flag in _runner.EvidenceFlags)
                    if (!_profile.AnomalyFlags.Contains(flag)) _profile.AnomalyFlags.Add(flag);
                if (_script.SeenFlag.Length > 0 && !_profile.AnomalyFlags.Contains(_script.SeenFlag))
                    _profile.AnomalyFlags.Add(_script.SeenFlag);
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        // -----------------------------------------------------------------
        // Audit log + animation
        // -----------------------------------------------------------------

        private void OpenAuditLog()
        {
            _audit.Open(AuditLogRepository.GetRecordsFor(OperatorName), OperatorName);
            _audit.Visible = true;
            _auditTarget = 1f;
            _anim.Start();
            AudioManager.Instance.PlayPaperRustle();
        }

        private void CloseAuditLog()
        {
            if (_auditTarget <= 0f) return;
            _auditTarget = 0f;
            _anim.Start();
        }

        private void Animate()
        {
            bool moving = Step(ref _stefT, _stefTarget, 0.055f)
                        | Step(ref _charmT, _charmTarget, 0.055f)
                        | Step(ref _auditT, _auditTarget, 0.07f);

            if (_auditT <= 0f && _auditTarget <= 0f && _audit.Visible) _audit.Visible = false;

            if (moving) LayoutScene();
            else _anim.Stop();
        }

        private static bool Step(ref float value, float target, float speed)
        {
            if (Math.Abs(value - target) < 0.001f) return false;
            value = value < target ? Math.Min(target, value + speed) : Math.Max(target, value - speed);
            return true;
        }

        private static int Lerp(int a, int b, float t) => (int)Math.Round(a + (b - a) * t);

        // -----------------------------------------------------------------
        // Layout and painting
        // -----------------------------------------------------------------

        private void LayoutScene()
        {
            int W = ClientSize.Width, H = ClientSize.Height;
            if (W < 100 || H < 100) return;

            // 1. Monitor art (contain-fit) and the CRT screen inside it
            Rectangle screen;
            if (_imgDesk != null)
            {
                _imgRect = ContainFit(_imgDesk.Size, ClientRectangle);
                float f = (float)_imgRect.Width / _imgDesk.Width;
                screen = new Rectangle(_imgRect.X + (int)(_imgScreen.X * f), _imgRect.Y + (int)(_imgScreen.Y * f),
                                       (int)(_imgScreen.Width * f), (int)(_imgScreen.Height * f));
            }
            else
            {
                _imgRect = ClientRectangle;
                screen = new Rectangle((int)(W * 0.08f), (int)(H * 0.10f), (int)(W * 0.84f), (int)(H * 0.64f));
            }
            _crt.Bounds = screen;

            // 2. Messenger (+ audit log sliding in from the right) inside the CRT
            float u = screen.Height / 600f;
            int header = (int)(38 * u), side = (int)(16 * u);
            int x0 = side, y0 = header;
            int w0 = screen.Width - side * 2, h0 = screen.Height - header - (int)(12 * u);
            EnsureTheme(h0 / 560f);

            float p = Shapes.Ease(_auditT);
            int auditW = (int)(w0 * 0.45f);
            int messengerW = (int)(w0 - (auditW + side * 0.6f) * p);
            _messenger.Bounds = new Rectangle(x0, y0, messengerW, h0);
            int auditX = x0 + w0 - auditW + (int)((1f - p) * (auditW + side * 2));
            _audit.Bounds = new Rectangle(auditX, y0, auditW, h0);

            // 3. Portraits: Stef at the left edge of the thread, Charm standing at the right
            int ph = (int)(H * 0.84f), pw = (int)(ph * 0.75f);
            var portraitSize = new Size(pw, ph);
            if (_stef.Size != portraitSize) _stef.Size = portraitSize;
            if (_charm.Size != portraitSize) _charm.Size = portraitSize;

            int threadLeft = screen.X + x0 + _messenger.ThreadLeft;
            int stefShown = Math.Max(-(int)(pw * 0.38f), threadLeft - (int)(pw * 0.85f));
            _stef.Location = new Point(Lerp(-pw - 8, stefShown, Shapes.Ease(_stefT)), H - ph);
            _stef.Visible = _stefT > 0.001f;

            int charmShown = Math.Min(W - (int)(pw * 0.80f), screen.Right - (int)(pw * 0.42f));
            _charm.Location = new Point(Lerp(W + 8, charmShown, Shapes.Ease(_charmT)), H - ph);
            _charm.Visible = _charmT > 0.001f;

            // 4. Room caption on the desk under the monitor, ending where Charm's sprite begins
            int capW = Math.Min(W - 32, (int)(_imgRect.Width * 0.62f));
            int capH = Math.Max(110, (int)(H * 0.15f));
            int capX = _imgRect.X + (_imgRect.Width - capW) / 2;
            int capRightLimit = charmShown + (int)(pw * 0.12f);
            if (capX + capW > capRightLimit) capX = Math.Max(16, capRightLimit - capW);
            capW = Math.Min(capW, capRightLimit - capX);
            capW = Math.Max(200, capW);
            int bottomMargin = (int)(H * 0.035f);

            // Spoken choices sit in a row under the caption, which moves up to make room
            if (_roomChoices.Count > 0)
            {
                int gap = Math.Max(8, (int)(capH * 0.08f));
                int choiceH = Math.Max(40, (int)(capH * 0.42f));
                int choiceW = (capW - gap * (_roomChoices.Count - 1)) / _roomChoices.Count;
                int choiceY = H - bottomMargin - choiceH;
                for (int i = 0; i < _roomChoices.Count; i++)
                    _roomChoices[i].Bounds = new Rectangle(capX + i * (choiceW + gap), choiceY, choiceW, choiceH);
                bottomMargin += choiceH + gap;
            }
            _caption.Bounds = new Rectangle(capX, H - capH - bottomMargin, capW, capH);

            // 5. Back button on the monitor bezel, above the screen's top-right corner
            int bezel = Math.Max(0, screen.Y - _imgRect.Y);
            int btnH = Math.Clamp((int)(bezel * 0.5f), 24, 40);
            int btnW = (int)(btnH * 3.2f);
            _btnBack.Bounds = new Rectangle(screen.Right - btnW, Math.Max(8, screen.Y - btnH - (int)(6 * u)), btnW, btnH);

            Invalidate();
        }

        private void EnsureTheme(float scale)
        {
            if (_theme != null && Math.Abs(_theme.Scale - scale) < 0.01f) return;
            var old = _theme;
            _theme = new MessengerTheme(scale);
            MessengerTheme.ApplyTo(_crt, _theme);
            MessengerTheme.ApplyTo(_btnBack, _theme);
            old?.Dispose();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(Color.Black);
            if (_imgDesk == null) return;

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(_imgDesk, _imgRect);

            // Darken the room so the monitor and characters carry the light
            using var path = new GraphicsPath();
            path.AddRectangle(ClientRectangle);
            using var vignette = new PathGradientBrush(path)
            {
                CenterColor = Color.FromArgb(0, 0, 0, 0),
                SurroundColors = new[] { Color.FromArgb(150, 0, 0, 0) },
                FocusScales = new PointF(0.6f, 0.5f)
            };
            g.FillRectangle(vignette, ClientRectangle);
        }

        /// <summary>CRT backdrop and the night-desk status strip above the messenger.</summary>
        private void PaintCrt(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rc = _crt.ClientRectangle;
            g.Clear(Color.FromArgb(14, 18, 15));
            if (_theme == null) return;
            Shapes.Prepare(g);

            float u = rc.Height / 600f;
            var strip = new RectangleF(16 * u, 0, rc.Width - 32 * u, 38 * u);
            using var ink = new SolidBrush(MessengerTheme.CrtInk);

            g.DrawString("NIGHT DESK  //  SHIFT 03", _theme.Caps, ink, strip, Shapes.Left);
            g.DrawString(_script.CallLabel, _theme.Caps, ink, strip, Shapes.Center);

            // The clock occasionally shows seconds that cannot exist
            var now = _clock.Now;
            string clock = _clockGlitchTicks > 0 ? $"{now:HH:mm}:{60 + _rng.Next(40)}" : now.ToString("HH:mm:ss");
            using var clockBrush = new SolidBrush(_clockGlitchTicks > 0 ? Color.FromArgb(232, 90, 84) : MessengerTheme.CrtInk);
            g.DrawString(clock, _theme.Mono, clockBrush, strip, Shapes.Right);

            const string link = "RECORDS LINK · OPEN";
            SizeF clockSize = g.MeasureString("00:00:00", _theme.Mono);
            SizeF linkSize = g.MeasureString(link, _theme.Caps);
            var box = new RectangleF(strip.Right - clockSize.Width - 24 * u - linkSize.Width - 12 * u,
                                     strip.Y + (strip.Height - linkSize.Height - 6 * u) / 2f,
                                     linkSize.Width + 12 * u, linkSize.Height + 6 * u);
            using (var pen = new Pen(MessengerTheme.CrtInk)) g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
            g.DrawString(link, _theme.Caps, ink, box, Shapes.Center);

            using var scan = new Pen(Color.FromArgb(22, 0, 0, 0));
            for (int y = 0; y < rc.Height; y += 3) g.DrawLine(scan, 0, y, rc.Width, y);
        }

        // -----------------------------------------------------------------
        // Assets
        // -----------------------------------------------------------------

        private Image? GetSprite(string key)
        {
            if (!_sprites.TryGetValue(key, out var image))
            {
                image = TryLoadImage(key + ".png") ?? TryLoadImage(Path.Combine("Characters", key + ".png"));
                _sprites[key] = image;
            }
            return image;
        }

        private static Image? TryLoadImage(string relativePath)
        {
            foreach (string baseDir in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                string path = Path.Combine(baseDir, "Assets", relativePath);
                if (!File.Exists(path)) continue;
                try { return Image.FromFile(path); } catch { }
            }
            return null;
        }

        private static Rectangle ContainFit(Size image, Rectangle target)
        {
            float k = Math.Min((float)target.Width / image.Width, (float)target.Height / image.Height);
            int w = (int)(image.Width * k), h = (int)(image.Height * k);
            return new Rectangle(target.X + (target.Width - w) / 2, target.Y + (target.Height - h) / 2, w, h);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pump.Dispose();
                _anim.Dispose();
                _clockTimer.Dispose();
                _theme?.Dispose();
                _imgDesk?.Dispose();
                foreach (var sprite in _sprites.Values) sprite?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>Double-buffered host for the messenger and audit log.</summary>
        private sealed class CrtPanel : Panel
        {
            public CrtPanel()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = Color.FromArgb(14, 18, 15);
            }
        }
    }
}
