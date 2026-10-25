using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION & SINGLETON
    /// Manages the multi-layered psychological horror audio atmosphere:
    /// 1. Menu Ambience (flickering fluorescent light buzz + intermittent lonely telephone ring).
    /// 2. Gameplay Atmosphere (shifts_bg.mp3 + subtle analog room clock ticking).
    /// 3. Switchboard Trunk Rings (emergency call telephone ring).
    /// 4. Tactile Dispatch SFX (mechanical typewriter clicks, dot-matrix printer rattles, bells, paper slides, stamps, burn hiss).
    /// 5. Narrative & Psychological Horror Stingers (Charmee giggles, intruder screams, haunted Line 08 laughter).
    /// Uses native Win32 MCI for non-blocking, multi-channel concurrent audio playback.
    /// </summary>
    public sealed class AudioManager
    {
        private static AudioManager? s_instance;
        public static AudioManager Instance => s_instance ??= new AudioManager();

        private readonly string _audioDir;
        private readonly Random _rng = new();
        private readonly ConcurrentDictionary<string, string> _loadedAliases = new();

        private bool _isMenuAmbiencePlaying = false;
        private bool _isGameplayAtmospherePlaying = false;
        private bool _isPhoneRinging = false;
        private bool _isOfficeBgPhonesPlaying = false;
        private bool _isMuted = false;

        private int _masterVolume = 80;
        private int _ambientVolume = 75;
        private int _sfxVolume = 85;

        // Relative loudness of repetitive frequent keystrokes and tactile clicks against overall SFX
        private const float TypingVolumeShare = 0.28f;
        private const float ClickVolumeShare = 0.32f;
        private const float BlipVolumeShare = 0.40f;
        // Phone ring volume scaling: lower gain so the telephone alert is pleasant and not piercing
        private const float PhoneRingVolumeShare = 0.35f;

        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi)]
        private static extern int mciSendString(string lpszCommand, StringBuilder? lpszReturnString, int cchReturn, IntPtr hwndCallback);

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                if (_isMuted)
                {
                    StopMenuAmbience();
                    StopGameplayAtmosphere();
                    StopOfficeBackgroundRing();
                    StopPhoneRing();
                }
                else
                {
                    ApplySettings(SettingsManager.Instance.Current);
                }
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
            // 1. Menu Ambience (STRICTLY for loading screen and main menu)
            OpenAudioFile("background.mp3", "MenuBgm");

            // 2. Gameplay Atmosphere Layers (Office environment during shift play)
            OpenAudioFile("shifts_bg.mp3", "GameplayBgm");
            OpenAudioFile("light_buzz.mp3", "OfficeBuzz");
            OpenAudioFile("clock_ticking.mp3", "RoomClock");
            OpenAudioFile("telephone_rings.mp3", "OfficeBgPhones");
            OpenAudioFile("printer_rattles.mp3", "PrinterTelex");

            // 3. Emergency Call Telephone Ring (player's console direct ring)
            OpenAudioFile("telephone_rings.mp3", "CallerRing");

            // 4. Dispatch & Tactile Sound Effects
            OpenAudioFile("paper_slide.mp3", "PaperSlide");
            OpenAudioFile("paper_rustle.wav", "PaperRustle");
            OpenAudioFile("click.mp3", "Click");
            OpenAudioFile("ink_stamp.wav", "InkStamp");
            OpenAudioFile("burn_hiss.wav", "BurnHiss");
            OpenAudioFile("burn_hiss.wav", "LineStatic");
            OpenAudioFile("frequency_blip.wav", "FreqBlip");
            OpenAudioFile("typewriter_1.wav", "Typewriter1");
            OpenAudioFile("typewriter_2.wav", "Typewriter2");
            OpenAudioFile("typewriter_3.wav", "Typewriter3");
            OpenAudioFile("typewriter_bell.wav", "TypewriterBell");

            // 5. Narrative & Psychological Horror Stingers
            OpenAudioFile("charm_giggles.mp3", "CharmGiggle");
            OpenAudioFile("female_scream.mp3", "FemaleScream");
            OpenAudioFile("female_villainlaugh.mp3", "FemaleLaugh");

            try
            {
                ApplySettings(SettingsManager.Instance.Current);
            }
            catch { }
        }

        /// <summary>
        /// Updates MCI channel volumes dynamically in real time according to persistent GameSettings.
        /// </summary>
        public void ApplySettings(Models.GameSettings settings)
        {
            _masterVolume = settings.MasterVolume;
            _ambientVolume = settings.AmbientVolume;
            _sfxVolume = settings.SfxVolume;
            _isMuted = settings.IsMuted;

            float m = _isMuted ? 0 : (_masterVolume / 100f);
            int effectiveAmbient = (int)(10 * m * _ambientVolume);
            int effectiveSfx = (int)(10 * m * _sfxVolume);

            // 1. Menu Ambience (background.mp3 strictly in loading/menu - full ambient level)
            mciSendString($"setaudio MenuBgm volume to {effectiveAmbient}", null, 0, IntPtr.Zero);

            // 2. Gameplay Atmosphere Layers (Dispatch office)
            // Shift music (primary score)
            mciSendString($"setaudio GameplayBgm volume to {effectiveAmbient}", null, 0, IntPtr.Zero);
            // Fluorescent light buzz (~25% of ambient level)
            mciSendString($"setaudio OfficeBuzz volume to {(int)(effectiveAmbient * 0.25f)}", null, 0, IntPtr.Zero);
            // Station room wall clock ticking (~25% of ambient level)
            mciSendString($"setaudio RoomClock volume to {(int)(effectiveAmbient * 0.25f)}", null, 0, IntPtr.Zero);
            // Soft distant background telephone rings from other dispatchers across the room (~12% of ambient level)
            mciSendString($"setaudio OfficeBgPhones volume to {(int)(effectiveAmbient * 0.12f)}", null, 0, IntPtr.Zero);
            // Soft background printer / telex rattles (~15% of ambient level)
            mciSendString($"setaudio PrinterTelex volume to {(int)(effectiveAmbient * 0.15f)}", null, 0, IntPtr.Zero);

            // 3. Incoming emergency call ring (scaled down so it is an urgent alert but not piercing)
            int effectiveRing = (int)(effectiveSfx * PhoneRingVolumeShare);
            mciSendString($"setaudio CallerRing volume to {effectiveRing}", null, 0, IntPtr.Zero);
            mciSendString($"setaudio BackgroundMusic volume to {effectiveRing}", null, 0, IntPtr.Zero);

            // 4. Standard dispatch SFX
            string[] sfxFull = { "PaperRustle", "PaperSlide", "InkStamp", "BurnHiss", "TypewriterBell", "CharmGiggle", "FemaleScream", "FemaleLaugh" };
            foreach (var alias in sfxFull)
            {
                mciSendString($"setaudio {alias} volume to {effectiveSfx}", null, 0, IntPtr.Zero);
            }

            // 5. Button clicks sit softly so repeated navigation is gentle
            int effectiveClick = (int)(effectiveSfx * ClickVolumeShare);
            mciSendString($"setaudio Click volume to {effectiveClick}", null, 0, IntPtr.Zero);

            // 6. Text typing keystrokes sit softly under other audio so dialogue typing isn't jarring
            int effectiveTyping = (int)(effectiveSfx * TypingVolumeShare);
            foreach (var alias in new[] { "Typewriter1", "Typewriter2", "Typewriter3" })
            {
                mciSendString($"setaudio {alias} volume to {effectiveTyping}", null, 0, IntPtr.Zero);
            }

            // 7. Soft CRT frequency hover blip
            mciSendString($"setaudio FreqBlip volume to {(int)(effectiveSfx * BlipVolumeShare)}", null, 0, IntPtr.Zero);

            if (_isMuted)
            {
                StopPhoneRing();
                StopMenuAmbience();
                StopGameplayAtmosphere();
            }
        }

        private string? ResolveAudioFilePath(string fileName)
        {
            string[] searchDirs = {
                _audioDir,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Audio"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets"),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets", "Audio")),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Assets"))
            };

            // 1. Direct match across search dirs
            foreach (var dir in searchDirs)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                string direct = Path.Combine(dir, fileName);
                if (File.Exists(direct)) return direct;
            }

            // 2. Specific alias fallbacks
            string l = fileName.ToLowerInvariant();
            string[] fallbacks = l switch
            {
                var s when s.Contains("light_buzz") || s.Contains("buzz") => new[] { "light_buzz.mp3", "buzz.mp3" },
                var s when s.Contains("ring") || s.Contains("telering") => new[] { "telephone_rings.mp3" },
                var s when s.Contains("shifts") || s.Contains("shift") => new[] { "shifts_bg.mp3" },
                var s when s.Contains("background") || s.Contains("menu") => new[] { "background.mp3" },
                var s when s.Contains("clock") => new[] { "clock_ticking.mp3", "clock_tick.mp3" },
                var s when s.Contains("laugh") => new[] { "female_villainlaugh.mp3", "female_laugh.mp3" },
                var s when s.Contains("scream") => new[] { "female_scream.mp3" },
                var s when s.Contains("giggle") => new[] { "charm_giggles.mp3" },
                var s when s.Contains("printer") => new[] { "printer_rattles.mp3" },
                var s when s.Contains("slide") => new[] { "paper_slide.mp3", "paper_rustle.wav" },
                var s when s.Contains("click") => new[] { "click.mp3" },
                _ => Array.Empty<string>()
            };

            foreach (var fb in fallbacks)
            {
                foreach (var dir in searchDirs)
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                    string p = Path.Combine(dir, fb);
                    if (File.Exists(p)) return p;
                }
            }

            // 3. Alternative extensions (.mp3, .wav, .ogg, .mp3.mp3)
            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string[] extensions = { ".mp3", ".wav", ".ogg", ".mp3.mp3" };
            foreach (var dir in searchDirs)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                foreach (var ext in extensions)
                {
                    string candidate = Path.Combine(dir, nameWithoutExt + ext);
                    if (File.Exists(candidate)) return candidate;
                }
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
                    int res = mciSendString($"open \"{path}\" type mpegvideo alias {alias}", null, 0, IntPtr.Zero);
                    if (res != 0)
                    {
                        mciSendString($"open \"{path}\" alias {alias}", null, 0, IntPtr.Zero);
                    }
                    mciSendString($"setaudio {alias} volume to 1000", null, 0, IntPtr.Zero);
                    _loadedAliases[alias] = path;
                }
            }
            catch { }
        }

        /// <summary>
        /// Starts the menu atmosphere: background.mp3 STRICTLY on loading screen and main menu.
        /// Silences gameplay atmosphere first.
        /// </summary>
        public void StartMenuAmbience()
        {
            if (_isMuted) return;
            StopGameplayAtmosphere();
            if (_isMenuAmbiencePlaying) return;
            try
            {
                if (!_loadedAliases.ContainsKey("MenuBgm"))
                {
                    OpenAudioFile("background.mp3", "MenuBgm");
                }
                mciSendString("seek MenuBgm to 0", null, 0, IntPtr.Zero);
                mciSendString("play MenuBgm repeat", null, 0, IntPtr.Zero);
                _isMenuAmbiencePlaying = true;
            }
            catch { }
        }

        /// <summary>
        /// Kills menu atmosphere completely by closing the MCI device so background.mp3 never leaks into gameplay.
        /// </summary>
        public void StopMenuAmbience()
        {
            try
            {
                mciSendString("stop MenuBgm", null, 0, IntPtr.Zero);
                mciSendString("close MenuBgm", null, 0, IntPtr.Zero);
                _loadedAliases.TryRemove("MenuBgm", out _);

                // Defensively cleanup any legacy aliases
                mciSendString("stop MenuBuzz", null, 0, IntPtr.Zero);
                mciSendString("close MenuBuzz", null, 0, IntPtr.Zero);
                mciSendString("stop MenuRing", null, 0, IntPtr.Zero);
                mciSendString("close MenuRing", null, 0, IntPtr.Zero);
                _loadedAliases.TryRemove("MenuBuzz", out _);
                _loadedAliases.TryRemove("MenuRing", out _);
            }
            catch { }
            finally
            {
                _isMenuAmbiencePlaying = false;
            }
        }

        /// <summary>
        /// Starts the shift gameplay atmosphere: shifts_bg.mp3 background music layered with fluorescent light buzz,
        /// station wall clock ticking, soft distant office phone rings, and background printer rattles.
        /// Silences menu atmosphere first.
        /// </summary>
        public void StartGameplayAtmosphere()
        {
            if (_isMuted) return;
            StopMenuAmbience();
            if (_isGameplayAtmospherePlaying) return;
            try
            {
                string[] layers = { "GameplayBgm", "OfficeBuzz", "RoomClock", "PrinterTelex" };
                foreach (var alias in layers)
                {
                    if (!_loadedAliases.ContainsKey(alias))
                    {
                        string file = alias switch
                        {
                            "GameplayBgm" => "shifts_bg.mp3",
                            "OfficeBuzz" => "light_buzz.mp3",
                            "RoomClock" => "clock_ticking.mp3",
                            "PrinterTelex" => "printer_rattles.mp3",
                            _ => ""
                        };
                        if (!string.IsNullOrEmpty(file)) OpenAudioFile(file, alias);
                    }
                    mciSendString($"seek {alias} to 0", null, 0, IntPtr.Zero);
                    mciSendString($"play {alias} repeat", null, 0, IntPtr.Zero);
                }
                _isGameplayAtmospherePlaying = true;
            }
            catch { }
        }

        /// <summary>
        /// Stops all gameplay office atmosphere loops.
        /// </summary>
        public void StopGameplayAtmosphere()
        {
            try
            {
                string[] layers = { "GameplayBgm", "OfficeBuzz", "RoomClock", "PrinterTelex" };
                foreach (var alias in layers)
                {
                    mciSendString($"stop {alias}", null, 0, IntPtr.Zero);
                    mciSendString($"seek {alias} to 0", null, 0, IntPtr.Zero);
                }
                StopOfficeBackgroundRing();
            }
            catch { }
            finally
            {
                _isGameplayAtmospherePlaying = false;
            }
        }

        /// <summary>
        /// Starts the distant, muffled background telephone rings across the dispatch office (DeskRoom view).
        /// </summary>
        public void StartOfficeBackgroundRing()
        {
            if (_isMuted || _isOfficeBgPhonesPlaying) return;
            try
            {
                if (!_loadedAliases.ContainsKey("OfficeBgPhones"))
                {
                    OpenAudioFile("telephone_rings.mp3", "OfficeBgPhones");
                }
                if (_loadedAliases.ContainsKey("OfficeBgPhones"))
                {
                    float m = _isMuted ? 0 : (_masterVolume / 100f);
                    int effectiveAmbient = (int)(10 * m * _ambientVolume);
                    int bgVol = Math.Clamp((int)(effectiveAmbient * 0.12f), 0, 1000);
                    mciSendString($"setaudio OfficeBgPhones volume to {bgVol}", null, 0, IntPtr.Zero);
                    mciSendString("seek OfficeBgPhones to 0", null, 0, IntPtr.Zero);
                    mciSendString("play OfficeBgPhones repeat", null, 0, IntPtr.Zero);
                    _isOfficeBgPhonesPlaying = true;
                }
            }
            catch { }
        }

        /// <summary>
        /// Stops the background telephone rings (e.g. when zooming into the computer terminal).
        /// </summary>
        public void StopOfficeBackgroundRing()
        {
            if (!_isOfficeBgPhonesPlaying) return;
            try
            {
                mciSendString("stop OfficeBgPhones", null, 0, IntPtr.Zero);
                mciSendString("seek OfficeBgPhones to 0", null, 0, IntPtr.Zero);
            }
            catch { }
            finally
            {
                _isOfficeBgPhonesPlaying = false;
            }
        }

        // Backward compatibility aliases
        public void PlayAmbientLoop() => StartGameplayAtmosphere();
        public void StopAmbientLoop()
        {
            StopMenuAmbience();
            StopGameplayAtmosphere();
        }

        /// <summary>
        /// Starts the incoming emergency telephone ring on repeat (CallerRing); call when player's line is ringing.
        /// Calibrated with PhoneRingVolumeShare for a clear but comfortable, non-piercing volume.
        /// </summary>
        public void StartPhoneRing()
        {
            if (_isMuted || _isPhoneRinging) return;
            try
            {
                if (!_loadedAliases.ContainsKey("CallerRing"))
                {
                    OpenAudioFile("telephone_rings.mp3", "CallerRing");
                }
                if (_loadedAliases.ContainsKey("CallerRing"))
                {
                    int effectiveRing = (int)(10 * (_masterVolume / 100f) * _sfxVolume * PhoneRingVolumeShare);
                    mciSendString($"setaudio CallerRing volume to {effectiveRing}", null, 0, IntPtr.Zero);
                    mciSendString("play CallerRing from 0 repeat", null, 0, IntPtr.Zero);
                    _isPhoneRinging = true;
                }
            }
            catch { }
        }

        /// <summary>
        /// Stops the incoming emergency telephone ring; call when the line is answered or the scene moves on.
        /// </summary>
        public void StopPhoneRing()
        {
            if (!_isPhoneRinging) return;
            try
            {
                mciSendString("stop CallerRing", null, 0, IntPtr.Zero);
                mciSendString("stop BackgroundMusic", null, 0, IntPtr.Zero);
            }
            catch { }
            finally
            {
                _isPhoneRinging = false;
            }
        }

        public void PlayFrequencyBlip()
        {
            if (_isMuted) return;
            PlayAlias("FreqBlip");
        }

        public void PlayPaperRustle()
        {
            if (_isMuted) return;
            PlayAlias("PaperRustle");
        }

        public void PlayPaperSlide()
        {
            if (_isMuted) return;
            if (_loadedAliases.ContainsKey("PaperSlide")) PlayAlias("PaperSlide");
            else PlayAlias("PaperRustle");
        }

        public void PlayInkStamp()
        {
            if (_isMuted) return;
            PlayAlias("InkStamp");
        }

        public void PlayBurnHiss()
        {
            if (_isMuted) return;
            PlayAlias("BurnHiss");
        }

        public void PlayClick()
        {
            if (_isMuted) return;
            if (_loadedAliases.ContainsKey("Click"))
            {
                PlayAlias("Click");
            }
            else
            {
                PlayTypewriterKeystroke();
            }
        }

        public void PlayKeyClick()
        {
            if (_isMuted) return;
            if (_loadedAliases.ContainsKey("Click"))
            {
                PlayAlias("Click");
            }
            else
            {
                PlayTypewriterKeystroke();
            }
        }

        public void PlayPrinterRattle()
        {
            if (_isMuted) return;
            PlayAlias("PrinterTelex");
        }

        public void PlayCharmGiggle()
        {
            if (_isMuted) return;
            PlayAlias("CharmGiggle");
        }

        public void PlayCallerScream()
        {
            if (_isMuted) return;
            PlayAlias("FemaleScream");
        }

        public void PlayEerieLaugh()
        {
            if (_isMuted) return;
            PlayAlias("FemaleLaugh");
        }

        /// <summary>
        /// Plays an analog mechanical telephone receiver pickup (cradle switch click followed by short line carrier static burst).
        /// Grounding the emergency call in 1980s telephone line infrastructure.
        /// </summary>
        public void PlayPhonePickup()
        {
            if (_isMuted) return;
            StopPhoneRing();
            PlayClick();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(65);
                PlayLineStatic();
            });
        }

        /// <summary>
        /// Plays an analog mechanical telephone hangup (cradle clank + paper slide + cutting carrier line static).
        /// </summary>
        public void PlayPhoneHangup()
        {
            if (_isMuted) return;
            StopAlias("LineStatic");
            PlayClick();
            PlayPaperRustle();
        }

        /// <summary>
        /// Plays a burst of analog telephone trunk line carrier static/hiss.
        /// </summary>
        public void PlayLineStatic()
        {
            if (_isMuted) return;
            if (!_loadedAliases.ContainsKey("LineStatic"))
            {
                OpenAudioFile("burn_hiss.wav", "LineStatic");
            }
            if (_loadedAliases.ContainsKey("LineStatic"))
            {
                int effectiveStatic = (int)(10 * (_masterVolume / 100f) * _sfxVolume * 0.32f);
                mciSendString($"setaudio LineStatic volume to {effectiveStatic}", null, 0, IntPtr.Zero);
                mciSendString("play LineStatic from 0", null, 0, IntPtr.Zero);
            }
        }

        public void StopAlias(string alias)
        {
            try
            {
                mciSendString($"stop {alias}", null, 0, IntPtr.Zero);
                mciSendString($"seek {alias} to 0", null, 0, IntPtr.Zero);
            }
            catch { }
        }

        private long _lastKeystrokeTick = 0;
        private const int MinKeystrokeIntervalMs = 15;

        /// <summary>
        /// Plays a mechanical typewriter keystroke click with randomized pitch variation.
        /// Synchronous and instantaneous for responsive, crisp typing feedback.
        /// </summary>
        public void PlayTypewriterKeystroke()
        {
            if (_isMuted) return;

            long now = Environment.TickCount64;
            if (now - _lastKeystrokeTick < MinKeystrokeIntervalMs)
                return;
            _lastKeystrokeTick = now;

            int variant;
            lock (_rng)
            {
                variant = _rng.Next(1, 4);
            }
            PlayAlias($"Typewriter{variant}");
        }

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
