namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: ABSTRACTION
    /// Contract defining save management operations. Decouples UI forms
    /// from file system details and serialization formats.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// Checks whether any saved shift files exist.
        /// </summary>
        bool HasAnySaves();

        /// <summary>
        /// Finds the first available empty slot (1, 2, or 3), or null if all slots are occupied.
        /// </summary>
        int? GetFirstAvailableSlot();

        /// <summary>
        /// Finds the slot index of the most recently played profile.
        /// </summary>
        int? GetMostRecentSlot();

        /// <summary>
        /// Retrieves a profile from a specific slot index (1..3), or null if empty.
        /// </summary>
        OperatorProfile? GetProfile(int slotIndex);

        /// <summary>
        /// Retrieves all slots (1, 2, 3) mapped to their profile or null if empty.
        /// </summary>
        IReadOnlyDictionary<int, OperatorProfile?> GetAllSlots();

        /// <summary>
        /// Creates a new profile in the specified or first available slot and saves it.
        /// </summary>
        OperatorProfile CreateNewProfile(string operatorName, int? preferredSlot = null);

        /// <summary>
        /// Saves or updates an existing operator profile to persistent storage.
        /// </summary>
        void SaveProfile(OperatorProfile profile);

        /// <summary>
        /// Deletes the save file in the specified slot, freeing it for a new shift.
        /// </summary>
        bool DeleteProfile(int slotIndex);
    }
}
