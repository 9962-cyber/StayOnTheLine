using System.Collections.Generic;
using StayOnTheLine.Models;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// Shift 4 – Procedure. Ysa warns the night desk about irregularities in the incident logs, and
    /// the calls all point at records that no longer match what happened: Chloe's earlier call
    /// survives only as an audio file, 14 Bell Street is missing from the database, and an
    /// incident at 8 Miller Road is marked resolved though no unit ever came.
    /// The per-call choices are scripted in ShiftPlayForm (ShiftPlayForm.Shift4.cs).
    /// </summary>
    public class Shift4Scenario : IShiftScenario
    {
        public int ShiftNumber => 4;
        public string ShiftTitle => "SHIFT 04 – PROCEDURE";
        public string SupervisorName => "YSA";
        public int InitialUnits => 2;
        public bool HasTutorialGuidance => false;

        // Ysa's memo becomes the briefing book; her warning plays in the room first ("Before call convo")
        public ShiftBriefing Briefing => new(
            "Four",
            "23:00 – 03:00",
            new[]
            {
                "Do not alter records without authorization",
                "Do not submit unverified allegations",
                "Focus on your assigned duties",
                "Your evaluation reflects how well you follow procedure"
            },
            "The audit log shows activity under my login while I was on another call.")
        {
            OpeningScene = new[]
            {
                new BriefingSceneLine("", "[01:45 AM — Dispatch Center]"),
                new BriefingSceneLine("YSA", "Several irregularities have been reported in the incident logs.", "ysa_serious", "SHIFT SUPERVISOR"),
                new BriefingSceneLine("YSA", "Do not alter records without authorization. Do not submit unverified allegations.", "ysa_annoyed", "SHIFT SUPERVISOR"),
                new BriefingSceneLine("YSA", "Your performance evaluation will reflect your ability to follow procedure.", "ysa_serious", "SHIFT SUPERVISOR"),
                new BriefingSceneLine("", "[Ysa places a printed memo on your desk.]"),
                new BriefingSceneLine("YSA", "I expect all operators to focus on their assigned duties.", "ysa_talking", "SHIFT SUPERVISOR"),
                new BriefingSceneLine("", "[She walks away.]")
            }
        };

        public List<EmergencyCall> GenerateCalls()
        {
            return new List<EmergencyCall>
            {
                // CALL 01: Chloe — "You're there again." An archived recording with no matching report
                new EmergencyCall
                {
                    SlotNumber = 1,
                    CallId = "999-0401",
                    CallerName = "CHLOE",
                    Location = "14 BELL STREET",
                    InitialDialogue = "CHLOE: You're there again.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 60,
                    PanicPercentage = 45,
                    Status = CallQueueStatus.Waiting,
                    HasDiscrepancy = true
                },

                // CALL 02: Daniel — 14 Bell Street is missing from the database
                new EmergencyCall
                {
                    SlotNumber = 2,
                    CallId = "999-0419",
                    CallerName = "DANIEL",
                    Location = "14 BELL STREET",
                    InitialDialogue = "CALLER 2: Hello? Is this the emergency dispatch center?",
                    SignalStatus = "STABLE",
                    TrustPercentage = 70,
                    PanicPercentage = 30,
                    Status = CallQueueStatus.Waiting,
                    HasDiscrepancy = true
                },

                // CALL 03: Martin — 8 Miller Road, "resolved" though nobody came
                new EmergencyCall
                {
                    SlotNumber = 3,
                    CallId = "999-0433",
                    CallerName = "MARTIN",
                    Location = "8 MILLER ROAD",
                    InitialDialogue = "CALLER 3: Hello? I called earlier, but nobody came.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 55,
                    PanicPercentage = 40,
                    Status = CallQueueStatus.Waiting,
                    HasDiscrepancy = true
                },

                // CALL 04: Line 08 (Restricted)
                new EmergencyCall
                {
                    SlotNumber = 4,
                    CallId = "LINE 08",
                    CallerName = "LINE 08 (RESTRICTED)",
                    Location = "SOURCE CLASSIFIED",
                    InitialDialogue = "[ LINE 08 LOCKED BY CIVIL DEFENSE DIRECTIVE - DO NOT ANSWER ]",
                    SignalStatus = "RESTRICTED",
                    TrustPercentage = 0,
                    PanicPercentage = 0,
                    Status = CallQueueStatus.Restricted,
                    IsRestricted = true,
                    AskResponse = "SECURITY LOCK: LINE 08 ACCESS FORBIDDEN.",
                    LogResponse = "CLASSIFIED ENTRY: ARCHIVE PURGED IN 1989. CONSULT HARDCOPY DIRECTIVE ON DESK.",
                    TraceResponse = "TRACE PROHIBITED BY SUPERVISOR PROTOCOL.",
                    SendResponse = "COMMAND REJECTED: DISPATCH RESTRICTED.",
                    HoldResponse = "LINE 08 CANNOT BE HELD.",
                    FlagResponse = "RESTRICTION ENFORCED."
                }
            };
        }

        public string GetDebriefMessage(ShiftState state)
        {
            // Kept short so the trunk log still fits on the clipboard
            string headline = state.CluesFound >= 1 ? "SHIFT 04 LOG FILED." : "SHIFT 04 FILED. NO FLAGS.";
            string evidence = state.CluesFound >= 3
                ? "Discrepancies documented: Chloe's recording, 14 Bell Street, 8 Miller Road."
                : "Old records no longer match what callers remember.";

            return headline + "\n\n" + evidence + "\n" +
                   "Steff (Records Archive) unavailable.\n\n" +
                   "Shift 05 pending supervisor review.";
        }
    }
}
