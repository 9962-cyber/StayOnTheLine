using System.Text.Json;
using StayOnTheLine.Models;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Encapsulates persistent JSON storage, slot management, and file operations.
    /// Implements ISaveService (Abstraction).
    /// </summary>
    public class SaveManager : ISaveService
    {
        private const int MaxSlots = 3;
        private readonly string _saveDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        // Singleton instance for global accessibility across game screens
        private static SaveManager? s_instance;
        public static SaveManager Instance => s_instance ??= new SaveManager();

        public SaveManager(string? customDirectory = null)
        {
            _saveDirectory = customDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
            EnsureDirectoryExists();

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_saveDirectory))
                {
                    Directory.CreateDirectory(_saveDirectory);
                }
            }
            catch
            {
                // Fallback to local working folder if BaseDirectory is restricted
                string fallback = Path.Combine(Directory.GetCurrentDirectory(), "Saves");
                if (!Directory.Exists(fallback))
                {
                    Directory.CreateDirectory(fallback);
                }
            }
        }

        private string GetSlotFilePath(int slotIndex)
        {
            return Path.Combine(_saveDirectory, $"slot_{slotIndex}.json");
        }

        public bool HasAnySaves()
        {
            for (int i = 1; i <= MaxSlots; i++)
            {
                if (File.Exists(GetSlotFilePath(i)))
                    return true;
            }
            return false;
        }

        public int? GetFirstAvailableSlot()
        {
            for (int i = 1; i <= MaxSlots; i++)
            {
                if (!File.Exists(GetSlotFilePath(i)))
                    return i;
            }
            return null; // All slots full
        }

        public int? GetMostRecentSlot()
        {
            int? mostRecentSlot = null;
            DateTime latestTime = DateTime.MinValue;

            for (int i = 1; i <= MaxSlots; i++)
            {
                var profile = GetProfile(i);
                if (profile != null && profile.LastPlayedTimestamp > latestTime)
                {
                    latestTime = profile.LastPlayedTimestamp;
                    mostRecentSlot = i;
                }
            }

            return mostRecentSlot;
        }

        public OperatorProfile? GetProfile(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > MaxSlots)
                return null;

            string path = GetSlotFilePath(slotIndex);
            if (!File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<OperatorProfile>(json, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public IReadOnlyDictionary<int, OperatorProfile?> GetAllSlots()
        {
            var dict = new Dictionary<int, OperatorProfile?>();
            for (int i = 1; i <= MaxSlots; i++)
            {
                dict[i] = GetProfile(i);
            }
            return dict;
        }

        public OperatorProfile CreateNewProfile(string operatorName, int? preferredSlot = null)
        {
            int targetSlot = preferredSlot ?? GetFirstAvailableSlot() ?? 1;

            var profile = new OperatorProfile(targetSlot, operatorName, currentShift: 1, callsLogged: 0);
            SaveProfile(profile);
            return profile;
        }

        public void SaveProfile(OperatorProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            EnsureDirectoryExists();
            string path = GetSlotFilePath(profile.SlotIndex);
            string json = JsonSerializer.Serialize(profile, _jsonOptions);
            File.WriteAllText(path, json);
        }

        public bool DeleteProfile(int slotIndex)
        {
            string path = GetSlotFilePath(slotIndex);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
