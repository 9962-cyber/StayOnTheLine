using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using StayOnTheLine.Models;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Controls
{
    /// <summary>
    /// OOP Principles Demonstrated:
    /// 1. INHERITANCE: Inherits from System.Windows.Forms.Control to provide specialized UI rendering.
    /// 2. ENCAPSULATION: Encapsulates operator profile data, flicker state animation machine,
    ///    timer cycles, and audio cues within private fields with controlled public lifecycle.
    /// 3. POLYMORPHISM: Overrides OnPaint, OnKeyDown, OnMouseDown, and Dispose to create custom
    ///    analog CRT terminal card presentation and electrical screen glitch transitions.
    /// 4. ABSTRACTION: Decouples notification via VerificationCompleted event, allowing the parent
    ///    form to coordinate subsequent scene transitions without tight coupling.
    /// </summary>
    public class OperatorVerificationOverlay : Control
    {
        // Encapsulation: Private backing fields
        private readonly OperatorProfile _profile;
        private readonly System.Windows.Forms.Timer _stateTimer;
        private int _step = 0;
        private bool _isFlickering = false;
        private float _flickerOpacity = 0.0f; // 0 = normal, 1 = total black flash
        private int _flickerTick = 0;
        private bool _completed = false;

        // Abstraction: Decoupled event notification
        public event Action? VerificationCompleted;

        public OperatorVerificationOverlay(OperatorProfile profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));

            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = Color.Black;
            Dock = DockStyle.Fill;
            Cursor = Cursors.Hand;
            TabStop = true;

            _stateTimer = new System.Windows.Forms.Timer { Interval = 75 };
            _stateTimer.Tick += StateTimer_Tick;
            _stateTimer.Start();

            // Play terminal verification audio cue
            try
            {
                AudioManager.Instance.PlayFrequencyBlip();
            }
            catch { }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        // OOP Principle: POLYMORPHISM (Custom rendering of dispatch verification card & flicker)
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = ClientRectangle.Width;
            int h = ClientRectangle.Height;
            if (w <= 0 || h <= 0) return;

            // 1. Dark Vignette Backdrop (Darkens the desk underneath)
            using (var path = new GraphicsPath())
            {
                path.AddRectangle(new Rectangle(0, 0, w, h));
                using var pgb = new PathGradientBrush(path)
                {
                    CenterColor = Color.FromArgb(180, 5, 8, 7),
                    SurroundColors = new[] { Color.FromArgb(245, 0, 0, 0) },
                    FocusScales = new PointF(0.6f, 0.5f)
                };
                g.FillRectangle(pgb, 0, 0, w, h);
            }

            // 2. Centered 1996 CRT Dispatch Terminal Card (proportionally fitted, zero dead space)
            float s = Math.Max(0.75f, Math.Min(w / 1920f, h / 1080f));
            int cardW = Math.Max(740, (int)(780 * s));
            int cardH = (int)(292 * s);
            if (cardW > w - 40) cardW = w - 40;
            if (cardH > h - 40) cardH = h - 40;
            int cardX = (w - cardW) / 2;
            int cardY = (h - cardH) / 2;
            Rectangle cardRect = new Rectangle(cardX, cardY, cardW, cardH);

            // Card body background (dark analog CRT green-black)
            using (var brush = new SolidBrush(Color.FromArgb(240, 10, 16, 12)))
                g.FillRectangle(brush, cardRect);

            // Double border: outer green/amber phosphor, inner subtle line
            using (var outerPen = new Pen(Color.FromArgb(220, 140, 200, 160), 2f))
            using (var innerPen = new Pen(Color.FromArgb(120, 60, 120, 90), 1f))
            {
                g.DrawRectangle(outerPen, cardRect);
                g.DrawRectangle(innerPen, cardX + 6, cardY + 6, cardW - 12, cardH - 12);
            }

            // 3. Header bar
            int headerH = (int)(54 * s);
            Rectangle headerRect = new Rectangle(cardX + 7, cardY + 7, cardW - 14, headerH);
            using (var hBrush = new SolidBrush(Color.FromArgb(60, 140, 200, 160)))
                g.FillRectangle(hBrush, headerRect);

            using (Font hFont = FontManager.GetSmallCapsFont(Math.Max(11f, 15f * s), FontStyle.Bold))
            using (Font titleFont = FontManager.GetSmallCapsFont(Math.Max(10f, 13f * s)))
            using (Font codeFont = new Font("Consolas", Math.Max(9f, 11f * s), FontStyle.Regular, GraphicsUnit.Point))
            using (Brush textWhite = new SolidBrush(Color.FromArgb(240, 248, 242)))
            using (Brush textDim = new SolidBrush(Color.FromArgb(160, 195, 175)))
            using (Brush greenGlow = new SolidBrush(Color.FromArgb(90, 220, 150)))
            {
                // Header Title
                string headerTitle = "DISPATCH LINE CONNECTED";
                g.DrawString(headerTitle, hFont, textWhite, cardX + (int)(20 * s), cardY + (int)(16 * s));

                // Right tag: Short and clean to guarantee NO text collision
                string headerSub = "TERMINAL 04";
                var subSize = g.MeasureString(headerSub, codeFont);
                int subX = cardX + cardW - (int)subSize.Width - (int)(20 * s);
                var titleSize = g.MeasureString(headerTitle, hFont);
                if (subX > cardX + (int)(20 * s) + titleSize.Width + 24)
                {
                    g.DrawString(headerSub, codeFont, textDim, subX, cardY + (int)(18 * s));
                }

                // 4. Card Details (Operator Verification Lines)
                int bodyY = cardY + headerH + (int)(22 * s);
                int padX = cardX + (int)(28 * s);
                int lineGap = (int)(32 * s);

                DrawInfoLine(g, "OPERATOR VERIFIED", _profile.OperatorName.ToUpperInvariant(), padX, bodyY, titleFont, textDim, textWhite, s);
                DrawInfoLine(g, "CLEARANCE LEVEL", "LEVEL 1 (PROVISIONAL DISPATCHER)", padX, bodyY + lineGap, titleFont, textDim, greenGlow, s);
                DrawInfoLine(g, "ASSIGNED SHIFT", $"SHIFT {_profile.CurrentShift} — TRAINING PROTOCOL", padX, bodyY + lineGap * 2, titleFont, textDim, textWhite, s);
                DrawInfoLine(g, "CALLS LOGGED", _profile.CallsLogged.ToString(), padX, bodyY + lineGap * 3, titleFont, textDim, textWhite, s);

                // Divider line
                int divY = bodyY + lineGap * 4 + (int)(12 * s);
                using (var divPen = new Pen(Color.FromArgb(70, 140, 200, 160), 1f))
                    g.DrawLine(divPen, padX, divY, cardX + cardW - (int)(28 * s), divY);

                // Bottom Callout
                string commMsg = "[ COMMENCING NIGHT DISPATCH SHIFT... ]";
                using (Font commFont = FontManager.GetSmallCapsFont(Math.Max(10f, 13f * s), FontStyle.Bold))
                {
                    g.DrawString(commMsg, commFont, greenGlow, padX, divY + (int)(12 * s));
                }

                string promptMsg = "[ CLICK ANYWHERE OR PRESS ENTER TO COMMENCE ]";
                using (Font promptFont = FontManager.GetItalicFont(Math.Max(8.5f, 10f * s)))
                using (Brush promptBrush = new SolidBrush(Color.FromArgb(120, 160, 140)))
                {
                    g.DrawString(promptMsg, promptFont, promptBrush, padX, divY + (int)(36 * s));
                }
            }

            // 5. Analog CRT Scanlines across the screen
            using (var scanPen = new Pen(Color.FromArgb(18, 0, 0, 0), 1f))
            {
                for (int y = 0; y < h; y += 4)
                    g.DrawLine(scanPen, 0, y, w, y);
            }

            // 6. Analog Screen Flicker / Blackout Glitch Flash
            if (_isFlickering && _flickerOpacity > 0f)
            {
                int alpha = (int)Math.Clamp(_flickerOpacity * 255f, 0f, 255f);
                using (var flickerBrush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                {
                    g.FillRectangle(flickerBrush, 0, 0, w, h);
                }

                // CRT Horizontal Glitch Beam during peak flicker
                if (_flickerOpacity > 0.4f && _flickerOpacity < 0.99f)
                {
                    int beamY = (int)((Math.Sin(_flickerTick * 1.5) * 0.5 + 0.5) * h);
                    using var beamBrush = new SolidBrush(Color.FromArgb((int)(alpha * 0.6f), 120, 255, 180));
                    g.FillRectangle(beamBrush, 0, beamY, w, (int)(12 * s));
                }
            }
        }

        private static void DrawInfoLine(Graphics g, string label, string value, int x, int y, Font font, Brush labelBrush, Brush valBrush, float s)
        {
            string fullLabel = label.PadRight(19) + ": ";
            g.DrawString(fullLabel, font, labelBrush, x, y);
            var labelSize = g.MeasureString(fullLabel, font);
            g.DrawString(value, font, valBrush, x + labelSize.Width, y);
        }

        private void StateTimer_Tick(object? sender, EventArgs e)
        {
            _step++;

            if (!_isFlickering)
            {
                // Card rests on screen. After ~18 ticks (approx 1.3s), initiate flicker
                if (_step > 18)
                {
                    TriggerFlicker();
                }
            }
            else
            {
                // Flicker animation state machine (analog electrical glitch)
                _flickerTick++;
                switch (_flickerTick)
                {
                    case 1: _flickerOpacity = 0.85f; break; // sudden blackout
                    case 2: _flickerOpacity = 0.15f; break; // quick flash
                    case 3: _flickerOpacity = 0.95f; break; // heavy glitch
                    case 4: _flickerOpacity = 0.30f; break; // dim flash
                    case 5: _flickerOpacity = 1.00f; break; // complete blackout
                    case 6:
                        _flickerOpacity = 1.00f;
                        FinishTransition();
                        break;
                }
                Invalidate();
            }
        }

        public void TriggerFlicker()
        {
            if (_isFlickering || _completed) return;
            _isFlickering = true;
            _flickerTick = 0;
            _stateTimer.Interval = 45; // faster flicker rate

            try
            {
                AudioManager.Instance.PlayFrequencyBlip();
            }
            catch { }
            Invalidate();
        }

        private void FinishTransition()
        {
            if (_completed) return;
            _completed = true;
            _stateTimer.Stop();
            _flickerOpacity = 1.0f; // Screen stays solidly black
            Invalidate();
            Update();

            VerificationCompleted?.Invoke();
        }

        // OOP Principle: POLYMORPHISM (User Interaction)
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            TriggerFlicker();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.Escape)
            {
                TriggerFlicker();
                e.Handled = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stateTimer.Stop();
                _stateTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
