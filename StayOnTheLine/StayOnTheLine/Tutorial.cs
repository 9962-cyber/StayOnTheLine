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
            { "KRISHA",  CharSide.Left   },
            { "CHARM",   CharSide.Right  },
            { "CHARMEE", CharSide.Right  },
            { "STEF",    CharSide.Left   },
            { "CHLOE",   CharSide.Center },
            { "YSA",     CharSide.Left   }
        };
        private CharSide currentSide = CharSide.Center;
        private Image? currentCharacterImage;
        private Rectangle characterBounds;
        private bool characterVisible = false;

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
            _playerName = string.IsNullOrWhiteSpace(_profile.OperatorName) ? "OPERATOR" : _profile.OperatorName.Trim();
            InitializeComponent();


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
            // Shift 2's intro happens on the dispatch floor itself; the first night uses the tutorial room
            string sceneBg = _profile?.CurrentShift == 2 ? "maingameplay_bg.png" : "tutorialdialogue_bg.png";
            defaultBackground = LoadAsset(sceneBg, () => MakePlaceholderBackground());
            originalBackground = defaultBackground;

            // Different background while CHARM is speaking
            var charmBg = LoadAsset(sceneBg,
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
                string.Equals(speaker, "PLAYER", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(speaker, _playerName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string lookupSpeaker = speaker;
            if (string.Equals(speaker, "CHARMEE", StringComparison.OrdinalIgnoreCase))
            {
                lookupSpeaker = "CHARM";
            }

            if (characterExpressions.TryGetValue(lookupSpeaker, out var expMap))
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

        // Helper to format line text dynamically with the input Player Name
        private string FormatDialogueText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return "";
            return rawText.Replace("{PLAYER}", _playerName)
                          .Replace("[PLAYER]", _playerName)
                          .Replace("the player's", $"{_playerName}'s")
                          .Replace("the player", _playerName)
                          .Replace("The player", _playerName);
        }

        // ------------------------------------------------------------------
        //  BACKGROUND (Dynamic aspect-ratio agnostic cover fit, zero black lines)
        // ------------------------------------------------------------------
        private void UpdateBackground()
        {
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
            float baseDialogue = (!string.IsNullOrEmpty(currentFullText) && currentFullText.Length > 120) ? 17.5f : 22f;
            SetFont(lblDialogue, baseDialogue * s);
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
            int cH = (int)(H * 0.92f);
            int cW = (int)(H * 0.70f);
            int top = H - cH; // pin to bottom of screen
            int left;
            switch (currentSide)
            {
                case CharSide.Left: left = (int)(W * 0.04f); break;
                case CharSide.Right: left = W - cW - (int)(W * 0.03f); break;
                default: left = (W - cW) / 2; break; // center
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

            // 1. Dynamic Cover-Fit Background
            if (originalBackground != null)
            {
                float scale = Math.Max((float)w / originalBackground.Width, (float)h / originalBackground.Height);
                float drawW = originalBackground.Width * scale;
                float drawH = originalBackground.Height * scale;
                float drawX = (w - drawW) / 2f;
                float drawY = (h - drawH) / 2f;

                g.DrawImage(originalBackground, drawX, drawY, drawW, drawH);

                // Subtle CRT edge vignette
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

            // 2. Character Sprite
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
            c.Font = StayOnTheLine.Systems.FontManager.GetDialogueFont(size); // IM FELL DW Pica SC
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
                path.AddEllipse(cx - hole, cy - hole, hole * 2, hole * 2);
                g.FillPath(Brushes.White, path);
            }
        }

        private void LoadStory()
        {
            if (_profile?.CurrentShift == 2)
            {
                LoadStoryShift2();
            }
            else
            {
                LoadStoryShift1();
            }
        }

        private void LoadStoryShift1()
        {
            storyQueue.Enqueue(new DialogueLine("NARRATION", "Tonight is your first overnight shift at the emergency dispatch center."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "The building looks older than it did during your interview. Fluorescent lights buzz overhead. A wall clock ticks above a row of desks, and the CRT monitors cast a faint phosphor glow across the room."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "A printer rattles somewhere behind you. On the wall is a faded slogan: EVERY CALL MATTERS, EVERY SECOND COUNTS."));

            // Krisha approaches workstation
            storyQueue.Enqueue(new DialogueLine("NARRATION", "[A woman approaches your desk carrying a folder.]"));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "First night?", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Don't worry. The phones look intimidating, but most people just need someone to listen.", "smile", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "I'm Krisha Narciso. I'll be training you tonight.", "talking", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "I've worked here long enough to know two things. One, the coffee is terrible. Two, never trust a quiet night.", "talking", CharSide.Left));

            // Charmee from neighboring console
            storyQueue.Enqueue(new DialogueLine("NARRATION", "[A cheerful voice comes from the neighboring console.]"));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Hey! Don't scare the newbie before they even get their headset on.", "giggle", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("NARRATION", $"[Charmee turns her chair toward {_playerName} and smiles.]"));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "I'm Charmee Cantones. Your neighboring console, unofficial coffee supplier, and the person you should ask if you need anything.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Anything?", "concerned", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Almost anything.", "giggle", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "That's what worries me.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "See? She's already telling you stories about me.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "I'm telling them to keep an eye on you.", "concerned", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Same thing!", "giggle", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "[Charmee laughs and turns back to her computer.]"));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "She's been here for years. She knows everyone, remembers everyone's birthday, and somehow always knows when someone needs coffee.", "talking", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "She's also very good at making a serious situation sound less serious than it is.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "I heard that!", "crashout", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "You were supposed to.", "smile", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("NARRATION", $"[Krisha hands {_playerName} a training folder.]"));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "All right. Jokes aside, listen carefully.", "serious", CharSide.Left));

            // Three Basic Rules
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Rule number one: verify the address before sending a unit, whenever it's safe to do so.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Rule number two: check the incident log if something doesn't look right.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "An incident log is the record of a call: when it came in, what was reported, where it happened, and what actions were taken. If the information doesn't match, don't ignore it. Check the record and make a note of the discrepancy.", "talking", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Rule number three: don't panic just because the caller does.", "talking", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "A frightened person may struggle to explain what's happening. Stay calm, ask clear questions, and take immediate danger seriously.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "And rule number four: don't let Krisha catch you sleeping.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "There is no rule number four.", "concerned", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "There is if you value your employment.", "serious", CharSide.Right));

            // Telephone rings -> Shift starts
            storyQueue.Enqueue(new DialogueLine("NARRATION", "[A telephone rings across the room. Charmee immediately turns back to her console.]"));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "That's your first lesson. Let's see how you do.", "serious", CharSide.Left));
        }

        private void LoadStoryShift2()
        {
            storyQueue.Enqueue(new DialogueLine("NARRATION", "[10:50 PM — Dispatch Center]"));
            storyQueue.Enqueue(new DialogueLine("NARRATION", $"[{_playerName} arrives. Krisha's desk is empty. Her headset rests beside the keyboard.]"));
            storyQueue.Enqueue(new DialogueLine("YSA", "Before we begin, there's been a staffing change.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("YSA", "Krisha Narciso has been transferred to another department. You will be working without her from now on.", "talking", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("NARRATION", $"[{_playerName} looks toward Krisha's empty console.]"));
            storyQueue.Enqueue(new DialogueLine("NARRATION", $"[{_playerName} checks Krisha's console.]"));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "SYSTEM: OPERATOR NARCISO — SESSION ACTIVE."));
            storyQueue.Enqueue(new DialogueLine("YSA", "Do not use another operator's account. If you encounter an access issue, report it to me.", "annoyed", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Wait, transferred? Since when?", "serious", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("YSA", "The reassignment was approved by management.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Oh.", "serious", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "[Charm looks at Krisha's empty chair. For once, she doesn't immediately make a joke.]"));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "She trained me when I first started here. She used to tell me that the night shift was like a family.", "talking", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "A very tired, underpaid family that argued over the last cup of coffee, but still a family.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("YSA", "Cantones, your calls.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Right. Sorry.", "serious", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("NARRATION", $"[Charm turns back to her console, then glances at {_playerName}'s screen.]"));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "If you need help tonight, I'm right here, okay?", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("YSA", "The department has procedures for assistance. Follow them.", "annoyed", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "[Ysa walks away.]"));
            storyQueue.Enqueue(new DialogueLine("NARRATION", $"[{_playerName} looks at Krisha's console again.]"));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "SYSTEM: OPERATOR NARCISO — SESSION ACTIVE."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "CLUE 2: Krisha's account remains active after her reported transfer."));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Hey. Don't stare at that all night. It'll only make you feel worse.", "serious", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Come on. Let's get through tonight. I'll even share my coffee.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("NARRATION", $"[She slides a cup toward {_playerName}.]"));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Don't worry. I made a fresh one.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "[The coffee is already cold.]"));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "...Mostly fresh.", "giggle", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "Shift Briefing"));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "SYSTEM: SHIFT 2 STARTED."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "SYSTEM: AVAILABLE UNITS: 3."));
            storyQueue.Enqueue(new DialogueLine("YSA", "Tonight's priority is the incoming queue. We have reports of delayed responses in the surrounding districts.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("YSA", "Verify addresses, document discrepancies, and don't leave urgent calls unattended.", "talking", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARMEE", "Three units, three calls to start with. Let's hope the phones behave.", "smile", CharSide.Right));
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

            bool isNarration = string.Equals(line.Speaker, "NARRATION", StringComparison.OrdinalIgnoreCase);

            // The phone rings when the script says so; it keeps ringing into the first call of the shift
            if (isNarration && line.Text.Contains("telephone rings", StringComparison.OrdinalIgnoreCase))
            {
                StayOnTheLine.Systems.AudioManager.Instance.StartPhoneRing();
            }

            if (isNarration)
            {
                lblSpeaker.Text = "";
                lblIncoming.Text = "";
                lblSpeaker.Visible = false;
                lblIncoming.Visible = false;
            }
            else
            {
                lblSpeaker.Visible = true;
                lblIncoming.Visible = true;

                if (string.Equals(line.Speaker, "Y/N", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(line.Speaker, "PLAYER", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(line.Speaker, _playerName, StringComparison.OrdinalIgnoreCase))
                {
                    lblSpeaker.Text = _playerName.ToUpperInvariant();
                    lblIncoming.Text = "TRAINEE DISPATCHER";
                }
                else if (string.Equals(line.Speaker, "KRISHA", StringComparison.OrdinalIgnoreCase))
                {
                    lblSpeaker.Text = "KRISHA NARCISO";
                    lblIncoming.Text = "SHIFT SUPERVISOR";
                }
                else if (string.Equals(line.Speaker, "YSA", StringComparison.OrdinalIgnoreCase))
                {
                    lblSpeaker.Text = "YSA";
                    lblIncoming.Text = "SHIFT SUPERVISOR";
                }
                else if (string.Equals(line.Speaker, "CHARM", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(line.Speaker, "CHARMEE", StringComparison.OrdinalIgnoreCase))
                {
                    lblSpeaker.Text = "CHARMEE CANTONES";
                    lblIncoming.Text = "OPERATOR / CONSOLE 03";
                }
                else
                {
                    lblSpeaker.Text = line.Speaker.ToUpperInvariant();
                    lblIncoming.Text = "INCOMING / VOICE";
                }
            }

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
                picCharacter.Visible = false;
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

            currentFullText = FormatDialogueText(line.Text);
            charIndex = 0;
            lblDialogue.Text = "";
            float s = this.ClientSize.Height / 1080f;
            float baseSize = currentFullText.Length > 120 ? 17.5f : 22f;
            SetFont(lblDialogue, baseSize * s);

            int pad = (int)(40 * s);
            int textTop = isNarration ? (int)(28 * s) : (int)(70 * s);
            lblDialogue.SetBounds(pad, textTop,
                pnlDialogue.Width - pad * 2 - (int)(30 * s),
                pnlDialogue.Height - textTop - (int)(20 * s));

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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Left the scene early: don't carry the phone ring back to the menu
            if (DialogResult != DialogResult.OK)
                StayOnTheLine.Systems.AudioManager.Instance.StopPhoneRing();
            base.OnFormClosed(e);
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
            Bitmap bmp = new Bitmap(700, 1000);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(body))
                using (var pen = new Pen(Color.FromArgb(160, 255, 255, 255), 3f))
                {
                    g.FillEllipse(br, 100, 400, 500, 900);
                    g.DrawEllipse(pen, 100, 400, 500, 900);
                    g.FillEllipse(br, 225, 90, 250, 330);
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