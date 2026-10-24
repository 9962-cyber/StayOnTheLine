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
                // CALL 01: Eleanor Vance (21 West Street - Apt 5B) — Routine Noise Disturbance
                new EmergencyCall
                {
                    SlotNumber = 1,
                    CallId = "999-0142",
                    CallerName = "ELEANOR VANCE",
                    Location = "21 WEST STREET, APT 5B",
                    InitialDialogue = "Hello? Is this emergency dispatch? The people directly above my apartment have been dragging heavy furniture around for hours. It's almost midnight and I can't sleep.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 75,
                    PanicPercentage = 25,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = false,
                    AskResponse = "It sounds like heavy furniture scraping the floor in Apartment 6B. I don't think anyone's in danger, but I just want it to stop.",
                    LogResponse = "LOG ARCHIVE #1042: 21 WEST STREET, APT 6B. RESIDENT: ROBERT COOPER. PRIOR INCIDENTS: NONE. CLASSIFICATION: DOMESTIC NOISE.",
                    TraceResponse = "SIGNAL LOCKED: CELL TOWER 4A (WEST DISTRICT). LANDLINE TRACE CONFIRMED.",
                    SendResponse = "PATROL UNIT 12 DISPATCHED TO 21 WEST STREET. ARRIVAL ETA: 8 MIN.",
                    HoldResponse = "LINE 01 PLACED ON HOLD. TONE ENGAGED.",
                    FlagResponse = "EVALUATION: STANDARD RESIDENTIAL NOISE COMPLAINT. NO ANOMALIES RECORDED.",
                    SupervisorHint = "KRISHA: Rule number one trainee: Always verify the caller's name and exact address before taking action.",
                    SupervisorAsk = "KRISHA: Good work. You confirmed Robert Cooper in 6B and verified no immediate threat to life.",
                    SupervisorLog = "KRISHA: Checking the log confirms this isn't a recurring disturbance.",
                    SupervisorTrace = "KRISHA: Signal confirmed. Proceed with resolution.",
                    SupervisorSend = "KRISHA: A unit is on the way, but remember: routine noise complaints rarely justify burning a patrol car.",
                    SupervisorHold = "KRISHA: Don't keep callers waiting too long unless prioritizing higher emergencies.",
                    SupervisorFlag = "KRISHA: No discrepancies here, but good habit to inspect."
                },

                // CALL 02: Chloe Bautista (14 Bell Street) — Home Intrusion & The Secret Paradox
                new EmergencyCall
                {
                    SlotNumber = 2,
                    CallId = "999-0196",
                    CallerName = "CHLOE BAUTISTA",
                    Location = "14 BELL STREET",
                    InitialDialogue = "Please send someone! Someone is trying to break into my house! I heard the back patio glass break and I'm locked upstairs!",
                    SignalStatus = "UNSTABLE (64%)",
                    TrustPercentage = 50,
                    PanicPercentage = 85,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = true,
                    HasDiscrepancy = true,
                    AskResponse = "I'm locked in the second floor bedroom! I can hear footsteps downstairs moving toward the hallway! Please hurry!",
                    LogResponse = "CRITICAL DISCREPANCY: RECORD #2196 WAS MODIFIED AT 01:42 AM. INCOMING CALL RECEIVED AT 02:16 AM. ADDRESS MISMATCH: 14 BELL ST VS 41 BELL AVE.",
                    TraceResponse = "TRACE REPORT: HIGH LINE JITTER. TOWER 11 HOOKED TO SECTOR B.",
                    SendResponse = "PATROL UNITS 08 & 14 DISPATCHED TO 14 BELL STREET UNDER CODE 3 (LIGHTS & SIRENS).",
                    HoldResponse = "WARNING: NEVER PLACE AN ACTIVE INTRUSION EMERGENCY ON HOLD.",
                    FlagResponse = "ANOMALY FLAGGED: DISCREPANCY IN MODIFICATION TIMESTAMP RECORDED IN STATION ARCHIVE.",
                    SupervisorHint = "KRISHA: Active home intrusion! Keep her calm, verify the address, and inspect the incident log immediately!",
                    SupervisorAsk = "KRISHA: Keep the caller on the line and whisper instructions to remain hidden.",
                    SupervisorLog = "KRISHA: Wait a second... that record was modified thirty-four minutes before this call even rang! FLAG that discrepancy!",
                    SupervisorTrace = "KRISHA: The signal is unstable—hurry with dispatch!",
                    SupervisorSend = "KRISHA: Units dispatched under Code 3.",
                    SupervisorHold = "KRISHA: An intruder is in the house! Never place an emergency call on hold!",
                    SupervisorFlag = "KRISHA: Excellent catch! That timestamp mismatch is going straight into tonight's official debriefing."
                },

                // CALL 03: Marcus Reed (88 Elm Ridge Road) — Suspicious Parked Sedan
                new EmergencyCall
                {
                    SlotNumber = 3,
                    CallId = "999-0109",
                    CallerName = "MARCUS REED",
                    Location = "88 ELM RIDGE ROAD",
                    InitialDialogue = "Hello? There's a car parked outside my house, and it's been there for hours.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 65,
                    PanicPercentage = 45,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = false,
                    HasDiscrepancy = false,
                    AskResponse = "It's a dark sedan. No headlights, no engine noise. I can't tell who's inside.",
                    LogResponse = "STATION ARCHIVE #0882: 88 ELM RIDGE RD. RESIDENCE ACROSS STREET VACANT FOR 6 WEEKS.",
                    TraceResponse = "SIGNAL LOCKED: CELL TOWER 2C (NORTH SUBURBS).",
                    SendResponse = "PATROL UNIT 04 DISPATCHED TO 88 ELM RIDGE ROAD FOR VEHICLE INSPECTION.",
                    HoldResponse = "LINE 03 PLACED ON HOLD.",
                    FlagResponse = "RECORD SAVED TO NIGHT PATROL SHIFT LEDGER.",
                    SupervisorHint = "KRISHA: Suspicious vehicle report. Verify details and determine if this warrants our limited patrol units.",
                    SupervisorAsk = "KRISHA: Good questioning. You gathered physical details and neighborhood context.",
                    SupervisorLog = "KRISHA: An empty house across the street explains why the car seems out of place.",
                    SupervisorTrace = "KRISHA: Line trace verified.",
                    SupervisorSend = "KRISHA: Unit 04 is en route. You burned our last patrol car on a stationary vehicle—good community caution, but remember to save units for real emergencies.",
                    SupervisorHold = "KRISHA: Routine report placed on hold.",
                    SupervisorFlag = "KRISHA: Disciplined call, trainee. An unoccupied vehicle without active threats doesn't justify burning a patrol car."
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
