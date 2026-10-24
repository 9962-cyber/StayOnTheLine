using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StayOnTheLine.Models.Dialogue;

namespace StayOnTheLine.Controls.Messenger
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// The bottom of the messenger: "SELECT A RESPONSE BEFORE SENDING" with numbered options,
    /// a player-action card (OPEN AUDIT LOG), or a locked "awaiting reply" strip. The footer
    /// keeps SAVE LOG and, once Stef shares it, OPEN AUDIT LOG. It only raises events; the
    /// scene decides what they mean. <see cref="PreferredHeight"/> changes with the mode so the
    /// messenger can animate the panel open and closed.
    /// </summary>
    public sealed class ResponsePanel : Control, IThemedControl
    {
        private enum Mode { Waiting, Choices, Action }

        private readonly List<OptionRowButton> _options = new();
        private readonly BevelButton _btnAction = new() { Primary = true, Visible = false };
        private readonly BevelButton _btnSave = new() { Text = "SAVE LOG" };
        private readonly BevelButton _btnAudit = new() { Text = "OPEN AUDIT LOG", Visible = false };
        private readonly System.Windows.Forms.Timer _pulseTimer = new() { Interval = 40 };
        private MessengerTheme? _theme;
        private Mode _mode = Mode.Waiting;
        private string _header = "RESPONSES LOCKED";
        private string _status = "AWAITING REPLY";
        private string _actionTitle = string.Empty;
        private string _actionBody = string.Empty;
        private int _pulseFrames;

        public event EventHandler<int>? ChoiceSelected;
        public event EventHandler? ActionInvoked;
        public event EventHandler? SaveLogRequested;
        public event EventHandler? AuditLogRequested;
        public event EventHandler? PreferredHeightChanged;

        public ResponsePanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = MessengerTheme.PanelBack;

            _btnAction.Click += (s, e) => { if (_mode == Mode.Action) ActionInvoked?.Invoke(this, EventArgs.Empty); };
            _btnSave.Click += (s, e) => SaveLogRequested?.Invoke(this, EventArgs.Empty);
            _btnAudit.Click += (s, e) => { _pulseFrames = 0; _btnAudit.Pulse = 0; AuditLogRequested?.Invoke(this, EventArgs.Empty); };
            Controls.AddRange(new Control[] { _btnAction, _btnSave, _btnAudit });

            _pulseTimer.Tick += (s, e) =>
            {
                if (_pulseFrames <= 0) { _btnAudit.Pulse = 0; _pulseTimer.Stop(); return; }
                _pulseFrames--;
                _btnAudit.Pulse = 0.5f + 0.5f * (float)Math.Sin(_pulseFrames * 0.35f);
            };
        }

        /// <summary>Height the panel wants for its current mode (in pixels).</summary>
        public int PreferredHeight
        {
            get
            {
                if (_theme == null) return 0;
                return _mode switch
                {
                    Mode.Choices => _theme.S(38 + _options.Count * 36 + 40),
                    Mode.Action => _theme.S(138),
                    _ => _theme.S(78)
                };
            }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool AuditLogAvailable
        {
            get => _btnAudit.Visible;
            set { _btnAudit.Visible = value; LayoutChildren(); }
        }

        public void ShowWaiting(string status)
        {
            SetMode(Mode.Waiting, "RESPONSES LOCKED UNTIL REPLY", status);
        }

        public void ShowChoices(string prompt, IReadOnlyList<DialogueChoice> choices)
        {
            RebuildOptions(choices);
            SetMode(Mode.Choices, prompt, "CHOICE REQUIRED");
        }

        public void ShowAction(string title, string body, string buttonText, string status)
        {
            _actionTitle = title;
            _actionBody = body;
            _btnAction.Text = buttonText;
            SetMode(Mode.Action, string.Empty, status);
        }

        /// <summary>Keyboard shortcut (1/2/3) for the numbered options.</summary>
        public bool TrySelect(int index)
        {
            if (_mode != Mode.Choices || index < 0 || index >= _options.Count || !_options[index].Enabled) return false;
            Select(index);
            return true;
        }

        /// <summary>Makes the action button work from the keyboard (Enter).</summary>
        public bool TryInvokeAction()
        {
            if (_mode != Mode.Action) return false;
            ActionInvoked?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void PulseAuditButton()
        {
            _pulseFrames = 90;
            _pulseTimer.Start();
        }

        public void ApplyTheme(MessengerTheme theme)
        {
            _theme = theme;
            LayoutChildren();
            Invalidate();
            PreferredHeightChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RebuildOptions(IReadOnlyList<DialogueChoice> choices)
        {
            foreach (var old in _options)
            {
                Controls.Remove(old);
                old.Dispose();
            }
            _options.Clear();

            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                var row = new OptionRowButton { Number = (i + 1).ToString("D2"), Text = choices[i].Text };
                if (_theme != null) row.ApplyTheme(_theme);
                row.Click += (s, e) => Select(index);
                _options.Add(row);
                Controls.Add(row);
            }
        }

        private void Select(int index)
        {
            if (_mode != Mode.Choices) return;
            // Lock every option at once so a double click can't send twice
            foreach (var row in _options) row.Enabled = false;
            ChoiceSelected?.Invoke(this, index);
        }

        private void SetMode(Mode mode, string header, string status)
        {
            bool heightChanges = mode != _mode || mode == Mode.Choices;
            _mode = mode;
            _header = header;
            _status = status;
            LayoutChildren();
            Invalidate();
            if (heightChanges) PreferredHeightChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            if (_theme == null) return;
            var t = _theme;
            int pad = t.S(12);

            // Options fill the full width; they are only shown in choice mode
            for (int i = 0; i < _options.Count; i++)
            {
                _options[i].Visible = _mode == Mode.Choices;
                _options[i].Bounds = new Rectangle(pad, t.S(34) + i * t.S(36), Math.Max(10, Width - pad * 2), t.S(30));
            }

            _btnAction.Visible = _mode == Mode.Action;
            int actionW = t.S(250);
            _btnAction.Bounds = new Rectangle(Width - pad - actionW, t.S(22), actionW, t.S(46));

            // Footer row pinned to the bottom of the preferred height (not the animated height)
            int footerY = Math.Max(0, PreferredHeight - t.S(34));
            _btnSave.Bounds = new Rectangle(pad, footerY, t.S(92), t.S(24));
            _btnAudit.Bounds = new Rectangle(_btnSave.Right + t.S(8), footerY, t.S(160), t.S(24));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (_theme == null) return;
            var t = _theme;
            Shapes.Prepare(g);
            int pad = t.S(12);

            using (var top = new Pen(MessengerTheme.Border))
                g.DrawLine(top, 0, 0, Width, 0);

            using var muted = new SolidBrush(MessengerTheme.MutedInk);
            using var ink = new SolidBrush(MessengerTheme.Ink);

            if (_mode == Mode.Action)
            {
                using var titleBrush = new SolidBrush(Color.FromArgb(28, 58, 98));
                g.DrawString(_actionTitle, t.Caps, titleBrush, new RectangleF(pad, t.F(14), Width - pad * 2, t.F(18)), Shapes.Left);
                float textW = Math.Max(20, _btnAction.Left - pad * 2);
                g.DrawString(_actionBody, t.Body, ink, new RectangleF(pad, t.F(34), textW, t.F(56)));
            }
            else
            {
                g.DrawString(_header, t.Caps, muted, new RectangleF(pad, t.F(8), Width - pad * 2, t.F(20)), Shapes.Left);
            }

            int footerY = Math.Max(0, PreferredHeight - t.S(34));
            using var statusBrush = new SolidBrush(_mode == Mode.Choices ? MessengerTheme.AlertInk : MessengerTheme.MutedInk);
            g.DrawString(_status, t.Caps, statusBrush, new RectangleF(pad, footerY, Width - pad * 2, t.F(24)), Shapes.Right);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _pulseTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
