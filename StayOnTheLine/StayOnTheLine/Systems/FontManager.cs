using System.Drawing.Text;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Encapsulates custom font loading, GDI resources, and font caching
    /// following the Single Responsibility Principle.
    /// </summary>
    public static class FontManager
    {
        private static readonly PrivateFontCollection s_fontCollection = new();
        private static FontFamily? s_imFellSCFamily;
        private static FontFamily? s_imFellItalicFamily;

        // The Regular and Italic files share one family name, and GDI+ serves the italic face for
        // every style once both are in a collection, so the upright face gets a collection of its own.
        private static readonly PrivateFontCollection s_regularCollection = new();
        private static FontFamily? s_imFellRegularFamily;
        private static bool s_isInitialized = false;

        public static bool IsLoaded => s_isInitialized;

        static FontManager()
        {
            InitializeFonts();
        }

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

                string scPath = Path.Combine(fontsDir, "IMFellDWPicaSC-Regular.ttf");
                if (File.Exists(scPath))
                {
                    s_fontCollection.AddFontFile(scPath);
                }

                string italicPath = Path.Combine(fontsDir, "IMFellDWPica-Italic.ttf");
                if (File.Exists(italicPath))
                {
                    s_fontCollection.AddFontFile(italicPath);
                }

                string regPath = Path.Combine(fontsDir, "IMFellDWPica-Regular.ttf");
                if (File.Exists(regPath))
                {
                    s_fontCollection.AddFontFile(regPath);
                    s_regularCollection.AddFontFile(regPath);
                    if (s_regularCollection.Families.Length > 0)
                        s_imFellRegularFamily = s_regularCollection.Families[0];
                }

                foreach (var family in s_fontCollection.Families)
                {
                    if (family.Name.Contains("SC", StringComparison.OrdinalIgnoreCase))
                    {
                        s_imFellSCFamily = family;
                    }
                    else if (s_imFellItalicFamily == null)
                    {
                        s_imFellItalicFamily = family;
                    }
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

            try
            {
                return new Font("IM FELL DW Pica SC", size, style, GraphicsUnit.Point);
            }
            catch
            {
                return new Font("Georgia", size, style, GraphicsUnit.Point);
            }
        }

        /// <summary>
        /// Retrieves upright IM FELL DW Pica, falling back to Georgia, then the generic serif.
        /// </summary>
        public static Font GetRegularFont(float size, FontStyle style = FontStyle.Regular)
        {
            if (s_imFellRegularFamily != null)
            {
                try { return new Font(s_imFellRegularFamily, size, style, GraphicsUnit.Point); } catch { }
            }

            return TryCreateInstalled("IM FELL DW Pica", size, style)
                ?? TryCreateInstalled("Georgia", size, style)
                ?? new Font(FontFamily.GenericSerif, size, style, GraphicsUnit.Point);
        }

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

        /// <summary>
        /// Retrieves the IM FELL DW Pica Italic font with fallback.
        /// </summary>
        public static Font GetItalicFont(float size)
        {
            if (s_imFellItalicFamily != null)
            {
                try { return new Font(s_imFellItalicFamily, size, FontStyle.Italic, GraphicsUnit.Point); } catch { }
            }

            try
            {
                return new Font("IM FELL DW Pica", size, FontStyle.Italic, GraphicsUnit.Point);
            }
            catch
            {
                return new Font("Georgia", size, FontStyle.Italic, GraphicsUnit.Point);
            }
        }
    }
}
