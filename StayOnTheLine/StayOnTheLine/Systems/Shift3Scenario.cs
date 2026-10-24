using System.Collections.Generic;
using StayOnTheLine.Models;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// Shift 3 – Records. Only two units tonight. Calls come from 28 Ashford Street (twice) and
    /// 62 Maple Street, while Stef messages the player about records being edited under their login.
    /// The per-call choices are scripted in ShiftPlayForm (SHIFT 3 blocks); Stef's two terminal
    /// threads run in StefSceneForm.
    /// </summary>
    public class Shift3Scenario : IShiftScenario
    {
        public int ShiftNumber => 3;
        public string ShiftTitle => "SHIFT 03 – RECORDS";
        public string SupervisorName => "KRISHA";
        public int InitialUnits => 2;
        public bool HasTutorialGuidance => false;

        // Krisha's shift briefing; a short "another shift" scene plays before the book opens
        public ShiftBriefing Briefing => new(
            "Three",
            "23:00 – 03:00",
            new[]
            {
                "Prioritize immediate danger",
                "Document every action taken",
                "Report record irregularities through the proper channel",
                "Make no unauthorized changes to records"
            },
            "Krisha's account is still active after her reported transfer. Who has been signing in as her?")
        {
            OpeningScene = new[]
            {
                new BriefingSceneLine("", "[Another night. Another shift.]"),
                new BriefingSceneLine("", "[{operator} signs in at Terminal 04. The fluorescent lights hum overhead, and the CRT monitors flicker awake.]"),
                new BriefingSceneLine("CHARM", "Another day, another shift, huh?", "charm_smile", "NEIGHBORING CONSOLE"),
                new BriefingSceneLine("CHARM", "Read the briefing before you clock in. We've only got two units tonight.", "charm_default", "NEIGHBORING CONSOLE")
            }
        };

        public List<EmergencyCall> GenerateCalls()
        {
            return new List<EmergencyCall>
            {
                // CALL 01: Someone standing outside the window (28 Ashford Street)
                new EmergencyCall
                {
                    SlotNumber = 1,
                    CallId = "999-0301",
                    CallerName = "CALLER 1",
                    Location = "28 ASHFORD STREET",
                    InitialDialogue = "CALLER 1: Hello? There's someone standing outside my window.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 70,
                    PanicPercentage = 55,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = true
                },

                // CALL 02: Someone inside the house (62 Maple Street) — record modified before the call
                new EmergencyCall
                {
                    SlotNumber = 2,
                    CallId = "999-0319",
                    CallerName = "CALLER 2",
                    Location = "62 MAPLE STREET",
                    InitialDialogue = "CALLER 2: Please, someone is inside my house.\nCALLER 2: I'm in the hallway. I can hear them moving around the other side of the door.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 65,
                    PanicPercentage = 70,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = true,
                    HasDiscrepancy = true
                },

                // CALL 03: Someone outside the door — 28 Ashford Street again
                new EmergencyCall
                {
                    SlotNumber = 3,
                    CallId = "999-0333",
                    CallerName = "CALLER 3",
                    Location = "28 ASHFORD STREET",
                    InitialDialogue = "CALLER 3: I need help. Someone's outside my door.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 60,
                    PanicPercentage = 60,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = true
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
            string clues = state.CluesFound >= 1
                ? "CLUE 3: The audit log shows activity under your login during a period when you were handling another call.\n"
                : "Audit entry not preserved. The live record may already have changed.\n";

            return "SHIFT 03 COMPLETED.\n\n" + clues +
                   "Records terminal: USER UNAVAILABLE (S. — Records Archive).\n\n" +
                   "Shift 04 clearance pending.";
        }
    }
}
