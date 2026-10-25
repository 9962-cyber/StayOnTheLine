using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using StayOnTheLine.Models;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// Before every shift: the scenario's optional opening scene in the room, then the Shift Briefing
    /// book (operator, units, expected calls, procedure, a margin note) that the player closes with
    /// CLOCK IN. Contents come from <see cref="IShiftScenario.Briefing"/>, so each shift brings its own.
    /// Everything on the pages is set in IM FELL DW Pica SC.
    /// </summary>
    public partial class ShiftPlayForm
    {
        private Panel? _pnlShiftBook;
        private Rectangle _clockInRect;
        private bool _clockInHovered;
        private readonly Dictionary<string, Image?> _sceneSprites = new();

        private static readonly Color BookInk = Color.FromArgb(38, 28, 20);
        private static readonly Color BookMuted = Color.FromArgb(112, 94, 72);
        private static readonly Color BookRule = Color.FromArgb(70, 120, 100, 80);
        private static readonly Color BookNote = Color.FromArgb(148, 34, 28);

        /// <summary>True while the briefing book is up; calls don't ring until the player clocks in.</summary>
        private bool ShiftBookOpen => _pnlShiftBook != null;

        private void BeginShiftBriefing()
        {
            var scene = Scenario.Briefing.OpeningScene;
            if (scene.Count > 0) PlayRoomScene(scene, ShowShiftBriefingBook);
            else ShowShiftBriefingBook();
        }

        // ──────────────────────────────────────────────────────────────
        // In-room scenes (same scene box Charm uses during calls)
        // ──────────────────────────────────────────────────────────────

        private Image? SceneSprite(string key)
        {
            if (!_sceneSprites.TryGetValue(key, out var img))
            {
                img = TryLoadImage(key + ".png");
                _sceneSprites[key] = img;
            }
            return img;
        }

        /// <summary>Plays lines as a full-screen room scene; <paramref name="onFinished"/> runs once it closes.</summary>
        private void PlayRoomScene(IEnumerable<BriefingSceneLine> lines, Action? onFinished = null)
        {
            var list = lines.ToList();
            if (list.Count == 0)
            {
                onFinished?.Invoke();
                return;
            }

            _charmDialogue.Clear();
            _charmDialogue.Docked = false;
            LayoutCharmDialogue();

            if (onFinished != null)
            {
                void Handler(object? s, EventArgs e)
                {
                    _charmDialogue.Finished -= Handler;
                    onFinished();
                }
                _charmDialogue.Finished += Handler;
            }

            foreach (var line in list)
            {
                string text = line.Text.Replace("{operator}", Profile.OperatorName);
                _charmDialogue.Enqueue(line.Speaker, text, line.Sprite != null ? SceneSprite(line.Sprite) : null, line.Tag);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Shift Briefing book
        // ──────────────────────────────────────────────────────────────

        private void ShowShiftBriefingBook()
        {
            AudioManager.Instance.PlayPaperRustle();
            AudioManager.Instance.StopPhoneRing();

            _pnlShiftBook = new Panel { Bounds = ClientRectangle, BackColor = Color.FromArgb(8, 6, 5) };
            EnableDoubleBuffering(_pnlShiftBook);
            _pnlShiftBook.Paint += PaintShiftBook;

            // CLOCK IN is drawn on the page itself (an inked box), so it is hit-tested here
            _pnlShiftBook.MouseMove += (s, e) =>
            {
                bool over = _clockInRect.Contains(e.Location);
                if (over == _clockInHovered) return;
                _clockInHovered = over;
                _pnlShiftBook.Cursor = over ? Cursors.Hand : Cursors.Default;
                if (over) AudioManager.Instance.PlayKeyClick();
                _pnlShiftBook.Invalidate(Rectangle.Inflate(_clockInRect, 6, 6));
            };
            _pnlShiftBook.MouseClick += (s, e) => { if (_clockInRect.Contains(e.Location)) ClockIn(); };
            _pnlShiftBook.Resize += (s, e) => _pnlShiftBook.Invalidate();
            Resize += (s, e) => { if (_pnlShiftBook != null) _pnlShiftBook.Bounds = ClientRectangle; };

            Controls.Add(_pnlShiftBook);
            ApplyResponsiveLayout(); // keeps the book above the desk controls
        }

        private void ClockIn()
        {
            if (_pnlShiftBook == null) return;
            // Definitive audio transition: kill any lingering menu audio and start gameplay atmosphere
            AudioManager.Instance.StopMenuAmbience();
            AudioManager.Instance.StartGameplayAtmosphere();
            AudioManager.Instance.PlayInkStamp();
            Controls.Remove(_pnlShiftBook);
            _pnlShiftBook.Dispose();
            _pnlShiftBook = null;
            _clockInHovered = false;

            if (Scenario.ShiftNumber == 3)
                _lblSupervisorBar.Text = "SYSTEM: RECORDS TERMINAL: 1 UNREAD MESSAGE.";
            ApplyResponsiveLayout(); // lay the desk out again now that the book is gone
            UpdatePhoneRing();       // the first line starts ringing now
        }

        /// <summary>The two pages inside briefing_book.png (1920x1080 art), in screen pixels.</summary>
        private (Rectangle Book, Rectangle Left, Rectangle Right, float Scale) ShiftBookGeometry()
        {
            var rc = _pnlShiftBook!.ClientRectangle;
            // The book sits on the desk at 85% of the screen instead of filling it
            var area = Rectangle.Inflate(rc, -(int)(rc.Width * 0.075f), -(int)(rc.Height * 0.075f));
            var book = _imgShiftBriefingBook != null ? GetContainFitRect(_imgShiftBriefingBook, area) : area;
            float s = book.Width / 1920f;
            var left = new Rectangle(book.X + (int)(430 * s), book.Y + (int)(100 * s), (int)(505 * s), (int)(860 * s));
            var right = new Rectangle(book.X + (int)(1045 * s), book.Y + (int)(100 * s), (int)(500 * s), (int)(860 * s));
            return (book, left, right, s);
        }

        private void PaintShiftBook(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            var rc = _pnlShiftBook!.ClientRectangle;
            if (rc.Width <= 0 || rc.Height <= 0) return;

            // The desk, dimmed, with the open book lying on it
            if (_imgDeskRoom != null) DrawImageCoverFit(g, _imgDeskRoom, rc);
            using (var wash = new SolidBrush(Color.FromArgb(200, 4, 6, 4))) g.FillRectangle(wash, rc);

            var (book, left, right, s) = ShiftBookGeometry();

            using (var shadow = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                g.FillRectangle(shadow, book.X + 300 * s, book.Y + 60 * s, 1380 * s, 960 * s);
            if (_imgShiftBriefingBook != null) g.DrawImage(_imgShiftBriefingBook, book);

            var briefing = Scenario.Briefing;
            int expectedCalls = _calls.Count(c => !c.IsRestricted);

            using var ink = new SolidBrush(BookInk);
            using var muted = new SolidBrush(BookMuted);
            using var rule = new Pen(BookRule, Math.Max(1f, 1.2f * s));
            using var tagFont = FontManager.GetSmallCapsFont(12.5f * s);
            using var titleFont = FontManager.GetSmallCapsFont(46f * s);

            // ── Left page: who is on shift tonight ──
            g.DrawString("NIGHT WATCH EMERGENCY SERVICES · FORM 08", tagFont, muted, left.X, left.Y);
            g.DrawString("Shift Briefing", titleFont, ink, left.X - 5 * s, left.Y + 26 * s);

            float y = left.Y + 165 * s;
            float valueX = left.X + 185 * s;
            g.DrawString("DISPATCHER 04", tagFont, muted, left.X, y);
            using (var nameFont = FontManager.GetSmallCapsFont(44f * s))
                g.DrawString(Profile.OperatorName, nameFont, ink, new RectangleF(left.X - 5 * s, y + 20 * s, left.Width, 80 * s));
            using (var roleFont = FontManager.GetSmallCapsFont(14f * s))
                g.DrawString("Probationary operator · Night desk", roleFont, muted, left.X, y + 102 * s);
            g.DrawLine(rule, left.X, y + 140 * s, left.Right - 10 * s, y + 140 * s);

            y += 172 * s;
            var rows = new[]
            {
                ("SHIFT", $"{briefing.ShiftLabel} / {briefing.Hours}"),
                ("UNITS", $"{Scenario.InitialUnits:D2} available"),
                ("EXPECTED", $"{expectedCalls:D2} calls"),
                ("TERMINAL", "04 · Night desk"),
                ("SUPERVISOR", System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Scenario.SupervisorName.ToLowerInvariant()))
            };
            using (var valueFont = FontManager.GetSmallCapsFont(23f * s))
            {
                foreach (var (key, val) in rows)
                {
                    g.DrawString(key, tagFont, muted, left.X, y + 14 * s);
                    g.DrawString(val, valueFont, ink, valueX, y);
                    g.DrawLine(rule, left.X, y + 54 * s, left.Right - 10 * s, y + 54 * s);
                    y += 78 * s;
                }
            }

            // ── Right page: tonight's procedure ──
            g.DrawString($"SUPERVISOR MEMO / {Scenario.SupervisorName}", tagFont, muted, right.X, right.Y);
            g.DrawString("Procedure", titleFont, ink, right.X - 5 * s, right.Y + 26 * s);

            float stepY = right.Y + 170 * s;
            using (var stepFont = FontManager.GetSmallCapsFont(18.5f * s))
            using (var numFont = FontManager.GetSmallCapsFont(16f * s, FontStyle.Bold))
            {
                for (int i = 0; i < briefing.Procedure.Count; i++)
                {
                    g.DrawString((i + 1).ToString("D2"), numFont, muted, right.X, stepY + 2 * s);
                    var box = new RectangleF(right.X + 62 * s, stepY, right.Width - 62 * s, 120 * s);
                    g.DrawString(briefing.Procedure[i], stepFont, ink, box);
                    float used = g.MeasureString(briefing.Procedure[i], stepFont, (int)box.Width).Height;
                    stepY += Math.Max(62 * s, used + 26 * s);
                }
            }

            // Handwritten margin note, in red, tilted like it was scribbled in a hurry
            using (var noteFont = FontManager.GetSmallCapsFont(21f * s))
            using (var noteBrush = new SolidBrush(BookNote))
            {
                var noteBox = new RectangleF(right.X + 20 * s, Math.Max(stepY + 40 * s, right.Y + 520 * s), right.Width - 40 * s, 230 * s);
                var state = g.Save();
                g.TranslateTransform(noteBox.X, noteBox.Y);
                g.RotateTransform(-5f);
                g.DrawString(briefing.Note, noteFont, noteBrush, new RectangleF(0, 0, noteBox.Width, noteBox.Height));
                g.Restore(state);
            }

            // [ CLOCK IN ] inked in the bottom corner of the right page
            using (var clockFont = FontManager.GetSmallCapsFont(17f * s))
            {
                const string label = "[ CLOCK IN ]";
                SizeF size = g.MeasureString(label, clockFont);
                int w = (int)(size.Width + 46 * s), h = (int)(size.Height + 22 * s);
                _clockInRect = new Rectangle(right.Right - w - (int)(18 * s), right.Bottom - h - (int)(24 * s), w, h);

                if (_clockInHovered)
                    using (var fill = new SolidBrush(Color.FromArgb(225, 38, 28, 20))) g.FillRectangle(fill, _clockInRect);
                using (var border = new Pen(Color.FromArgb(_clockInHovered ? 240 : 170, BookInk), Math.Max(1f, 1.4f * s)))
                    g.DrawRectangle(border, _clockInRect);
                using var labelBrush = new SolidBrush(_clockInHovered ? Color.FromArgb(240, 230, 210) : BookInk);
                using var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(label, clockFont, labelBrush, _clockInRect, center);
            }
        }
    }
}
