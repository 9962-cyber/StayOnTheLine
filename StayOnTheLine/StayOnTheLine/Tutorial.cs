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
    public partial class Tutorial : Form
    {
        private enum CharSide { Left, Center, Right }

        private class DialogueLine
        {
            public string Speaker { get; set; }
            public string Text { get; set; }
            public string Expression { get; set; }
            public CharSide? Side { get; set; }
            public string Background { get; set; }

            public DialogueLine(string speaker, string text, string expression = null, CharSide? side = null, string background = null)
            {
                Speaker = speaker;
                Text = text;
                Expression = expression;
                Side = side;
                Background = background;
            }
        }

        private Queue<DialogueLine> storyQueue = new Queue<DialogueLine>();
        private FormsTimer typingTimer;
        private string currentFullText = "";
        private int charIndex = 0;
        private bool isTyping = false;

        // Backgrounds
        private Image defaultBackground;                                   // used by everyone without their own
        private Image originalBackground;                                  // the one currently shown (full size)
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
        private Image currentCharacterImage;
        private Rectangle characterBounds;
        private bool characterVisible = false;

        // Font used everywhere (falls back to Georgia if IM Fell isn't installed)
        private readonly string fontName;

        public Tutorial()
        {
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
            speakerBackgrounds["CHARM"] = LoadAsset("tutorialdialogue_bg.png",
                () => MakePlaceholderBackground("CHARM BACKGROUND PLACEHOLDER",
                                                Color.FromArgb(78, 52, 70), Color.FromArgb(16, 8, 14)));

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

        private Image LoadAsset(string fileName, Func<Image> placeholder)
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

                foreach (string file in Directory.GetFiles(assetsDir, "*.png"))
                {
                    string fname = Path.GetFileNameWithoutExtension(file);

                    // Skip background files
                    if (fname.StartsWith("bg_", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("tutorial", StringComparison.OrdinalIgnoreCase) ||
                        fname.StartsWith("test_", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    int underscore = fname.IndexOf('_');
                    if (underscore > 0 && underscore < fname.Length - 1)
                    {
                        string charName = fname.Substring(0, underscore).ToUpperInvariant();
                        string expression = fname.Substring(underscore + 1).ToLowerInvariant();

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
            string[] preferredDefaults = { "default", "talking", "smile", "serious", "neutral", "sad" };
            foreach (var kvp in characterExpressions)
            {
                string charName = kvp.Key;
                var expMap = kvp.Value;

                if (!characterImages.ContainsKey(charName) && expMap.Count > 0)
                {
                    Image chosen = null;
                    foreach (string pref in preferredDefaults)
                    {
                        if (expMap.TryGetValue(pref, out Image img))
                        {
                            chosen = img;
                            break;
                        }
                    }
                    characterImages[charName] = chosen ?? expMap.Values.First();
                }
            }

            // 3. Fallback placeholder silhouettes only if no art is available
            string[] knownChars = { "YSA", "CHARM", "KRISHA", "STEF", "CHLOE" };
            Color[] colors = {
                Color.FromArgb(70, 70, 80),
                Color.FromArgb(50, 70, 60),
                Color.FromArgb(60, 50, 80),
                Color.FromArgb(80, 60, 50),
                Color.FromArgb(50, 60, 80)
            };

            for (int i = 0; i < knownChars.Length; i++)
            {
                string c = knownChars[i];
                if (!characterImages.ContainsKey(c))
                {
                    Image placeholder = MakePlaceholderCharacter(c, colors[i]);
                    characterImages[c] = placeholder;
                    if (!characterExpressions.TryGetValue(c, out var map))
                    {
                        map = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
                        characterExpressions[c] = map;
                    }
                    map["default"] = placeholder;
                }
            }
        }

        private Image GetCharacterImage(string speaker, string expression)
        {
            if (string.IsNullOrEmpty(speaker)) return null;

            if (characterExpressions.TryGetValue(speaker, out var expMap))
            {
                if (!string.IsNullOrEmpty(expression))
                {
                    if (expMap.TryGetValue(expression, out Image img))
                        return img;

                    if (string.Equals(expression, "annoyed", StringComparison.OrdinalIgnoreCase) && expMap.TryGetValue("concerned", out Image cImg))
                        return cImg;

                    if (string.Equals(expression, "concerned", StringComparison.OrdinalIgnoreCase) && expMap.TryGetValue("annoyed", out Image aImg))
                        return aImg;
                }

                string[] fallbacks = { "default", "talking", "smile", "serious", "concerned", "neutral", "sad" };
                foreach (string fb in fallbacks)
                {
                    if (expMap.TryGetValue(fb, out Image fbImg))
                        return fbImg;
                }

                if (expMap.Count > 0)
                    return expMap.Values.First();
            }

            if (characterImages.TryGetValue(speaker, out Image defaultImg))
            {
                return defaultImg;
            }

            return null;
        }

        // ------------------------------------------------------------------
        //  BACKGROUND (fills the whole screen, crops the overflow)
        // ------------------------------------------------------------------
        private void UpdateBackground()
        {
            int W = ClientSize.Width, H = ClientSize.Height;
            if (W <= 0 || H <= 0 || originalBackground == null) return;

            picBackground.SizeMode = PictureBoxSizeMode.Normal;

            Image old = picBackground.Image;
            picBackground.Image = CoverFit(originalBackground, W, H);
            if (old != null) old.Dispose();
        }

        // Scales the image to fill W x H completely, cropping whatever overflows
        private static Image CoverFit(Image src, int W, int H)
        {
            float scale = Math.Max((float)W / src.Width, (float)H / src.Height);
            float drawW = src.Width * scale;
            float drawH = src.Height * scale;

            Bitmap result = new Bitmap(W, H);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, (W - drawW) / 2f, (H - drawH) / 2f, drawW, drawH);
            }
            return result;
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

        private void PicBackground_Paint(object sender, PaintEventArgs e)
        {
            if (characterVisible && currentCharacterImage != null && characterBounds.Width > 0 && characterBounds.Height > 0)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

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

                e.Graphics.DrawImage(currentCharacterImage, drawX, drawY, drawW, drawH);
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

        // ------------------------------------------------------------------
        //  STORY
        // ------------------------------------------------------------------
        private void LoadStory()
        {
            storyQueue.Enqueue(new DialogueLine("Y/N", "Your application was approved three days ago."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "Tonight is your first overnight shift at the emergency dispatch center."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "The building looks older than it did during your interview. Fluorescent lights buzz overhead. A wall clock ticks above a row of desks, and the CRT monitors cast a faint glow across the room."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "A printer rattles somewhere behind you."));
            storyQueue.Enqueue(new DialogueLine("NARRATION", "On the wall is a faded department slogan: EVERY CALL MATTERS, EVERY SECOND COUNTS."));


            storyQueue.Enqueue(new DialogueLine("KRISHA", "[A woman approaches your desk carrying a folder.] First night?", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Don't worry. The phones look intimidating, but most people just need someone to listen.", "smile", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "I'm Krisha Siblero. I'll be training you tonight.", "talking", CharSide.Left));

            storyQueue.Enqueue(new DialogueLine("CHARM", "Hey! Don't scare the newbie before they even get their headset on.", "giggle", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("CHARM", "I'm Charm Villareal. Your neighboring console, unofficial coffee supplier, and the person you should ask if you need anything.", "smile", CharSide.Right));

            storyQueue.Enqueue(new DialogueLine("KRISHA", "Anything?", "concerned", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARM", "Almost anything.", "giggle", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "That's what worries me.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARM", "See? She's already telling you stories about me.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "I'm telling them to keep an eye on you.", "concerned", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARM", "Same thing!", "giggle", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "She's been here for years. She knows everyone, remembers everyone's birthday, and somehow always knows when someone needs coffee.", "talking", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "She's also very good at making a serious situation sound less serious than it is.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARM", "I heard that!", "crashout", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "You were supposed to.", "smile", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "All right. Jokes aside, listen carefully. Rule number one: verify the address before sending a unit, whenever it's safe to do so.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Rule number two: check the incident log if something doesn't look right.", "serious", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "Rule number three: don't panic just because the caller does. A frightened person may struggle to explain what's happening.", "talking", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARM", "And rule number four: don't let Ysa catch you sleeping.", "smile", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "There is no rule number four.", "concerned", CharSide.Left));
            storyQueue.Enqueue(new DialogueLine("CHARM", "There is if you value your employment.", "serious", CharSide.Right));
            storyQueue.Enqueue(new DialogueLine("KRISHA", "[A telephone rings across the room. Charm immediately turns back to her console.] That's your first lesson. Let's see how you do.", "serious", CharSide.Left));
        }

        private void DisplayNextLine()
        {
            if (storyQueue.Count == 0)
            {
                lblSpeaker.Text = "SYSTEM";
                lblDialogue.Text = "End of current build.";
                lblIncoming.Text = "";
                lblArrow.Visible = false;
                currentCharacterImage = null;
                characterVisible = false;
                picCharacter.Visible = false;
                picBackground.Invalidate(true);
                return;
            }

            DialogueLine line = storyQueue.Dequeue();

            // Pick the background for this speaker or line (default for everyone else)
            Image wanted = defaultBackground;
            if (!string.IsNullOrEmpty(line.Background) && speakerBackgrounds.TryGetValue(line.Background, out Image lineBg))
            {
                wanted = lineBg;
            }
            else if (speakerBackgrounds.TryGetValue(line.Speaker, out Image spkBg))
            {
                wanted = spkBg;
            }

            if (wanted != null && wanted != originalBackground)
            {
                originalBackground = wanted;
                UpdateBackground();
            }

            if (line.Speaker == "NARRATION")
            {
                lblSpeaker.Text = "Y/N";
                lblIncoming.Text = "";
            }
            else
            {
                lblSpeaker.Text = line.Speaker;
                lblIncoming.Text = line.Speaker == "Y/N" ? "" : "INCOMING / VOICE";
            }

            // Show the speaking character with their expression.
            // The sprite is drawn entirely by PicBackground_Paint using characterBounds –
            // picCharacter is kept hidden because its Bounds are never set and would appear
            // at (0,0) with a tiny default size (the bug visible in the upper-left corner).
            Image charImg = GetCharacterImage(line.Speaker, line.Expression);
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

        private void TypingTimer_Tick(object sender, EventArgs e)
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
        private void Form_Click(object sender, EventArgs e)
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

        private void Tutorial_KeyDown(object sender, KeyEventArgs e)
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

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using var sf = new Forms.SettingsForm();
            sf.ShowExitButton = true;
            var result = sf.ShowDialog(this);
            if (result == DialogResult.Abort)
            {
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