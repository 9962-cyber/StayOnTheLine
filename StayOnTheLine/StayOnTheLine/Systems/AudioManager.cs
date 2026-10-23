using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION & SINGLETON
    /// Manages the dual-layered psychological horror audio atmosphere:
    /// 1. Primary Background Music (e.g., user-supplied background.mp3)
    /// 2. Layered Subtle Ambient Drone & Analog Tape Hiss (continuous, seamless loop without fade-out, played at lower volume)
    /// 3. Vintage Dispatch SFX (mechanical typewriter clicks, bells, paper rustling, archival ink stamps, burn sizzle).
    /// Uses native Win32 MCI for non-blocking, multi-channel concurrent audio.
    /// </summary>
    public sealed class AudioManager
    {
        private static AudioManager? s_instance;
        public static AudioManager Instance => s_instance ??= new AudioManager();

        private readonly string _audioDir;
        private readonly Random _rng = new();
        private readonly ConcurrentDictionary<string, string> _loadedAliases = new();
        private bool _isAmbientPlaying = false;
        private bool _isMuted = false;

        private int _masterVolume = 80;
        private int _ambientVolume = 75;
        private int _sfxVolume = 85;

        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi)]
        private static extern int mciSendString(string lpszCommand, StringBuilder? lpszReturnString, int cchReturn, IntPtr hwndCallback);

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                if (_isMuted) StopAmbientLoop();
                else PlayAmbientLoop();
            }
        }

        private AudioManager()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            _audioDir = Path.Combine(basePath, "Assets", "Audio");
            if (!Directory.Exists(_audioDir))
            {
                _audioDir = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Audio");
            }

            PreloadCommonAudio();
        }

        private void PreloadCommonAudio()
        {
            // 1. Dual-layered background channels:
            // Channel A: Primary background music (background.mp3)
            OpenAudioFile("background.mp3", "BackgroundMusic");
            // Channel B: Subtle continuous drone + tape hiss (ambient_drone.wav)
            OpenAudioFile("ambient_drone.wav", "AmbientDrone");

            // 2. Dispatch sound effects
            OpenAudioFile("paper_rustle.wav", "PaperRustle");
            OpenAudioFile("ink_stamp.wav", "InkStamp");
            OpenAudioFile("burn_hiss.wav", "BurnHiss");
            OpenAudioFile("frequency_blip.wav", "FreqBlip");
            OpenAudioFile("typewriter_1.wav", "Typewriter1");
            OpenAudioFile("typewriter_2.wav", "Typewriter2");
            OpenAudioFile("typewriter_3.wav", "Typewriter3");
            OpenAudioFile("typewriter_bell.wav", "TypewriterBell");

            try
            {
                ApplySettings(SettingsManager.Instance.Current);
            }
            catch { }
        }

        /// <summary>
        /// Updates MCI channel volumes dynamically in real time according to persistent GameSettings.
        /// Layers the ambient drone at a softer, subtle volume level underneath the background music.
        /// </summary>
        public void ApplySettings(Models.GameSettings settings)
        {
            _masterVolume = settings.MasterVolume;
            _ambientVolume = settings.AmbientVolume;
            _sfxVolume = settings.SfxVolume;
            _isMuted = settings.IsMuted;

            // 1. Primary Background Music (Full ambient volume calibration)
            int effectiveMusic = _isMuted ? 0 : (int)(10 * (_masterVolume / 100f) * _ambientVolume);
            mciSendString($"setaudio BackgroundMusic volume to {effectiveMusic}", null, 0, IntPtr.Zero);

            // 2. Layered Ambient Drone / Tape Hiss (Subtle eerie undercurrent ~35% volume)
            int effectiveDrone = _isMuted ? 0 : (int)(10 * (_masterVolume / 100f) * _ambientVolume * 0.35f);
            mciSendString($"setaudio AmbientDrone volume to {effectiveDrone}", null, 0, IntPtr.Zero);

            // 3. Dispatch Sound Effects
            int effectiveSfx = _isMuted ? 0 : (int)(10 * (_masterVolume / 100f) * _sfxVolume);
            string[] sfxAliases = { "PaperRustle", "InkStamp", "BurnHiss", "FreqBlip", "Typewriter1", "Typewriter2", "Typewriter3", "TypewriterBell" };
            foreach (var alias in sfxAliases)
            {
                mciSendString($"setaudio {alias} volume to {effectiveSfx}", null, 0, IntPtr.Zero);
            }
        }

        private string? ResolveAudioFilePath(string fileName)
        {
            // 1. Direct match in Assets/Audio
            string direct = Path.Combine(_audioDir, fileName);
            if (File.Exists(direct)) return direct;

            // 2. Direct match in parent Assets folder
            string? parentDir = Path.GetDirectoryName(_audioDir);
            if (!string.IsNullOrEmpty(parentDir))
            {
                string parentCandidate = Path.Combine(parentDir, fileName);
                if (File.Exists(parentCandidate)) return parentCandidate;
            }

            // 3. Special handling for background music (.mp3, .mp3.mp3, .wav)
            if (fileName.Contains("background", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("music", StringComparison.OrdinalIgnoreCase))
            {
                string[] bgNames = { "background.mp3", "background.mp3.mp3", "background.wav", "music.mp3", "music.wav" };
                foreach (var bg in bgNames)
                {
                    string cand = Path.Combine(_audioDir, bg);
                    if (File.Exists(cand)) return cand;
                    if (!string.IsNullOrEmpty(parentDir))
                    {
                        string candParent = Path.Combine(parentDir, bg);
                        if (File.Exists(candParent)) return candParent;
                    }
                }

                // Check directory wildcard search
                try
                {
                    if (Directory.Exists(_audioDir))
                    {
                        var files = Directory.GetFiles(_audioDir, "*background*.*");
                        if (files.Length > 0) return files[0];
                    }
                }
                catch { }
            }

            // 4. Special handling for ambient drone
            if (fileName.Contains("ambient", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("drone", StringComparison.OrdinalIgnoreCase))
            {
                string[] droneNames = { "ambient_drone.wav", "ambient_drone.mp3", "ambient.wav", "ambient.mp3" };
                foreach (var dn in droneNames)
                {
                    string cand = Path.Combine(_audioDir, dn);
                    if (File.Exists(cand)) return cand;
                }
            }

            // 5. Check alternative extensions (.mp3, .wav, .ogg, .mp3.mp3)
            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string[] extensions = { ".mp3", ".wav", ".ogg", ".mp3.mp3" };
            foreach (var ext in extensions)
            {
                string candidate = Path.Combine(_audioDir, nameWithoutExt + ext);
                if (File.Exists(candidate)) return candidate;
            }

            return null;
        }

        private void OpenAudioFile(string fileName, string alias)
        {
            try
            {
                string? path = ResolveAudioFilePath(fileName);
                if (path != null && File.Exists(path))
                {
                    mciSendString($"close {alias}", null, 0, IntPtr.Zero);
                    mciSendString($"open \"{path}\" type mpegvideo alias {alias}", null, 0, IntPtr.Zero);
                    mciSendString($"setaudio {alias} volume to 1000", null, 0, IntPtr.Zero);
                    _loadedAliases[alias] = path;
                }
            }
            catch { }
        }

        /// <summary>
        /// Starts the dual-layered continuous audio loop:
        /// 1. Primary Background Music (background.mp3)
        /// 2. Layered subtle ambient drone and analog tape hiss (ambient_drone.wav at lower gain)
        /// </summary>
        public void PlayAmbientLoop()
        {
            if (_isMuted || _isAmbientPlaying) return;

            try
            {
                // Channel 1: Primary Background Music
                if (!_loadedAliases.ContainsKey("BackgroundMusic"))
                {
                    OpenAudioFile("background.mp3", "BackgroundMusic");
                }
                if (_loadedAliases.ContainsKey("BackgroundMusic"))
                {
                    mciSendString("play BackgroundMusic repeat", null, 0, IntPtr.Zero);
                    int effectiveMusic = _isMuted ? 0 : (int)(10 * (_masterVolume / 100f) * _ambientVolume);
                    mciSendString($"setaudio BackgroundMusic volume to {effectiveMusic}", null, 0, IntPtr.Zero);
                }

                // Channel 2: Layered subtle ambient drone & tape hiss (softer layer underneath)
                if (!_loadedAliases.ContainsKey("AmbientDrone"))
                {
                    OpenAudioFile("ambient_drone.wav", "AmbientDrone");
                }
                if (_loadedAliases.ContainsKey("AmbientDrone"))
                {
                    mciSendString("play AmbientDrone repeat", null, 0, IntPtr.Zero);
                    int effectiveDrone = _isMuted ? 0 : (int)(10 * (_masterVolume / 100f) * _ambientVolume * 0.35f);
                    mciSendString($"setaudio AmbientDrone volume to {effectiveDrone}", null, 0, IntPtr.Zero);
                }

                _isAmbientPlaying = true;
            }
            catch { }
        }

        /// <summary>
        /// Stops both layered background audio loops.
        /// </summary>
        public void StopAmbientLoop()
        {
            if (!_isAmbientPlaying) return;

            try
            {
                mciSendString("stop BackgroundMusic", null, 0, IntPtr.Zero);
                mciSendString("stop AmbientDrone", null, 0, IntPtr.Zero);
            }
            catch { }
            finally
            {
                _isAmbientPlaying = false;
            }
        }

        /// <summary>
        /// Plays an analog radio frequency tick / slot hover blip.
        /// </summary>
        public void PlayFrequencyBlip()
        {
            if (_isMuted) return;
            PlayAlias("FreqBlip");
        }

        /// <summary>
        /// Plays a vintage parchment / ledger paper rustling sound.
        /// </summary>
        public void PlayPaperRustle()
        {
            if (_isMuted) return;
            PlayAlias("PaperRustle");
        }

        /// <summary>
        /// Plays a heavy physical rubber stamp impact thud.
        /// </summary>
        public void PlayInkStamp()
        {
            if (_isMuted) return;
            PlayAlias("InkStamp");
        }

        /// <summary>
        /// Plays an ominous flame burn and hiss sound when incinerating a shift dossier.
        /// </summary>
        public void PlayBurnHiss()
        {
            if (_isMuted) return;
            PlayAlias("BurnHiss");
        }

        /// <summary>
        /// Plays a mechanical typewriter keystroke click with randomized pitch variation.
        /// </summary>
        public void PlayTypewriterKeystroke()
        {
            if (_isMuted) return;

            int variant = _rng.Next(1, 4);
            PlayAlias($"Typewriter{variant}");
        }

        /// <summary>
        /// Plays an antique carriage return brass bell chime.
        /// </summary>
        public void PlayTypewriterBell()
        {
            if (_isMuted) return;
            PlayAlias("TypewriterBell");
        }

        private void PlayAlias(string alias)
        {
            try
            {
                mciSendString($"play {alias} from 0", null, 0, IntPtr.Zero);
            }
            catch { }
        }
    }
}
