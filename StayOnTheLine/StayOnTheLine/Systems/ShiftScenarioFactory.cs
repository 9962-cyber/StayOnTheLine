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
        public static IShiftScenario Create(int shiftNumber)
        {
            return shiftNumber switch
            {
                1 => new Shift1TutorialScenario(),
                // Shift 2 and subsequent shifts to be implemented by co-programmer:
                // 2 => new Shift2Scenario(),
                _ => new Shift1TutorialScenario()
            };
        }
    }
}
