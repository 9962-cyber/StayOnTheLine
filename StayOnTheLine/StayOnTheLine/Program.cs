namespace StayOnTheLine
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            // Register the bundled IM FELL DW Pica SC before any form creates a font
            StayOnTheLine.Systems.FontManager.EnsureLoaded();
            ApplicationConfiguration.Initialize();
            Application.Run(new LoadingForm());
        }
    }
}