namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Encapsulates an operator's persistent shift progress, statistics,
    /// and save metadata with data validation and read-only external access.
    /// </summary>
    public class OperatorProfile
    {
        // OOP Principle: ENCAPSULATION (Private backing fields with validation)
        private string _operatorName = "Operator";
        private int _currentShift = 1;
        private int _callsLogged = 0;
        private int _slotIndex = 1;

        public int SlotIndex
        {
            get => _slotIndex;
            set => _slotIndex = Math.Clamp(value, 1, 3);
        }

        public string OperatorName
        {
            get => _operatorName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Operator name cannot be empty or whitespace.", nameof(value));
                _operatorName = value.Trim();
            }
        }

        public int CurrentShift
        {
            get => _currentShift;
            set => _currentShift = Math.Clamp(value, 1, 8);
        }

        public int CallsLogged
        {
            get => _callsLogged;
            set => _callsLogged = Math.Max(0, value);
        }

        public DateTime LastPlayedTimestamp { get; set; } = DateTime.Now;

        public int TrustScore { get; set; } = 100;

        public int ProtocolErrors { get; set; } = 0;

        public bool TutorialCompleted { get; set; } = false;

        public List<string> AnomalyFlags { get; set; } = new();

        /// <summary>
        /// Parameterless constructor required for JSON serialization.
        /// </summary>
        public OperatorProfile()
        {
        }

        public OperatorProfile(int slotIndex, string operatorName, int currentShift = 1, int callsLogged = 0)
        {
            SlotIndex = slotIndex;
            OperatorName = operatorName;
            CurrentShift = currentShift;
            CallsLogged = callsLogged;
            LastPlayedTimestamp = DateTime.Now;
        }

        // Helper computed properties for UI display
        public string FormattedSlotNumber => SlotIndex.ToString("D2");

        public string FormattedDetails => $"SHIFT {CurrentShift}  ·  {CallsLogged} CALLS LOGGED";

        public string FormattedTimestamp => LastPlayedTimestamp.ToString("MMM dd  ·  HH:mm").ToUpper();

        /// <summary>
        /// Updates the profile when finishing a shift.
        /// </summary>
        public void AdvanceShift(int newCalls = 0, int errors = 0)
        {
            CurrentShift = Math.Min(8, CurrentShift + 1);
            CallsLogged += newCalls;
            ProtocolErrors += errors;
            LastPlayedTimestamp = DateTime.Now;
        }
    }
}
