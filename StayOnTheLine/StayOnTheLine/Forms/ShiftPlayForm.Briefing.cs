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
    /// </summary>
    public partial class ShiftPlayForm
    {
        private Panel? _pnlShiftBook;
        private Label? _btnClockIn;
        private readonly Dictionary<string, Image?> _sceneSprites = new();

        private static readonly Color BookInk = Color.FromArgb(40, 30, 22);
        private static readonly Color BookMuted = Color.FromArgb(110, 92, 70);
        private static readonly Color BookRule = Color.FromArgb(90, 120, 100, 80);
        private static readonly Color BookNote = Color.FromArgb(150, 36, 30);

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

            foreach (var line in lines)
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

            _pnlShiftBook = new Panel { Bounds = ClientRectangle, BackColor = Color.FromArgb(20, 10, 8) };
            EnableDoubleBuffering(_pnlShiftBook);
            _pnlShiftBook.Paint += PaintShiftBook;

            _btnClockIn = new Label
            {
                Text = "[ CLOCK IN ]",
                ForeColor = BookInk,
                BackColor = Color.FromArgb(232, 220, 192),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _btnClockIn.MouseEnter += (s, e) =>
            {
                _btnClockIn.BackColor = Color.FromArgb(48, 36, 26);
                _btnClockIn.ForeColor = Color.FromArgb(245, 235, 215);
                AudioManager.Instance.PlayKeyClick();
            };
            _btnClockIn.MouseLeave += (s, e) =>
            {
                _btnClockIn.BackColor = Color.FromArgb(232, 220, 192);
                _btnClockIn.ForeColor = BookInk;
            };
            _btnClockIn.Click += (s, e) => ClockIn();

            _pnlShiftBook.Controls.Add(_btnClockIn);
            _pnlShiftBook.Resize += (s, e) => LayoutShiftBook();
            Resize += (s, e) => { if (_pnlShiftBook != null) _pnlShiftBook.Bounds = ClientRectangle; };

            Controls.Add(_pnlShiftBook);
            ApplyResponsiveLayout(); // keeps the book above the desk controls
            LayoutShiftBook();
        }

        private void ClockIn()
        {
            if (_pnlShiftBook == null) return;
            AudioManager.Instance.PlayInkStamp();
            Controls.Remove(_pnlShiftBook);
            _pnlShiftBook.Dispose();
            _pnlShiftBook = null;
            _btnClockIn = null;

            if (Scenario.ShiftNumber == 3)
                _lblSupervisorBar.Text = "SYSTEM: RECORDS TERMINAL: 1 UNREAD MESSAGE.";
            ApplyResponsiveLayout(); // lay the desk out again now that the book is gone
            UpdatePhoneRing();       // the first line starts ringing now
        }

        /// <summary>The two pages inside shiftbriefing_book.png (1920x1080 art), in screen pixels.</summary>
        private (Rectangle Book, Rectangle Left, Rectangle Right, float Scale) ShiftBookGeometry()
        {
            var rc = _pnlShiftBook!.ClientRectangle;
            var book = _imgShiftBriefingBook != null ? GetContainFitRect(_imgShiftBriefingBook, rc) : rc;
            float s = book.Width / 1920f;
            var left = new Rectangle(book.X + (int)(390 * s), book.Y + (int)(110 * s), (int)(580 * s), (int)(860 * s));
            var right = new Rectangle(book.X + (int)(1060 * s), book.Y + (int)(110 * s), (int)(580 * s), (int)(860 * s));
            return (book, left, right, s);
        }

        private void LayoutShiftBook()
        {
            if (_pnlShiftBook == null || _btnClockIn == null || _pnlShiftBook.Width <= 0) return;
            var (_, _, right, s) = ShiftBookGeometry();
            _btnClockIn.Font = FontManager.GetSmallCapsFont(Math.Max(8f, 15f * s), FontStyle.Bold);
            int w = (int)(200 * s), h = (int)(54 * s);
            _btnClockIn.Bounds = new Rectangle(right.Right - w - (int)(20 * s), right.Bottom - h - (int)(30 * s), w, h);
            _pnlShiftBook.Invalidate();
        }

        private void PaintShiftBook(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            var rc = _pnlShiftBook!.ClientRectangle;
            if (rc.Width <= 0 || rc.Height <= 0) return;

            if (_imgDeskRoom != null) DrawImageCoverFit(g, _imgDeskRoom, rc);
            using (var wash = new SolidBrush(Color.FromArgb(170, 4, 8, 4))) g.FillRectangle(wash, rc);

            var (book, left, right, s) = ShiftBookGeometry();
            if (_imgShiftBriefingBook != null) g.DrawImage(_imgShiftBriefingBook, book);

            var briefing = Scenario.Briefing;
            int expectedCalls = _calls.Count(c => !c.IsRestricted);

            using var ink = new SolidBrush(BookInk);
            using var muted = new SolidBrush(BookMuted);
            using var rule = new Pen(BookRule, Math.Max(1f, 1.5f * s));

            // ── Left page: who is on shift tonight ──
            using (var tag = FontManager.GetSmallCapsFont(9f * s))
                g.DrawString("MUNICIPAL EMERGENCY SERVICES · FORM 08", tag, muted, left.X, left.Y);
            using (var title = FontManager.GetRegularFont(34f * s))
                g.DrawString("Shift Briefing", title, ink, left.X, left.Y + 34 * s);

            var photo = new RectangleF(left.X, left.Y + 140 * s, 150 * s, 150 * s);
            using (var photoFill = new SolidBrush(Color.FromArgb(28, 64, 50))) g.FillRectangle(photoFill, photo);
            using (var photoFont = FontManager.GetSmallCapsFont(13f * s))
            using (var photoSmall = FontManager.GetSmallCapsFont(8f * s))
            using (var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var light = new SolidBrush(Color.FromArgb(200, 220, 205)))
            {
                g.DrawString("04", photoFont, light, new RectangleF(photo.X, photo.Y, photo.Width, photo.Height * 0.85f), center);
                g.DrawString("NO IMAGE", photoSmall, light, new RectangleF(photo.X, photo.Y + photo.Height * 0.3f, photo.Width, photo.Height * 0.85f), center);
            }

            float infoX = photo.Right + 22 * s;
            using (var tag = FontManager.GetSmallCapsFont(9f * s))
                g.DrawString("DISPATCHER 04", tag, muted, infoX, photo.Y + 6 * s);
            using (var name = FontManager.GetRegularFont(26f * s))
                g.DrawString(Profile.OperatorName, name, ink, new RectangleF(infoX, photo.Y + 30 * s, left.Right - infoX, 60 * s));
            using (var role = FontManager.GetItalicFont(11f * s))
                g.DrawString("Probationary operator", role, muted, infoX, photo.Y + 92 * s);

            float rowY = photo.Bottom + 60 * s;
            var rows = new[]
            {
                ("SHIFT", $"{briefing.ShiftLabel} / {briefing.Hours}"),
                ("UNITS", $"{Scenario.InitialUnits:D2} available"),
                ("EXPECTED", $"{expectedCalls:D2} calls")
            };
            using (var label = FontManager.GetSmallCapsFont(9f * s))
            using (var value = FontManager.GetRegularFont(17f * s))
            {
                foreach (var (key, val) in rows)
                {
                    g.DrawString(key, label, muted, left.X, rowY + 10 * s);
                    g.DrawString(val, value, ink, left.X + 150 * s, rowY);
                    g.DrawLine(rule, left.X, rowY + 46 * s, left.Right, rowY + 46 * s);
                    rowY += 66 * s;
                }
            }

            // ── Right page: tonight's procedure and a margin note ──
            using (var tag = FontManager.GetSmallCapsFont(9f * s))
                g.DrawString($"SUPERVISOR MEMO / {Scenario.SupervisorName}", tag, muted, right.X, right.Y);
            using (var title = FontManager.GetRegularFont(34f * s))
                g.DrawString("Procedure", title, ink, right.X, right.Y + 34 * s);

            float stepY = right.Y + 140 * s;
            using (var num = FontManager.GetSmallCapsFont(9f * s))
            using (var step = FontManager.GetSmallCapsFont(14f * s))
            {
                for (int i = 0; i < briefing.Procedure.Count; i++)
                {
                    g.DrawString((i + 1).ToString("D2"), num, muted, right.X, stepY + 6 * s);
                    var box = new RectangleF(right.X + 60 * s, stepY, right.Width - 60 * s, 90 * s);
                    g.DrawString(briefing.Procedure[i], step, ink, box);
                    float used = g.MeasureString(briefing.Procedure[i], step, (int)box.Width).Height;
                    stepY += Math.Max(56 * s, used + 22 * s);
                }
            }

            // Handwritten note in the margin, slightly tilted
            using (var note = FontManager.GetItalicFont(15f * s))
            using (var noteBrush = new SolidBrush(BookNote))
            {
                var noteBox = new RectangleF(right.X + 10 * s, stepY + 30 * s, right.Width - 20 * s, 170 * s);
                var state = g.Save();
                g.TranslateTransform(noteBox.X, noteBox.Y);
                g.RotateTransform(-3f);
                g.DrawString(briefing.Note, note, noteBrush, new RectangleF(0, 0, noteBox.Width, noteBox.Height));
                g.Restore(state);
            }
        }
    }
}
