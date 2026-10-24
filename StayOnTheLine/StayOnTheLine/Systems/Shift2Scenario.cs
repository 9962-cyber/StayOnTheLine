using System.Collections.Generic;
using StayOnTheLine.Models;

namespace StayOnTheLine.Systems
{
    public class Shift2Scenario : IShiftScenario
    {
        public int ShiftNumber => 2;
        public string ShiftTitle => "SHIFT 02 – ESCALATION";
        public string SupervisorName => "YSA";
        public int InitialUnits => 3;
        public bool HasTutorialGuidance => false;

        // Ysa's shift briefing
        public ShiftBriefing Briefing => new(
            "Two",
            "23:00 – 03:00",
            new[]
            {
                "Prioritize the incoming queue",
                "Verify addresses",
                "Document discrepancies",
                "Don't leave urgent calls unattended"
            },
            "The Bell Street record was modified before the call was received.");

        public List<EmergencyCall> GenerateCalls()
        {
            return new List<EmergencyCall>
            {
                // CALL 01: Stranded Driver (Old Water Tower Road) — Changing Distance Anomaly
                new EmergencyCall
                {
                    SlotNumber = 1,
                    CallId = "999-0201",
                    CallerName = "CALLER 1",
                    Location = "OLD WATER TOWER ROAD",
                    InitialDialogue = "CALLER 1: Hello? My car broke down on the road. The engine just stopped, and now it won't start.\nCALLER 1: It's dark out here. I need someone to help me get off the road.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 70,
                    PanicPercentage = 45,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = true,
                    HasDiscrepancy = true,
                    AskResponse = "CALLER 1: There's a sign for the old water tower. I passed it a few minutes ago.\nSYSTEM: LOCATION CHECK: 2 MILES FROM WATER TOWER.",
                    LogResponse = "SYSTEM: LOCATION COULD NOT BE FULLY VERIFIED.",
                    TraceResponse = "SYSTEM: LOCATION CHECK: 5 MILES FROM WATER TOWER.",
                    SendResponse = "SYSTEM: UNIT ASSIGNED.\nSYSTEM: DISPATCH LOCATION BASED ON LAST AVAILABLE INFORMATION.",
                    HoldResponse = "LINE 01 PLACED ON HOLD.",
                    FlagResponse = "SYSTEM: LOCATION DISCREPANCY FLAGGED.\nCHARM: That's strange. The caller says they're standing still, but the distance keeps changing.\nCHARM: Maybe the record system is mixing up the location. Let's not pretend we know more than we do.\nSYSTEM: EVIDENCE FLAG ADDED: ChangingDistance.",
                    SupervisorHint = string.Empty,
                    SupervisorAsk = string.Empty,
                    SupervisorLog = string.Empty,
                    SupervisorTrace = string.Empty,
                    SupervisorSend = string.Empty,
                    SupervisorHold = string.Empty,
                    SupervisorFlag = string.Empty
                },

                // CALL 02: 14 Bell Street — Someone at the Door / Address Inconsistency
                new EmergencyCall
                {
                    SlotNumber = 2,
                    CallId = "999-0245",
                    CallerName = "CALLER 2",
                    Location = "14 BELL STREET",
                    InitialDialogue = "CALLER 2: Please, I need help. Someone keeps trying to open my front door.\nCALLER 2: I locked it, but I can hear the handle moving. I don't know who it is.",
                    SignalStatus = "STABLE",
                    TrustPercentage = 65,
                    PanicPercentage = 60,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = true,
                    HasDiscrepancy = false, // the script gives Bell Street no evidence clue
                    AskResponse = "CALLER 2: I'm in my bedroom. I locked the door, too.\nCALLER 2: I can still hear someone moving around outside. Please, I don't think they know I'm here.",
                    LogResponse = "SYSTEM: ADDRESS RECEIVED.\nSYSTEM: ADDRESS STATUS: INCONSISTENT WITH RECENT RECORD.",
                    TraceResponse = "CALLER 2: Fourteen Bell Street. It's the house with the broken streetlight near the corner.\nSYSTEM: ADDRESS RECEIVED: 14 BELL STREET.\nSYSTEM: ADDRESS STATUS: INCONSISTENT WITH RECENT RECORD.",
                    SendResponse = "SYSTEM: UNIT ASSIGNED.\nCALLER 2: Okay. I'll stay away from the door.\nSYSTEM: DISPATCH RECORDED.\nSYSTEM: ADDRESS DISCREPANCY REMAINS UNRESOLVED.\nSYSTEM: CALL RECORD SAVED.\nSYSTEM: CALL 2 COMPLETE.",
                    HoldResponse = "LINE 02 PLACED ON HOLD.",
                    FlagResponse = "SYSTEM: CALL DISCONNECTED BEFORE RESPONSE CONFIRMATION.\nSYSTEM: OUTCOME UNCONFIRMED.",
                    SupervisorHint = string.Empty,
                    SupervisorAsk = string.Empty,
                    SupervisorLog = string.Empty,
                    SupervisorTrace = string.Empty,
                    SupervisorSend = string.Empty,
                    SupervisorHold = string.Empty,
                    SupervisorFlag = string.Empty
                },

                // CALL 03: Chloe — Calling for Krisha / Fourteen Bell Street
                new EmergencyCall
                {
                    SlotNumber = 3,
                    CallId = "999-0288",
                    CallerName = "CHLOE",
                    Location = "FOURTEEN BELL STREET",
                    InitialDialogue = "CHLOE: Hello? Is Krisha there?",
                    SignalStatus = "STABLE",
                    TrustPercentage = 55,
                    PanicPercentage = 55,
                    Status = CallQueueStatus.Waiting,
                    RequiresUnit = false,
                    HasDiscrepancy = true,
                    AskResponse = "CHLOE: Chloe.\nCHLOE: I need to speak to Krisha. She knows about it.\nCHLOE: She told me to call back if it happened again.",
                    LogResponse = "SYSTEM: SEARCHING INCIDENT DATABASE...\nSYSTEM: NO MATCHING INCIDENT FOUND.\nCHLOE: You can check again if you want.\nCHLOE: That's what everyone does.\n[Silence.]\nCHLOE: They check. They tell me there's nothing there. Then they hang up.",
                    TraceResponse = "CHLOE: I can see a streetlight through the window.\nCHLOE: There's a house across the road. The porch light is on.\nCHLOE: I don't know the street name. I just know the number.\nSYSTEM: LOCATION UNVERIFIED.",
                    SendResponse = "SYSTEM: NO UNIT DISPATCHED — LOCATION UNVERIFIED.",
                    HoldResponse = "LINE 03 PLACED ON HOLD.",
                    FlagResponse = "CLUE: Chloe remembers Krisha, Bell Street, and a previous call that the records cannot confirm.",
                    SupervisorHint = string.Empty,
                    SupervisorAsk = string.Empty,
                    SupervisorLog = string.Empty,
                    SupervisorTrace = string.Empty,
                    SupervisorSend = string.Empty,
                    SupervisorHold = string.Empty,
                    SupervisorFlag = string.Empty
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
                    SupervisorHint = "YSA: Do not use another operator's account. Keep away from restricted channels."
                }
            };
        }

        public string GetDebriefMessage(ShiftState state)
        {
            if (state.CluesFound >= 1)
            {
                return "SHIFT 02 LOG FILED WITH DISTINCTION.\n\nCLUE 2: Krisha's account remains active after her reported transfer.\nChanging distance anomaly logged in the record.\nCLUE: Chloe remembers Krisha, Bell Street, and a previous call that the records cannot confirm.\n\nShift 03 clearance pending.";
            }

            return "SHIFT 02 COMPLETED.\n\nCLUE 2: Krisha's account remains active after her reported transfer.\n\nShift 03 clearance pending.";
        }
    }
}
