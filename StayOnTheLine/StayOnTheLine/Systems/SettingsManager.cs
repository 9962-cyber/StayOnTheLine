using System;
using System.IO;
using System.Text.Json;
using StayOnTheLine.Models;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION & SINGLETON
    /// Manages persistent JSON storage for game settings, audio calibration,
    /// teletype speed, and factory reset station operations.
    /// </summary>
    public sealed class SettingsManager
    {
        private static SettingsManager? s_instance;
        public static SettingsManager Instance => s_instance ??= new SettingsManager();

        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private GameSettings _currentSettings;

        public GameSettings Current => _currentSettings;

        private SettingsManager()
        {
            string saveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
            if (!Directory.Exists(saveDir))
            {
                try { Directory.CreateDirectory(saveDir); }
                catch { saveDir = Path.Combine(Directory.GetCurrentDirectory(), "Saves"); }
            }

            _settingsFilePath = Path.Combine(saveDir, "settings.json");
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            _currentSettings = LoadSettings();
        }

        public GameSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<GameSettings>(json, _jsonOptions);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch { }

            // Default fallback
            var defaults = new GameSettings();
            SaveSettings(defaults);
            return defaults;
        }

        public void SaveSettings(GameSettings newSettings)
        {
            try
            {
                newSettings.LastModifiedUtc = DateTime.UtcNow;
                _currentSettings = newSettings;

                string json = JsonSerializer.Serialize(newSettings, _jsonOptions);
                File.WriteAllText(_settingsFilePath, json);

                // Apply updated volumes to AudioManager immediately
                AudioManager.Instance.ApplySettings(newSettings);
            }
            catch { }
        }

        public void PurgeAllShiftLogs()
        {
            for (int i = 1; i <= 3; i++)
            {
                SaveManager.Instance.DeleteProfile(i);
            }
        }
    }
}
