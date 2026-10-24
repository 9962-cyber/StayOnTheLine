using System;
using System.Collections.Generic;
using StayOnTheLine.Models;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principles Demonstrated:
    /// 1. INHERITANCE / IMPLEMENTATION: Implements IShiftScenario (Abstraction).
    /// 2. ENCAPSULATION: Encapsulates all Shift 1 emergency call scripts,
    ///    branching teletype responses, and supervisor training guidance.
    /// 3. POLYMORPHISM: Provides Shift 1 specific evaluation logic and debriefings.
    ///    Co-programmer can create Shift2Scenario without modifying this file or the UI!
    /// </summary>
    public class Shift1TutorialScenario : IShiftScenario
    {
        public int ShiftNumber => 1;
        public string ShiftTitle => "SHIFT 01 — TRAINING PROTOCOL";
        public string SupervisorName => "KRISHA NARCISO";
        public int InitialUnits => 3;

        public List<EmergencyCall> GenerateCalls()
        {
            return new List<EmergencyCall>
            {
                // CALL 01: Noise Complaint at West Street Apartments
                new EmergencyCall
                {
                    SlotNumber = 1,
                    CallId = "999-0142",
                    CallerName = "ELEANOR VANCE",
                    Location = "21 WEST STREET",
                    InitialDialogue = "Hello? I'd like to report a noise complaint. Someone upstairs has been dragging heavy furniture around for hours. It's almost midnight and I can't sleep.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 88,
                    Status = CallQueueStatus.Live,
                    RequiresUnit = false,
                    AskResponse = "It sounds like heavy furniture scraping the floor in Apartment 6B. I don't think anyone's in danger, but I just want it to stop.",
                    LogResponse = "LOG ARCHIVE #1042: 21 WEST STREET, APT 6B. RESIDENT: ROBERT COOPER. PRIOR INCIDENTS: NONE.",
                    TraceResponse = "SIGNAL LOCKED: CELL TOWER 4A (WEST DISTRICT). CALLER CONFIRMED AT 21 WEST ST.",
                    SendResponse = "UNIT 12 DISPATCHED TO 21 WEST ST. PATROL ARRIVAL ETA: 8 MIN.",
                    HoldResponse = "LINE 01 PLACED ON HOLD. TONE ENGAGED.",
                    FlagResponse = "EVALUATION: STANDARD RESIDENTIAL NOISE COMPLAINT. NO ANOMALIES RECORDED.",
                    SupervisorHint = "KRISHA: Rule number one: verify the address before taking action whenever it's safe to do so.",
                    SupervisorAsk = "KRISHA: Good. You obtained the exact apartment number and confirmed no immediate danger.",
                    SupervisorLog = "KRISHA: Checking the log confirms this isn't a recurring disturbance.",
                    SupervisorTrace = "KRISHA: Landline signal verified with the district exchange.",
                    SupervisorSend = "KRISHA: A unit is on the way, though for noise complaints, logging the report is usually sufficient.",
                    SupervisorHold = "KRISHA: Don't keep callers waiting too long unless prioritizing higher emergencies.",
                    SupervisorFlag = "KRISHA: No discrepancies here, but good habit to inspect."
                },

                // CALL 02: Bell Street Intruder (The Discrepancy / Mystery Hook)
                new EmergencyCall
                {
                    SlotNumber = 2,
                    CallId = "999-0196",
                    CallerName = "MARA LANG",
                    Location = "14 BELL STREET",
                    InitialDialogue = "Please send someone! Someone's inside my house! I heard the back door break open and I locked myself in the upstairs bedroom!",
                    SignalStatus = "UNSTABLE (64%)",
                    TrustPercentage = 72,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = true,
                    HasDiscrepancy = true,
                    AskResponse = "I'm locked in the second floor bedroom! I can hear footsteps downstairs moving toward the hallway! Please hurry!",
                    LogResponse = "CRITICAL DISCREPANCY: RECORD #2196 WAS MODIFIED AT 01:42 AM. INCOMING CALL RECEIVED AT 02:16 AM. ADDRESS MISMATCH: 14 BELL ST VS 41 BELL AVE.",
                    TraceResponse = "TRACE REPORT: HIGH LINE JITTER. TOWER 11 HOOKED TO SECTOR B.",
                    SendResponse = "UNITS 08 & 14 DISPATCHED TO 14 BELL STREET UNDER CODE 3 (LIGHTS & SIRENS).",
                    HoldResponse = "WARNING: NEVER PLACE AN ACTIVE INTRUSION EMERGENCY ON HOLD.",
                    FlagResponse = "ANOMALY FLAGGED: DISCREPANCY IN MODIFICATION TIMESTAMP RECORDED IN STATION ARCHIVE.",
                    SupervisorHint = "KRISHA: Active home intrusion. Prioritize caller safety, check records, and dispatch immediately!",
                    SupervisorAsk = "KRISHA: Keep the caller on the line and whisper instructions to remain hidden.",
                    SupervisorLog = "KRISHA: Wait a second... that record was modified before the call came in! Make sure to FLAG that discrepancy!",
                    SupervisorTrace = "KRISHA: The signal is unstable—hurry with dispatch!",
                    SupervisorSend = "KRISHA: Both available units assigned. Rapid response is underway.",
                    SupervisorHold = "KRISHA: An intruder is in the house! Never place an emergency call on hold!",
                    SupervisorFlag = "KRISHA: Excellent catch! That timestamp mismatch is going straight into tonight's official debriefing."
                },

                // CALL 03: Strange Sighting near Old Quarry Rd (Mockup Anchor)
                new EmergencyCall
                {
                    SlotNumber = 3,
                    CallId = "999-0109",
                    CallerName = "UNKNOWN_CALLER",
                    Location = "OLD QUARRY RD.",
                    InitialDialogue = "I can see the dispatch building. Your window is lit.",
                    SignalStatus = "UNSTABLE",
                    TrustPercentage = 38,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = false,
                    HasDiscrepancy = true,
                    AskResponse = "You're the only lights on for miles. Who is that standing on the access road by your perimeter fence?",
                    LogResponse = "SECTOR LOG: OLD QUARRY ROAD - ABANDONED CIVIL DEFENSE TRANSMITTER SITE. NO CIVILIANS REGISTERED.",
                    TraceResponse = "TRACE WARNING: SIGNAL RELAYED THROUGH RESTRICTED SUB-STATION TRUNK LINE.",
                    SendResponse = "ADVISORY: NO UNITS AVAILABLE IN QUARRY SECTOR. PERIMETER LOCKDOWN RECOMMENDED.",
                    HoldResponse = "STATIC CRACKLE HEARD ON LINE. THE CALLER DOES NOT HANG UP.",
                    FlagResponse = "HIGH-PRIORITY ANOMALY MARKED IN TERMINAL LEDGER.",
                    SupervisorHint = "KRISHA: Stay calm. Focus on verifying who this is. Don't look out the window.",
                    SupervisorAsk = "KRISHA: Keep your voice steady. Don't let them know we're listening to the perimeter.",
                    SupervisorLog = "KRISHA: Old Quarry Road... nobody has lived out there since the 1996 closure.",
                    SupervisorTrace = "KRISHA: Trace the line! We need to know which repeater is relaying this transmission.",
                    SupervisorSend = "KRISHA: We don't have enough patrol cars to send blind into the quarry. Flag the signal first.",
                    SupervisorFlag = "KRISHA: Documented. We'll report this anomaly to Shift Trainer Ysa at debriefing."
                },

                // CALL 04: Line 08 (Restricted)
                new EmergencyCall
                {
                    SlotNumber = 4,
                    CallId = "LINE 08",
                    CallerName = "RESTRICTED LINE 08",
                    Location = "SOURCE UNKNOWN",
                    InitialDialogue = "[ LINE 08 LOCKED BY CIVIL DEFENSE DIRECTIVE - DO NOT ANSWER ]",
                    SignalStatus = "RESTRICTED",
                    TrustPercentage = 0,
                    Status = CallQueueStatus.Restricted,
                    IsRestricted = true,
                    AskResponse = "SECURITY LOCK: LINE 08 ACCESS FORBIDDEN.",
                    LogResponse = "CLASSIFIED ENTRY: ARCHIVE PURGED IN 1989. CONSULT HARDCOPY DIRECTIVE ON DESK.",
                    TraceResponse = "TRACE PROHIBITED BY SUPERVISOR PROTOCOL.",
                    SendResponse = "COMMAND REJECTED: DISPATCH RESTRICTED.",
                    HoldResponse = "LINE 08 CANNOT BE HELD.",
                    FlagResponse = "RESTRICTION ENFORCED.",
                    SupervisorHint = "KRISHA: Look at the yellow note on your desk: 'Line 08 Do not answer.' That channel has been dead for twenty years.",
                    SupervisorAsk = "KRISHA: Leave Line 08 alone.",
                    SupervisorLog = "KRISHA: Line 08 records were sealed decades ago.",
                    SupervisorTrace = "KRISHA: You don't have clearance for trunk trace 08.",
                    SupervisorSend = "KRISHA: Units are prohibited from responding to Line 08.",
                    SupervisorFlag = "KRISHA: The directive is clear."
                }
            };
        }

        public string GetDebriefMessage(ShiftState state)
        {
            if (state.CluesFound >= 1)
            {
                return "TRAINING SHIFT COMPLETED WITH DISTINCTION.\n\nAll emergency lines resolved. Critical anomalies flagged for Shift Trainer Ysa.\nPromotion to Shift 2 authorized.";
            }
            return "TRAINING SHIFT COMPLETED.\n\nBasic protocol standards met. Shift 2 access granted.";
        }
    }
}
