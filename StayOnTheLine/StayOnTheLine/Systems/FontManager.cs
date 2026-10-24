using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Encapsulates custom font loading, GDI resources, and font caching
    /// following the Single Responsibility Principle.
    ///
    /// The game's type is IM FELL DW Pica SC everywhere. The font files ship in Assets/Fonts and are
    /// not installed on the player's PC, so they are registered with Windows for this process at
    /// startup: that lets <c>new Font("IM FELL DW Pica SC", ...)</c> in the designer files, Labels and
    /// TextRenderer find the real font instead of silently falling back to a system sans-serif.
    /// </summary>
    public static class FontManager
    {
        public const string FamilyName = "IM FELL DW Pica SC";

        private const uint FR_PRIVATE = 0x10;

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern int AddFontResourceEx(string name, uint flags, IntPtr reserved);

        private static readonly PrivateFontCollection s_fontCollection = new();
        private static FontFamily? s_imFellSCFamily;
        private static bool s_isInitialized = false;

        public static bool IsLoaded => s_isInitialized;

        static FontManager()
        {
            InitializeFonts();
        }

        /// <summary>
        /// Call first thing at startup, before any form creates a font, so every by-name font
        /// request already sees IM FELL DW Pica SC.
        /// </summary>
        public static void EnsureLoaded() { }

        private static void InitializeFonts()
        {
            if (s_isInitialized) return;
            s_isInitialized = true;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fontsDir = Path.Combine(baseDir, "Assets", "Fonts");

                if (!Directory.Exists(fontsDir))
                {
                    string candidate = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Fonts");
                    if (Directory.Exists(candidate))
                        fontsDir = candidate;
                }

                foreach (string file in new[] { "IMFellDWPicaSC-Regular.ttf", "IMFellDWPica-Regular.ttf", "IMFellDWPica-Italic.ttf" })
                {
                    string path = Path.Combine(fontsDir, file);
                    if (!File.Exists(path)) continue;

                    // Visible to GDI (Labels, TextRenderer) and to by-name lookups, for this process only
                    try { AddFontResourceEx(path, FR_PRIVATE, IntPtr.Zero); } catch { }
                }

                string scPath = Path.Combine(fontsDir, "IMFellDWPicaSC-Regular.ttf");
                if (File.Exists(scPath))
                {
                    s_fontCollection.AddFontFile(scPath);
                    s_imFellSCFamily = s_fontCollection.Families.FirstOrDefault(f => f.Name.Contains("SC", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch
            {
                // Fallback gracefully to system fonts
            }
        }

        /// <summary>
        /// Retrieves the IM FELL DW Pica SC font with fallback to installed system font or Georgia.
        /// </summary>
        public static Font GetSmallCapsFont(float size, FontStyle style = FontStyle.Regular)
        {
            if (s_imFellSCFamily != null)
            {
                try { return new Font(s_imFellSCFamily, size, style, GraphicsUnit.Point); } catch { }
            }

            return TryCreateInstalled(FamilyName, size, style)
                ?? TryCreateInstalled("Georgia", size, style)
                ?? new Font(FontFamily.GenericSerif, size, style, GraphicsUnit.Point);
        }

        /// <summary>Spoken and written dialogue: upright IM FELL DW Pica SC.</summary>
        public static Font GetDialogueFont(float size) => GetSmallCapsFont(size);

        /// <summary>Body text. The whole game uses IM FELL DW Pica SC.</summary>
        public static Font GetRegularFont(float size, FontStyle style = FontStyle.Regular) => GetSmallCapsFont(size, style);

        /// <summary>Emphasis (taglines, stage directions): IM FELL DW Pica SC, slanted.</summary>
        public static Font GetItalicFont(float size) => GetSmallCapsFont(size, FontStyle.Italic);

        /// <summary>
        /// Terminal / system text: Consolas, falling back to the generic monospace font.
        /// </summary>
        public static Font GetMonospaceFont(float size, FontStyle style = FontStyle.Regular)
        {
            return TryCreateInstalled("Consolas", size, style)
                ?? TryCreateInstalled("Courier New", size, style)
                ?? new Font(FontFamily.GenericMonospace, size, style, GraphicsUnit.Point);
        }

        // GDI+ silently substitutes a missing family, so compare the resolved name
        private static Font? TryCreateInstalled(string family, float size, FontStyle style)
        {
            try
            {
                var font = new Font(family, size, style, GraphicsUnit.Point);
                if (string.Equals(font.Name, family, StringComparison.OrdinalIgnoreCase)) return font;
                font.Dispose();
            }
            catch { }
            return null;
        }
    }
}
