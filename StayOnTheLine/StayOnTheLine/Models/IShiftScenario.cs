using System.Collections.Generic;

namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: ABSTRACTION
    /// Contract defining an operational shift scenario.
    /// Standardizes queue management, call generation, and evaluation
    /// across Shift 1 (Tutorial), Shift 2, and future shifts without coupling to the UI.
    /// </summary>
    public interface IShiftScenario
    {
        int ShiftNumber { get; }
        string ShiftTitle { get; }
        string SupervisorName { get; }
        int InitialUnits { get; }
        bool HasTutorialGuidance { get; }

        /// <summary>Contents of the Shift Briefing book shown before the shift starts.</summary>
        ShiftBriefing Briefing { get; }
        List<EmergencyCall> GenerateCalls();
        string GetDebriefMessage(ShiftState state);
    }
}
