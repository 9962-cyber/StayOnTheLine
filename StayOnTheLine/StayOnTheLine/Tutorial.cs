using FormsTimer = System.Windows.Forms.Timer;
using ThreadingTimer = System.Threading.Timer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace StayOnTheLine
{
    /// <summary>
    /// OOP Principles Demonstrated:
    /// 1. INHERITANCE: Inherits from BaseGameForm, standardizing horror game window behavior
    ///    (borderless, maximized, double-buffered, black background).
    /// 2. ENCAPSULATION: Encapsulates queue-based dialogue state machine, typewriter printing,
    ///    and speaker asset maps within private fields with controlled public/protected access.
    /// 3. POLYMORPHISM: Overrides OnResize, OnKeyDown, and custom graphics rendering to provide dynamic,
    ///    aspect-ratio agnostic Cover-Fit rendering (zero black lines) across all screen resolutions.
    /// 4. ABSTRACTION: Communicates completion through DialogResult, allowing
    ///    MainForm to coordinate the subsequent shift gameplay without tight coupling.
    /// </summary>
    public partial class Tutorial : BaseGameForm
    {
        private enum CharSide { Left, Center, Right }

        private class DialogueLine
        {
            public string Speaker { get; set; }
            public string Text { get; set; }
            public string? Expression { get; set; }
            public CharSide? Side { get; set; }
            public string? Background { get; set; }

            public DialogueLine(string speaker, string text, string? expression = null, CharSide? side = null, string? background = null)
            {
                Speaker = speaker;
                Text = text;
                Expression = expression;
                Side = side;
                Background = background;
            }
        }

        private Queue<DialogueLine> storyQueue = new Queue<DialogueLine>();
        private FormsTimer typingTimer = null!;
        private string currentFullText = "";
        private int charIndex = 0;
        private bool isTyping = false;

        // Backgrounds
        private Image? defaultBackground;                                   // used by everyone without their own
        private Image? originalBackground;                                  // the one currently shown (full size)
        private Dictionary<string, Image> speakerBackgrounds = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        // Character art organized by character -> expression -> Image
        private Dictionary<string, Dictionary<string, Image>> characterExpressions =
            new Dictionary<string, Dictionary<string, Image>>(StringComparer.OrdinalIgnoreCase);

        // Fallback default image per character
        private Dictionary<string, Image> characterImages =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, CharSide> characterSide =
            new Dictionary<string, CharSide>(StringComparer.OrdinalIgnoreCase)
        {
            { "KRISHA", CharSide.Center },
            { "CHARM",  CharSide.Right  },
            { "YSA",    CharSide.Left   },
            { "STEF",   CharSide.Left   },
            { "CHLOE",  CharSide.Center }
        };
        private CharSide currentSide = CharSide.Center;
        private Image? currentCharacterImage;
        private Rectangle characterBounds;
        private bool characterVisible = false;

        // Font used everywhere (falls back to Georgia if IM Fell isn't installed)
        private readonly string fontName;
        private readonly string _playerName;
        private readonly StayOnTheLine.Models.OperatorProfile? _profile;

        public Tutorial() : this(new StayOnTheLine.Models.OperatorProfile(1, "OPERATOR"))
        {
        }

        public Tutorial(string playerName) : this(new StayOnTheLine.Models.OperatorProfile(1, string.IsNullOrWhiteSpace(playerName) ? "OPERATOR" : playerName.Trim()))
        {
        }

        public Tutorial(StayOnTheLine.Models.OperatorProfile profile)
        {
            _profile = profile ?? new StayOnTheLine.Models.OperatorProfile(1, "OPERATOR");
            _playerName = _profile.OperatorName;
            InitializeComponent();

            fontName = FontExists("IM FELL DW Pica") ? "IM FELL DW Pica" : "Georgia";

            SetupGame();
            ApplyLayout();
            this.Resize += (s, e) => ApplyLayout();
            this.KeyDown += Tutorial_KeyDown;
        }

        // ------------------------------------------------------------------
        //  SETUP
        // ------------------------------------------------------------------
        private void SetupGame()
        {
            typingTimer = new FormsTimer();
            typingTimer.Interval = 25;
            typingTimer.Tick += TypingTimer_Tick;

            // Click anywhere (except the gear) to advance
            HookClick(this);

            // ---- ART ----
            defaultBackground = LoadAsset("tutorialdialogue_bg.png", () => MakePlaceholderBackground());
            originalBackground = defaultBackground;

            // Different background while CHARM is speaking
            var charmBg = LoadAsset("tutorialdialogue_bg.png",
                () => MakePlaceholderBackground("CHARM BACKGROUND PLACEHOLDER",
                                                Color.FromArgb(78, 52, 70), Color.FromArgb(16, 8, 14)));
            if (charmBg != null) speakerBackgrounds["CHARM"] = charmBg;

            // Load all character expression assets from Assets folder
            LoadCharacterAssets();

            picCharacter.Visible = false;
            picBackground.Paint += PicBackground_Paint;

            LoadStory();
            DisplayNextLine();
        }

        private void HookClick(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c != btnSettings)
                {
                    c.Click += Form_Click;
                    HookClick(c);
                }
            }
            if (parent == this) this.Click += Form_Click;
        }

        private static bool FontExists(string name)
        {
            using (InstalledFontCollection fonts = new InstalledFontCollection())
                return fonts.Families.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private Image? LoadAsset(string fileName, Func<Image>? placeholder = null)
        {
            var searchNames = new List<string> { fileName };
            if (!Path.HasExtension(fileName))
            {
                searchNames.Add(fileName + ".png");
                searchNames.Add(fileName + ".jpg");
                searchNames.Add(fileName + ".jpeg");
            }

            // Support slight spelling variations (dialogue vs dialouge)
            if (fileName.IndexOf("dialogue", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                searchNames.Add(fileName.Replace("dialogue", "dialouge"));
                searchNames.Add(fileName.Replace("Dialogue", "Dialouge"));
            }
            else if (fileName.IndexOf("dialouge", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                searchNames.Add(fileName.Replace("dialouge", "dialogue"));
                searchNames.Add(fileName.Replace("Dialouge", "Dialogue"));
            }

            var baseDirs = new[]
            {
                Application.StartupPath,
                AppDomain.CurrentDomain.BaseDirectory,
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
                        using (var fs = new FileStream(pathInAssets, FileMode.Open, FileAccess.Read))
                        using (var img = Image.FromStream(fs))
                            return new Bitmap(img);
                    }

                    string pathDirect = Path.Combine(baseDir, name);
                    if (File.Exists(pathDirect))
                    {
                        using (var fs = new FileStream(pathDirect, FileMode.Open, FileAccess.Read))
                        using (var img = Image.FromStream(fs))
                            return new Bitmap(img);
                    }
                }
            }

            return placeholder != null ? placeholder() : null;
        }

        private void LoadCharacterAssets()
        {
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

            // 1. Scan Assets folders for <character>_<expression>.png files
            foreach (string baseDir in baseDirs)
            {
                if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir)) continue;

                string assetsDir = Path.Combine(baseDir, "Assets");
                if (!Directory.Exists(assetsDir)) continue;

                foreach (string file in Directory.GetFiles(assetsDir, "*.png", SearchOption.AllDirectories))
                {
                    string fname = Path.GetFileNameWithoutExtension(file);

                    // Skip background files
                    if (fname.StartsWith("bg_", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("tutorial", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("test_", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("loading", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("main", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("computer", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("endshift", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("shiftbriefing", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    int underscore = fname.IndexOf('_');
                    if (underscore > 0 && underscore < fname.Length - 1)
                    {
                        string charName = fname.Substring(0, underscore).ToUpperInvariant();
                        string expression = fname.Substring(underscore + 1).ToLowerInvariant();
                        if (expression.EndsWith(" - copy", StringComparison.OrdinalIgnoreCase))
                        {
                            expression = expression.Replace(" - copy", "", StringComparison.OrdinalIgnoreCase).Trim();
                        }

                        if (!characterExpressions.TryGetValue(charName, out var expMap))
                        {
                            expMap = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
                            characterExpressions[charName] = expMap;
                        }

                        if (!expMap.ContainsKey(expression))
                        {
                            try
                            {
                                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                                using (var img = Image.FromStream(fs))
                                {
                                    expMap[expression] = new Bitmap(img);
                                }
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        string charName = fname.ToUpperInvariant();
                        if (!characterImages.ContainsKey(charName))
                        {
                            try
                            {
                                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                                using (var img = Image.FromStream(fs))
                                {
                                    characterImages[charName] = new Bitmap(img);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }

            // 2. Set default fallback image for each character
            string[] preferredDefaults = { "talking", "smile", "serious", "default", "neutral", "sad", "annoyed", "concerned" };
            foreach (var kvp in characterExpressions)
            {
                string charName = kvp.Key;
                var expMap = kvp.Value;

                if (!characterImages.ContainsKey(charName) && expMap.Count > 0)
                {
                    Image? chosen = null;
                    foreach (string pref in preferredDefaults)
                    {
                        if (expMap.TryGetValue(pref, out Image? img))
                        {
                            chosen = img;
                            break;
                        }
                    }
                    characterImages[charName] = chosen ?? expMap.Values.First();
                }
            }
        }

        private Image? GetCharacterImage(string speaker, string? expression)
        {
            if (string.IsNullOrEmpty(speaker)) return null;

            // Narration or player never has a character portrait
            if (string.Equals(speaker, "NARRATION", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(speaker, "Y/N", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(speaker, _playerName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (characterExpressions.TryGetValue(speaker, out var expMap))
            {
                if (!string.IsNullOrEmpty(expression))
                {
                    if (expMap.TryGetValue(expression, out Image? img))
                        return img;

                    if (string.Equals(expression, "annoyed", StringComparison.OrdinalIgnoreCase) && expMap.TryGetValue("concerned", out Image? cImg))
                        return cImg;

                    if (string.Equals(expression, "concerned", StringComparison.OrdinalIgnoreCase) && expMap.TryGetValue("annoyed", out Image? aImg))
                        return aImg;

                    if (string.Equals(expression, "giggle", StringComparison.OrdinalIgnoreCase) && expMap.TryGetValue("smile", out Image? sImg))
                        return sImg;

                    if (string.Equals(expression, "crashout", StringComparison.OrdinalIgnoreCase) && expMap.TryGetValue("serious", out Image? srImg))
                        return srImg;
                }

                string[] fallbacks = { "talking", "smile", "serious", "default", "concerned", "neutral", "sad", "annoyed" };
                foreach (string fb in fallbacks)
                {
                    if (expMap.TryGetValue(fb, out Image? fbImg))
                        return fbImg;
                }

                if (expMap.Count > 0)
                    return expMap.Values.First();
            }

            if (characterImages.TryGetValue(speaker, out Image? defaultImg))
            {
                return defaultImg;
            }

            return null;
        }

        // ------------------------------------------------------------------
        //  BACKGROUND (Dynamic aspect-ratio agnostic cover fit, zero black lines)
        // ------------------------------------------------------------------
        private void UpdateBackground()
        {
            // Do not store a static bitmap on PictureBox to prevent aspect distortion and black letterboxing.
            // Rendering is handled dynamically on Paint with high-quality bicubic interpolation.
            if (picBackground.Image != null)
            {
                var old = picBackground.Image;
                picBackground.Image = null;
                old.Dispose();
            }
            picBackground.Invalidate();
        }

        // ------------------------------------------------------------------
        //  LAYOUT  (everything scales from a 1920x1080 design)
        // ------------------------------------------------------------------
        private void ApplyLayout()
        {
            int W = this.ClientSize.Width;
            int H = this.ClientSize.Height;
            if (W <= 0 || H <= 0) return;
            float s = H / 1080f;   // scale factor

            UpdateBackground();

            // Dialogue box: wide, near the bottom, with side margins
            int marginX = (int)(W * 0.055f);
            int boxH = (int)(H * 0.20f);
            int boxBottomGap = (int)(H * 0.055f);
            pnlDialogue.SetBounds(marginX, H - boxH - boxBottomGap, W - marginX * 2, boxH);

            // Fonts
            SetFont(lblSpeaker, 15f * s);
            SetFont(lblIncoming, 11f * s);
            SetFont(lblDialogue, 22f * s);
            SetFont(lblArrow, 14f * s);

            // Inside the box
            int pad = (int)(40 * s);
            lblSpeaker.Location = new Point(pad, (int)(22 * s));

            int tagW = (int)(420 * s);
            lblIncoming.SetBounds(pnlDialogue.Width - tagW - pad, (int)(24 * s), tagW, (int)(30 * s));

            int textTop = (int)(70 * s);
            lblDialogue.SetBounds(pad, textTop,
                pnlDialogue.Width - pad * 2 - (int)(30 * s),
                pnlDialogue.Height - textTop - (int)(20 * s));

            lblArrow.Location = new Point(pnlDialogue.Width - (int)(60 * s), pnlDialogue.Height - (int)(55 * s));

            // Gear (top-right)
            int gear = (int)(56 * s);
            btnSettings.SetBounds(W - gear - (int)(24 * s), (int)(24 * s), gear, gear);
            btnSettings.Invalidate();

            // Character
            PlaceCharacter(W, H);

            picBackground.Invalidate(true);
        }

        private void PlaceCharacter(int W, int H)
        {
            // Large bottom-anchored bounding box – sprite rendered via PicBackground_Paint.
            // Height = 92 % of screen, width derived from height so art is never distorted.
            int cH = (int)(H * 0.92f);
            int cW = (int)(H * 0.70f);
            int top = H - cH; // pin to bottom of screen
            int left;
            switch (currentSide)
            {
                case CharSide.Left:   left = (int)(W * 0.04f); break;
                case CharSide.Right:  left = W - cW - (int)(W * 0.03f); break;
                default:              left = (W - cW) / 2; break; // center
            }
            characterBounds = new Rectangle(left, top, cW, cH);
            picCharacter.Visible = false; // always hidden; drawing done in PicBackground_Paint
            picBackground.Invalidate(true);
        }

        private void PicBackground_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = picBackground.Width;
            int h = picBackground.Height;
            if (w <= 0 || h <= 0) return;

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 1. Dynamic Cover-Fit Background (Zero black lines, dynamically fills 100% of viewport)
            if (originalBackground != null)
            {
                float scale = Math.Max((float)w / originalBackground.Width, (float)h / originalBackground.Height);
                float drawW = originalBackground.Width * scale;
                float drawH = originalBackground.Height * scale;
                float drawX = (w - drawW) / 2f;
                float drawY = (h - drawH) / 2f;

                g.DrawImage(originalBackground, drawX, drawY, drawW, drawH);

                // Subtle CRT edge vignette to blend cinematic immersion
                using var vPath = new GraphicsPath();
                vPath.AddRectangle(new Rectangle(0, 0, w, h));
                using var pgb = new PathGradientBrush(vPath)
                {
                    CenterColor = Color.FromArgb(0, 0, 0, 0),
                    SurroundColors = new[] { Color.FromArgb(90, 0, 0, 0) },
                    FocusScales = new PointF(0.75f, 0.70f)
                };
                g.FillRectangle(pgb, 0, 0, w, h);
            }

            // 2. Character Sprite (Proportionally scaled, pinned to bottom of screen)
            if (characterVisible && currentCharacterImage != null && characterBounds.Width > 0 && characterBounds.Height > 0)
            {
                float imgAspect = (float)currentCharacterImage.Width / currentCharacterImage.Height;
                float boxAspect = (float)characterBounds.Width / characterBounds.Height;
                int drawW = characterBounds.Width;
                int drawH = characterBounds.Height;
                int drawX = characterBounds.X;
                int drawY = characterBounds.Y;

                if (imgAspect > boxAspect)
                {
                    drawH = (int)(characterBounds.Width / imgAspect);
                    drawY = characterBounds.Bottom - drawH;
                }
                else
                {
                    drawW = (int)(characterBounds.Height * imgAspect);
                    drawX = characterBounds.X + (characterBounds.Width - drawW) / 2;
                }

                g.DrawImage(currentCharacterImage, drawX, drawY, drawW, drawH);
            }
        }

        private void SetFont(Control c, float size)
        {
            Font old = c.Font;
            c.Font = new Font(fontName, size, FontStyle.Regular, GraphicsUnit.Point);
            if (old != null && old != Control.DefaultFont) old.Dispose();
        }

        // ------------------------------------------------------------------
        //  CUSTOM DRAWING: GEAR ICON
        // ------------------------------------------------------------------
        private void btnSettings_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = btnSettings.Width / 2f;
            float cy = btnSettings.Height / 2f;
            float u = btnSettings.Width / 50f;       // unit scale
            float outer = 16f * u, inner = 12f * u, hole = 5f * u;
            int teeth = 8;
            double step = 2 * Math.PI / teeth;

            var pts = new List<PointF>();
            for (int i = 0; i < teeth; i++)
            {
                double a = i * step;
                double[] ang = { a - 0.22 * step, a - 0.12 * step, a + 0.12 * step, a + 0.22 * step };
                float[] rad = { inner, outer, outer, inner };
                for (int k = 0; k < 4; k++)
                    pts.Add(new PointF(cx + rad[k] * (float)Math.Cos(ang[k]),
                                       cy + rad[k] * (float)Math.Sin(ang[k])));
            }

            using (GraphicsPath path = new GraphicsPath(FillMode.Alternate))
            {
                path.AddPolygon(pts.ToArray());
                path.AddEllipse(cx - hole, cy - hole, hole * 2, hole * 2); // real hole (alternate fill)
                g.FillPath(Brushes.White, path);
            }
        }

        private void LoadStory()
        {
            storyQueue.Enqueue(new DialogueLine(_playerName, "Your application was approved three days ago."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "Tonight is your first overnight shift at the emergency dispatch center."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "The building looks older than it did during your interview. Fluorescent lights buzz overhead. A wall clock ticks above a row of desks, and the CRT monitors cast a faint phosphor glow across the room."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "A printer rattles somewhere behind you. On the wall is a faded slogan: EVERY CALL MATTERS, EVERY SECOND COUNTS."));

            // 1. Shift Trainer Ysa conducts shift briefing
            storyQueue.Enqueue(new DialogueLine("YSA", "Attention, new dispatcher. I am Ysa, your shift trainer.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("YSA", "Tonight begins your provisional training protocol. The calls you receive tonight are genuine emergency trunk lines.", "talking", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("YSA", "Follow procedure strictly: verify the reported address, inspect the station incident logs, and never dispatch our limited patrol units without reasonable justification.", "serious", CharSide.Left));

            // 2. Neighboring console Charm chimes in
            storyQueue.Enqueue(new DialogueLine("CHARM", "Translation: don't panic, don't guess, and don't make Ysa's paperwork worse than it already is.", "giggle", CharSide.Right));

            // 3. Trainer Ysa disciplines Charm
            storyQueue.Enqueue(new DialogueLine("YSA", "Villareal. Headset on. Back to your line.", "annoyed", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARM", "Yes, ma'am! Just offering official morale support to the trainee.", "smile", CharSide.Right));

            // 4. Ysa exits to supervisory office; Krisha Narciso steps over as Shift Supervisor
            storyQueue.Enqueue(new DialogueLine("NARRATION", "[Trainer Ysa heads into the administrative office. Night Desk Supervisor Krisha Narciso steps over to stand beside your workstation.]"));

            // 5. Krisha Narciso - Shift Supervisor introduction & banter
            storyQueue.Enqueue(new DialogueLine("KRISHA", "First night? Don't let Ysa's strict tone intimidate you. The switchboards look overwhelming, but most callers just need someone calm to listen.", "smile", CharSide.Center));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "I'm Krisha Narciso, your shift supervisor. I'll be monitoring your console feed tonight and evaluating your performance.", "talking", CharSide.Center));

            storyQueue.Enqueue(new DialogueLine("CHARM", "And I'm Charm Villareal! Your neighboring console, unofficial coffee supplier, and resident emergency backup.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Unofficial coffee supplier?", "concerned", CharSide.Center));
            storyQueue.Enqueue(new DialogueLine("CHARM", "Hey, caffeine keeps this city safe.", "giggle", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Just make sure you don't spill it on the trunk terminals.", "smile", CharSide.Center));

            // 6. Krisha's 3 Core Rules
            storyQueue.Enqueue(new DialogueLine("KRISHA", "All right. Jokes aside, listen carefully. Rule number one: verify the address before sending a unit, whenever it's safe to do so.", "serious", CharSide.Center));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Rule number two: check the incident log if something doesn't look right. Discrepancies matter.", "serious", CharSide.Center));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Rule number three: don't panic just because the caller does. A frightened person may struggle to explain what's happening.", "talking", CharSide.Center));

            storyQueue.Enqueue(new DialogueLine("CHARM", "And rule number four: don't let Ysa catch you sleeping.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "There is no rule number four.", "concerned", CharSide.Center));
            storyQueue.Enqueue(new DialogueLine("CHARM", "There is if you value your employment.", "serious", CharSide.Right));

            // 7. Telephone rings -> transition to computer terminal
            storyQueue.Enqueue(new DialogueLine("KRISHA", "[A telephone rings across the room on Trunk Line 01. Charm immediately turns back to her console.] That's our first incoming emergency line. Head to your terminal and answer it.", "serious", CharSide.Center));
        }

        private void DisplayNextLine()
        {
            if (storyQueue.Count == 0)
            {
                // Completed tutorial dialogue, transition cleanly to shift gameplay
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            DialogueLine line = storyQueue.Dequeue();

            // Pick the background for this speaker or line (default for everyone else)
            Image? wanted = defaultBackground;
            if (!string.IsNullOrEmpty(line.Background) && speakerBackgrounds.TryGetValue(line.Background, out Image? lineBg))
            {
                wanted = lineBg;
            }
            else if (speakerBackgrounds.TryGetValue(line.Speaker, out Image? spkBg))
            {
                wanted = spkBg;
            }

            if (wanted != null && wanted != originalBackground)
            {
                originalBackground = wanted;
                UpdateBackground();
            }

            if (string.Equals(line.Speaker, "NARRATION", StringComparison.OrdinalIgnoreCase))
            {
                lblSpeaker.Text = "DISPATCH BRIEFING";
                lblIncoming.Text = "PROTOCOL LOG";
            }
            else if (string.Equals(line.Speaker, "Y/N", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(line.Speaker, _playerName, StringComparison.OrdinalIgnoreCase))
            {
                lblSpeaker.Text = _playerName;
                lblIncoming.Text = "TRAINEE DISPATCHER";
            }
            else if (string.Equals(line.Speaker, "KRISHA", StringComparison.OrdinalIgnoreCase))
            {
                lblSpeaker.Text = "KRISHA NARCISO";
                lblIncoming.Text = "NIGHT DESK SUPERVISOR";
            }
            else if (string.Equals(line.Speaker, "YSA", StringComparison.OrdinalIgnoreCase))
            {
                lblSpeaker.Text = "YSA";
                lblIncoming.Text = "SHIFT TRAINER";
            }
            else if (string.Equals(line.Speaker, "CHARM", StringComparison.OrdinalIgnoreCase))
            {
                lblSpeaker.Text = "CHARM VILLAREAL";
                lblIncoming.Text = "OPERATOR / CONSOLE 03";
            }
            else
            {
                lblSpeaker.Text = line.Speaker;
                lblIncoming.Text = "INCOMING / VOICE";
            }

            // Show the speaking character with their expression.
            // The sprite is drawn entirely by PicBackground_Paint using characterBounds –
            // picCharacter is kept hidden because its Bounds are never set and would appear
            // at (0,0) with a tiny default size (the bug visible in the upper-left corner).
            Image? charImg = GetCharacterImage(line.Speaker, line.Expression);
            if (charImg != null)
            {
                currentCharacterImage = charImg;
                if (line.Side.HasValue)
                {
                    currentSide = line.Side.Value;
                }
                else if (characterSide.TryGetValue(line.Speaker, out CharSide side))
                {
                    currentSide = side;
                }
                else
                {
                    currentSide = CharSide.Center;
                }

                characterVisible = true;
                picCharacter.Visible = false; // never shown directly; drawn via PicBackground_Paint
                PlaceCharacter(ClientSize.Width, ClientSize.Height);
                pnlDialogue.BringToFront();
                btnSettings.BringToFront();
                pnlDialogue.Invalidate();
            }
            else
            {
                currentCharacterImage = null;
                characterVisible = false;
                picCharacter.Visible = false;
                picBackground.Invalidate(true);
            }

            currentFullText = line.Text;
            charIndex = 0;
            lblDialogue.Text = "";
            lblArrow.Visible = false;
            isTyping = true;
            typingTimer.Start();
        }

        private void TypingTimer_Tick(object? sender, EventArgs e)
        {
            if (charIndex < currentFullText.Length)
            {
                charIndex++;
                lblDialogue.Text = currentFullText.Substring(0, charIndex);
            }
            else
            {
                typingTimer.Stop();
                isTyping = false;
                lblArrow.Visible = true;
            }
        }

        // ------------------------------------------------------------------
        //  INPUT
        // ------------------------------------------------------------------
        private void Form_Click(object? sender, EventArgs e)
        {
            Advance();
        }

        private void Advance()
        {
            if (isTyping)
            {
                typingTimer.Stop();
                lblDialogue.Text = currentFullText;
                isTyping = false;
                lblArrow.Visible = true;
            }
            else
            {
                DisplayNextLine();
            }
        }

        private void Tutorial_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Advance();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                btnSettings_Click(sender, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void btnSettings_Click(object? sender, EventArgs e)
        {
            using var sf = new Forms.SettingsForm();
            sf.ShowExitButton = true;
            sf.CurrentProfile = _profile;
            var result = sf.ShowDialog(this);
            if (result == DialogResult.Abort)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        // ------------------------------------------------------------------
        //  PLACEHOLDER ART  (replaced automatically when real files exist)
        // ------------------------------------------------------------------
        private static Image MakePlaceholderBackground(string label = "BACKGROUND PLACEHOLDER",
                                                       Color? top = null, Color? bottom = null)
        {
            Color c1 = top ?? Color.FromArgb(48, 66, 56);
            Color c2 = bottom ?? Color.FromArgb(8, 12, 10);

            Bitmap bmp = new Bitmap(1920, 1080);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, 1920, 1080), c1, c2, 90f))
                    g.FillRectangle(br, 0, 0, 1920, 1080);

                // A few rectangles so it reads like a dim room
                using (var b = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                {
                    g.FillRectangle(b, 220, 260, 420, 260);
                    g.FillRectangle(b, 760, 220, 300, 300);
                    g.FillRectangle(b, 1200, 280, 380, 240);
                }
                using (var f = new Font("Segoe UI", 28f))
                using (var b = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
                    g.DrawString(label, f, b, 40, 30);
            }
            return bmp;
        }

        private static Image MakePlaceholderCharacter(string name, Color body)
        {
            Bitmap bmp = new Bitmap(700, 1000);   // transparent canvas
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(body))
                using (var pen = new Pen(Color.FromArgb(160, 255, 255, 255), 3f))
                {
                    g.FillEllipse(br, 100, 400, 500, 900);     // shoulders / body
                    g.DrawEllipse(pen, 100, 400, 500, 900);
                    g.FillEllipse(br, 225, 90, 250, 330);      // head
                    g.DrawEllipse(pen, 225, 90, 250, 330);
                }
                using (var f = new Font("Segoe UI", 26f, FontStyle.Bold))
                using (var b = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString(name + "\nPLACEHOLDER", f, b, new RectangleF(0, 600, 700, 120), sf);
                }
            }
            return bmp;
        }
    }
}