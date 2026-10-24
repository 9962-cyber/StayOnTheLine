using System;
using StayOnTheLine.Models;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principles Demonstrated:
    /// 1. FACTORY PATTERN: Decouples shift scenario creation from UI forms and save services.
    /// 2. POLYMORPHISM: Instantiates appropriate IShiftScenario implementation dynamically
    ///    based on runtime shift index (Shift 1, Shift 2, future shifts).
    /// </summary>
    public static class ShiftScenarioFactory
    {
        /// <summary>True when a playable scenario exists for this shift (used to chain shifts).</summary>
        public static bool HasScenario(int shiftNumber) => shiftNumber is 1 or 2;

        public static IShiftScenario Create(int shiftNumber)
        {
            return shiftNumber switch
            {
                1 => new Shift1TutorialScenario(),
                2 => new Shift2Scenario(), // <--- Wired up!
                _ => new Shift1TutorialScenario()
            };
        }
    }
}
