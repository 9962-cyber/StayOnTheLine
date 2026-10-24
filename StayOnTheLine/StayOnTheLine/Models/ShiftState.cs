using System;

namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Tracks runtime shift resources, available patrol units,
    /// logged calls, and investigation score.
    /// </summary>
    public class ShiftState
    {
        private int _availableUnits = 3;
        private int _totalUnits = 3;
        private int _callsLogged = 0;
        private int _cluesFound = 0;

        public int AvailableUnits
        {
            get => _availableUnits;
            set => _availableUnits = Math.Clamp(value, 0, _totalUnits);
        }

        public int TotalUnits
        {
            get => _totalUnits;
            set => _totalUnits = Math.Max(1, value);
        }

        public int CallsLogged
        {
            get => _callsLogged;
            set => _callsLogged = Math.Max(0, value);
        }

        public int CluesFound
        {
            get => _cluesFound;
            set => _cluesFound = Math.Max(0, value);
        }

        public int ProtocolErrors { get; set; } = 0;
        public int Errors => ProtocolErrors;

        public int CurrentCallIndex { get; set; } = 0;
        public bool ShiftCompleted { get; set; } = false;

        public ShiftState(int initialUnits = 3)
        {
            _totalUnits = initialUnits;
            _availableUnits = initialUnits;
        }
    }
}
