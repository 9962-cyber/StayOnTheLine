using System;

namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Defines caller transcript print speeds for teletype dialogue rendering.
    /// </summary>
    public enum TextSpeed
    {
        Slow = 0,     // 65ms per character (authentic typewriter cadence)
        Standard = 1, // 30ms per character (recommended pacing)
        Instant = 2   // 0ms (immediate line printout)
    }

    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Encapsulates persistent player preferences, audio calibrations,
    /// teletype print speed, and station configuration settings.
    /// </summary>
    public class GameSettings
    {
        public int MasterVolume { get; set; } = 80;    // 0 - 100%
        public int AmbientVolume { get; set; } = 75;   // 0 - 100%
        public int SfxVolume { get; set; } = 85;       // 0 - 100%
        public bool IsMuted { get; set; } = false;
        public TextSpeed PrintSpeed { get; set; } = TextSpeed.Standard;
        public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;

        public GameSettings()
        {
        }

        public GameSettings(int master, int ambient, int sfx, bool muted, TextSpeed speed)
        {
            MasterVolume = Math.Clamp(master, 0, 100);
            AmbientVolume = Math.Clamp(ambient, 0, 100);
            SfxVolume = Math.Clamp(sfx, 0, 100);
            IsMuted = muted;
            PrintSpeed = speed;
            LastModifiedUtc = DateTime.UtcNow;
        }

        public int GetDelayPerCharacterMs() => PrintSpeed switch
        {
            TextSpeed.Slow => 65,
            TextSpeed.Standard => 30,
            TextSpeed.Instant => 0,
            _ => 30
        };
    }
}
