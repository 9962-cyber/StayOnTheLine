using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StayOnTheLine.Models;
using StayOnTheLine.Systems;
using StayOnTheLine.Systems.Dialogue;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// SHIFT 4 – PROCEDURE, following the Shift 4 flowchart. Kept in its own file like Shift 3,
    /// built from the same blocks (answer / ask / converge / room scenes / records terminal).
    ///
    /// Flow: Ysa's memo scene → briefing book → open the address database → check the paper
    /// directory → Call 1 (Chloe, the archived recording) → Call 2 (Daniel, 14 Bell Street missing
    /// from the database) → Call 3 (Martin, 8 Miller Road marked resolved though nobody came) →
    /// Ysa warns the player off old records → end-shift: Steff can't be reached, Charm
    /// rolls over → finish shift.
    ///
    /// Every evidence choice raises <see cref="ShiftState.CluesFound"/> and adds an anomaly flag,
    /// so the End of Shift Report and later shifts can tell what the player kept.
    /// </summary>
    public partial class ShiftPlayForm
    {
        private const string S4Database = "s4.database";
        private const string S4PaperDirectory = "s4.paper";
        private const string S4YsaWarning = "s4.ysa";
        private const string S4YsaAnswered = "s4.ysa.answered";
        private const string S4EndShift = "s4.endshift";

        // Evidence flags (names from the flowchart where it gives one)
        private const string FlagChloeRecording = "ChloeRecordingPreserved";
        private const string FlagChloeMissingReport = "ChloeMissingReportFlagged";
        private const string FlagBellStreetAddress = "BELL_STREET_ADDRESS";
        private const string FlagUnconfirmedArrival = "UNCONFIRMED_UNIT_ARRIVAL";
        private const string FlagMillerRoadReview = "MillerRoadReferredForReview";
        private const string FlagPreservedEvidence = "PreservedEvidence";
        private const string FlagShift4Anomaly = "Shift4AnomalyFlagged";

        /// <summary>The first call only rings after the database and paper-directory checks.</summary>
        private bool Shift4PreCallPending =>
            Scenario.ShiftNumber == 4 && _activeCallIndex == 0 && !Shift2Asked(S4PaperDirectory);

        private void AddShift4Choices(EmergencyCall call, List<ActionChoice> choices)
        {
            switch (_activeCallIndex)
            {
                case 0: AddShift4Call1(call, choices); break;
                case 1: AddShift4Call2(call, choices); break;
                case 2: AddShift4Call3(call, choices); break;
            }
        }

        /// <summary>Records an evidence choice once: clue count, anomaly flag, banner and stamp sound.</summary>
        private void RecordShift4Evidence(string flag, string banner)
        {
            if (Profile.AnomalyFlags.Contains(flag)) return;
            Profile.AnomalyFlags.Add(flag);
            State.CluesFound++;
            AudioManager.Instance.PlayInkStamp();
            _lblClueBanner.Text = "⭐ " + banner;
            _lblClueBanner.Visible = true;
        }

        // ──────────────────────────────────────────────────────────────
        // BEFORE CALL CONVO + CALL 01 – Chloe: "You're there again."
        // ──────────────────────────────────────────────────────────────
        private void AddShift4Call1(EmergencyCall call, List<ActionChoice> choices)
        {
            switch (call.Stage)
            {
                case CallStage.Ringing:
                    if (!Shift2Asked(S4Database))
                    {
                        // PLAYER ACTION: Open the address database.
                        choices.Add(Shift2Option(S4Database, "PLAYER ACTION", "🗄 Open the address database",
                            "SYSTEM: 14 BELL STREET — RECORD NOT FOUND.",
                            string.Empty, recommended: true));
                    }
                    else if (!Shift2Asked(S4PaperDirectory))
                    {
                        choices.Add(Shift2Option(S4PaperDirectory, "PAPER DIRECTORY", "📒 Check the paper street directory beside the desk",
                            $"[{Profile.OperatorName} checks the paper street directory beside the desk.]\n" +
                            "SYSTEM: ADDRESS LISTED IN PAPER DIRECTORY.\nSYSTEM: CURRENT DATABASE ENTRY UNAVAILABLE.",
                            string.Empty, recommended: true));
                    }
                    else
                    {
                        // Wait for the first call to ring → *call ringing*
                        choices.Add(Shift2AnswerChoice(call, "INCOMING LINE 01", "📞 Answer call"));
                    }
                    break;

                case CallStage.Inquiry:
                    if (!Shift2Asked("s4.c1.archive"))
                    {
                        // The first two questions loop back to the options
                        if (!Shift2Asked("s4.c1.voice"))
                        {
                            choices.Add(Shift2Option("s4.c1.voice", "DISPATCHER", "❓ Ask how she knows it's the same dispatcher",
                                "CHLOE: I remember your voice.\nCHLOE: Last time, you were looking for the address.",
                                string.Empty));
                        }
                        if (!Shift2Asked("s4.c1.bell"))
                        {
                            choices.Add(Shift2Option("s4.c1.bell", "BELL STREET", "📍 Ask whether she's calling about Bell Street again",
                                "CHLOE: You found it before. Then it disappeared again.",
                                string.Empty, () =>
                                {
                                    call.IdentityVerified = true;
                                    _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
                                    UpdateQueueCards();
                                }));
                        }
                        choices.Add(Shift2Option("s4.c1.archive", "ARCHIVE", "🗄 Check the archived records while keeping the call active",
                            "SYSTEM: AUDIO RECORDING FOUND.\nSYSTEM: MATCHING INCIDENT REPORT NOT FOUND.\n" +
                            $"[{Profile.OperatorName} recognizes the recording as evidence of Chloe's earlier call.]",
                            string.Empty, recommended: true));
                        break;
                    }

                    if (!Shift2Asked("s4.c1.play"))
                    {
                        choices.Add(Shift2Option("s4.c1.play", "PLAYBACK", "▶ Play the archived recording",
                            "[▶ ARCHIVED AUDIO — PLAYBACK]\n[Static fills the line. An older call plays back, faint and distorted.]\n\n" +
                            "SYSTEM: RECORDING ENDS.\nSYSTEM: RESPONSE OUTCOME: NO MATCHING RECORD.\n\nCHLOE: Did you find it?",
                            string.Empty,
                            () =>
                            {
                                AudioManager.Instance.PlayBurnHiss(); // tape hiss under the playback
                                SetCallSignal("PLAYBACK");
                            }, recommended: true));
                        break;
                    }

                    // Either reply converges on Chloe asking whether you'll keep it
                    const string keepIt = "\n\nCHLOE: Are you going to keep this one?";
                    bool replied = Shift2Asked("s4.c1.tell") || Shift2Asked("s4.c1.after");
                    if (!replied)
                    {
                        choices.Add(Shift2Option("s4.c1.tell", "RECORDING", "💬 Tell Chloe the recording exists",
                            "CHLOE: So it wasn't just me.\nCHLOE: Someone really did answer." + keepIt,
                            string.Empty, () => SetCallSignal("STABLE"), recommended: true));
                        choices.Add(Shift2Option("s4.c1.after", "AFTERWARD", "❓ Ask what she remembers after the call ended",
                            "CHLOE: I kept waiting for the sound of a car.\nCHLOE: I thought maybe they were just taking a long time.\n" +
                            "CHLOE: But I never heard anyone come." + keepIt,
                            string.Empty, () => SetCallSignal("STABLE")));
                        break;
                    }

                    // [The player can preserve the recording, flag the missing report, or continue
                    //  prioritizing Chloe's current emergency needs.]
                    choices.Add(Shift4ResolveChoice(call, "PRESERVE", "🔒 Preserve the recording",
                        "SYSTEM: AUDIO RECORDING PRESERVED.\nSYSTEM: EVIDENCE FLAG ADDED: CHLOE_RECORDING.",
                        () => RecordShift4Evidence(FlagChloeRecording, "EVIDENCE: CHLOE'S EARLIER CALL — RECORDING PRESERVED"),
                        Color.FromArgb(220, 120, 255), recommended: true));
                    choices.Add(Shift4ResolveChoice(call, "FLAG", "🚩 Flag the missing report",
                        "SYSTEM: MISSING INCIDENT REPORT FLAGGED.\nSYSTEM: EVIDENCE FLAG ADDED: CHLOE_MISSING_REPORT.",
                        () => RecordShift4Evidence(FlagChloeMissingReport, "EVIDENCE: NO REPORT FOR CHLOE'S EARLIER CALL"),
                        Color.FromArgb(255, 160, 120)));
                    choices.Add(Shift4ResolveChoice(call, "CHLOE", "🛡 Keep prioritizing Chloe's current emergency",
                        $"[{Profile.OperatorName} stays on the line and focuses on Chloe's current situation.]\nSYSTEM: CALL DETAILS RECORDED.",
                        null, Color.FromArgb(120, 220, 180)));
                    break;

                case CallStage.Resolved:
                    choices.Add(Shift2NextCallChoice(1, "INCOMING LINE 02"));
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // CALL 02 – Daniel, 14 Bell Street: "The address is missing."
        // ──────────────────────────────────────────────────────────────
        private void AddShift4Call2(EmergencyCall call, List<ActionChoice> choices)
        {
            switch (call.Stage)
            {
                case CallStage.Ringing:
                    choices.Add(Shift2AnswerChoice(call, "INCOMING LINE 02", "📞 Answer call"));
                    break;

                case CallStage.Inquiry:
                    if (!Shift2Asked("s4.c2.name"))
                    {
                        choices.Add(Shift2Option("s4.c2.name", "IDENTIFY", "📇 Ask for his name and location",
                            "DANIEL: I'm Daniel. I'm calling from fourteen Bell Street.\n" +
                            "SYSTEM: ADDRESS NOT FOUND IN CURRENT DATABASE.\n" +
                            "DANIEL: There it is again. The address is missing.",
                            string.Empty,
                            () =>
                            {
                                call.IdentityVerified = true;
                                _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
                                UpdateQueueCards();
                            }, recommended: true));
                        break;
                    }

                    // Tier 1 converges on the paper directory still listing the address
                    string[] tier1 = { "s4.c2.what", "s4.c2.database" };
                    const string tier1Converge = "DANIEL: I thought it was a mistake, but the paper directory still lists it.";
                    if (!tier1.All(Shift2Asked))
                    {
                        if (!Shift2Asked("s4.c2.what"))
                        {
                            choices.Add(Shift2Option("s4.c2.what", "WHAT HAPPENED", "❓ Ask what happened",
                                Converge(tier1, "s4.c2.what",
                                    "DANIEL: Someone came by earlier asking about the people who used to live here. " +
                                    "After they left, I checked the address database, and my address was missing.", tier1Converge),
                                string.Empty));
                        }
                        if (!Shift2Asked("s4.c2.database"))
                        {
                            choices.Add(Shift2Option("s4.c2.database", "DATABASE", "🗄 Check the database",
                                Converge(tier1, "s4.c2.database",
                                    "SYSTEM: SEARCHING CURRENT ADDRESS DATABASE...\nSYSTEM: NO MATCH FOUND: 14 BELL STREET.", tier1Converge),
                                string.Empty, recommended: true));
                        }
                        break;
                    }

                    // Tier 2: the visitor and landmarks loop back; the paper directory moves the call on
                    if (!Shift2Asked("s4.c2.paper"))
                    {
                        choices.Add(Shift2Option("s4.c2.paper", "PAPER DIRECTORY", "📒 Check the paper street directory",
                            "SYSTEM: ADDRESS FOUND IN ARCHIVED PAPER DIRECTORY.\nSYSTEM: CURRENT DATABASE ENTRY UNAVAILABLE.\n" +
                            "DANIEL: See? I told you. It's right there.\n\n" +
                            "DANIEL: I just don't want someone needing help here and getting turned away because of a missing address.",
                            string.Empty,
                            () =>
                            {
                                call.AddressVerified = true;
                                _lblLocation.Text = $"LOCATION: {call.DisplayLocation}";
                                UpdateQueueCards();
                            }, recommended: true));
                        if (!Shift2Asked("s4.c2.visitor"))
                        {
                            choices.Add(Shift2Option("s4.c2.visitor", "VISITOR", "👤 Ask about the person who visited",
                                "DANIEL: They asked about the people who used to live here. " +
                                "Then they asked whether I'd heard about an emergency call from this address.",
                                string.Empty));
                        }
                        if (!Shift2Asked("s4.c2.landmarks"))
                        {
                            choices.Add(Shift2Option("s4.c2.landmarks", "LANDMARKS", "🌳 Ask for nearby landmarks",
                                "DANIEL: There's a large tree beside the gate. The house has a porch light, " +
                                "and the number fourteen is still on the mailbox.\nSYSTEM: LOCATION DETAILS RECEIVED.",
                                string.Empty));
                        }
                        break;
                    }

                    // Record and flag, document without flagging (both end the call), or ask more first (loops)
                    choices.Add(Shift4ResolveChoice(call, "FLAG", "🚩 Record the information and flag the discrepancy",
                        "EVIDENCE RECORDED.\nSYSTEM: ADDRESS DISCREPANCY FLAGGED.\nSYSTEM: EVIDENCE FLAG ADDED: BELL_STREET_ADDRESS.\n" +
                        "DANIEL: Thank you for actually checking.",
                        () =>
                        {
                            call.DiscrepancyFlagged = true;
                            RecordShift4Evidence(FlagBellStreetAddress, "EVIDENCE: 14 BELL STREET MISSING FROM THE DATABASE");
                        },
                        Color.FromArgb(220, 120, 255), recommended: true));
                    choices.Add(Shift4ResolveChoice(call, "DOCUMENT", "📝 Document the call without flagging it",
                        "SYSTEM: CALL DETAILS RECORDED.\nSYSTEM: ADDRESS DISCREPANCY REMAINS UNFLAGGED.\n" +
                        "DANIEL: All right. I hope someone looks into it.",
                        null, Color.FromArgb(200, 200, 160)));
                    if (!Shift2Asked("s4.c2.residents"))
                    {
                        choices.Add(Shift2Option("s4.c2.residents", "FORMER RESIDENTS", "❓ Ask more about the former residents first",
                            "DANIEL: I don't know their names. The visitor asked whether I'd heard about an emergency call " +
                            "from this address. I said no.",
                            string.Empty));
                    }
                    break;

                case CallStage.Resolved:
                    choices.Add(Shift2NextCallChoice(2, "INCOMING LINE 03"));
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // CALL 03 – Martin, 8 Miller Road: "I called earlier, but nobody came."
        // ──────────────────────────────────────────────────────────────
        private void AddShift4Call3(EmergencyCall call, List<ActionChoice> choices)
        {
            switch (call.Stage)
            {
                case CallStage.Ringing:
                    choices.Add(Shift2AnswerChoice(call, "INCOMING LINE 03", "📞 Answer call"));
                    break;

                case CallStage.Inquiry:
                    if (!Shift2Asked("s4.c3.name"))
                    {
                        choices.Add(Shift2Option("s4.c3.name", "IDENTIFY", "📇 Ask for the caller's name and location",
                            "MARTIN: My name is Martin. I'm at 8 Miller Road. Someone tried to force their way into my house earlier.",
                            string.Empty,
                            () =>
                            {
                                call.IdentityVerified = true;
                                call.AddressVerified = true;
                                _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
                                _lblLocation.Text = $"LOCATION: {call.DisplayLocation}";
                                UpdateQueueCards();
                            }, recommended: true));
                        break;
                    }

                    if (!Shift2Asked("s4.c3.search"))
                    {
                        if (!Shift2Asked("s4.c3.what"))
                        {
                            choices.Add(Shift2Option("s4.c3.what", "WHAT HAPPENED", "❓ Ask what happened",
                                "MARTIN: I called for help, and the person eventually left.\n" +
                                "MARTIN: I waited for an emergency unit, but nobody showed up.",
                                string.Empty));
                        }
                        choices.Add(Shift2Option("s4.c3.search", "INCIDENT LOG", "🗄 Search for the earlier incident report",
                            "SYSTEM: SEARCHING INCIDENT LOG...\nSYSTEM: MATCHING INCIDENT FOUND — 8 MILLER ROAD.\n" +
                            "SYSTEM: INCIDENT STATUS: RESOLVED.\n\nMARTIN: Resolved? Nobody came to my house.",
                            string.Empty, recommended: true));
                        break;
                    }

                    // Both checks converge on what to do with the record
                    string[] tier2 = { "s4.c3.unit", "s4.c3.timestamps" };
                    if (!tier2.All(Shift2Asked))
                    {
                        if (!Shift2Asked("s4.c3.unit"))
                        {
                            choices.Add(Shift2Option("s4.c3.unit", "UNIT", "🚓 Ask Martin to confirm whether a unit arrived",
                                "MARTIN: I waited by the front window for a long time. No emergency vehicle came down my street.",
                                string.Empty));
                        }
                        if (!Shift2Asked("s4.c3.timestamps"))
                        {
                            choices.Add(Shift2Option("s4.c3.timestamps", "TIMESTAMPS", "🕒 Check the recorded timestamps",
                                "SYSTEM: INITIAL CALL RECEIVED: 02:11 AM.\nSYSTEM: UNIT ARRIVAL RECORDED: 02:16 AM.\n" +
                                "SYSTEM: INCIDENT MARKED RESOLVED: 02:18 AM.\nSYSTEM: RECORDED OUTCOME REQUIRES VERIFICATION.",
                                string.Empty, recommended: true));
                        }
                        break;
                    }

                    // What to do with the record; each leads to Martin asking if you'll check
                    const string willYouCheck = "\n\nMARTIN: Are you going to check what happened?";
                    if (!Shift2Asked("s4.c3.outcome"))
                    {
                        choices.Add(Shift4MarkedChoice("s4.c3.outcome", "FLAG", "🚩 Flag the discrepancy and preserve the incident details",
                            "SYSTEM: INCIDENT DISCREPANCY FLAGGED.\nSYSTEM: EVIDENCE FLAG ADDED: UNCONFIRMED_UNIT_ARRIVAL." + willYouCheck,
                            () =>
                            {
                                call.DiscrepancyFlagged = true;
                                RecordShift4Evidence(FlagUnconfirmedArrival, "EVIDENCE: 8 MILLER ROAD — UNIT ARRIVAL UNCONFIRMED");
                            },
                            Color.FromArgb(220, 120, 255), recommended: true));
                        choices.Add(Shift4MarkedChoice("s4.c3.outcome", "REVIEW", "📤 Submit the incident for supervisor review",
                            "SYSTEM: INCIDENT REFERRED FOR SUPERVISOR REVIEW.\nSYSTEM: CALLER'S ACCOUNT ATTACHED." + willYouCheck,
                            () => { if (!Profile.AnomalyFlags.Contains(FlagMillerRoadReview)) Profile.AnomalyFlags.Add(FlagMillerRoadReview); },
                            Color.FromArgb(140, 200, 255)));
                        choices.Add(Shift4MarkedChoice("s4.c3.outcome", "ACCEPT", "✔ Accept the existing record and continue handling calls",
                            "SYSTEM: EXISTING INCIDENT RECORD ACCEPTED.\nSYSTEM: NO EVIDENCE FLAG ADDED." + willYouCheck,
                            null, Color.FromArgb(200, 200, 160)));
                        break;
                    }

                    if (!Shift2Asked("s4.c3.reply"))
                    {
                        choices.Add(Shift4MarkedChoice("s4.c3.reply", "REVIEWED", "💬 Tell Martin the incident will be reviewed",
                            "MARTIN: All right. I just don't want the report to say someone came when nobody did.",
                            null, Color.FromArgb(255, 215, 120), recommended: true));
                        choices.Add(Shift4MarkedChoice("s4.c3.reply", "CAN'T CONFIRM", "💬 Explain that you cannot confirm the earlier response yet",
                            "MARTIN: Then please make sure the report says that. " +
                            "I don't want someone looking at it later and thinking everything was fine.",
                            null, Color.FromArgb(255, 215, 120)));
                        break;
                    }

                    choices.Add(Shift4ResolveChoice(call, "ASSISTANCE", "❓ Ask whether he needs any further assistance",
                        "MARTIN: No. I'm safe inside for now. I just wanted someone to know what happened.\nSYSTEM: CALL 3 COMPLETE.",
                        null, Color.FromArgb(120, 220, 180), recommended: true));
                    break;

                case CallStage.Resolved:
                    AddShift4AfterCalls(choices);
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // After Call 3: Ysa at the desk, END-SHIFT CONVERSATION, FINISH SHIFT 4
        // ──────────────────────────────────────────────────────────────
        private void AddShift4AfterCalls(List<ActionChoice> choices)
        {
            if (!Shift2Asked(S4YsaWarning))
            {
                choices.Add(new ActionChoice
                {
                    Tag = "QUEUE QUIET",
                    Caption = "🔕 The call queue is temporarily quiet",
                    BorderColor = Color.FromArgb(200, 200, 200),
                    TagColor = Color.FromArgb(230, 230, 230),
                    IsRecommended = true,
                    OnClick = () => PlayRoomScene(new[]
                    {
                        new BriefingSceneLine("", "[The call queue is temporarily quiet. Ysa approaches your desk.]"),
                        new BriefingSceneLine("YSA", "I've noticed you've been spending too much time reviewing old records.", "ysa_serious", "SHIFT SUPERVISOR"),
                        new BriefingSceneLine("YSA", "Your responsibility is to handle incoming calls, not investigate the history of the department.", "ysa_talking", "SHIFT SUPERVISOR")
                    }, () =>
                    {
                        _shift2Asked.Add(S4YsaWarning);
                        UpdateDynamicChoices();
                    })
                });
                return;
            }

            if (!Shift2Asked(S4YsaAnswered))
            {
                choices.Add(Shift4YsaChoice("FOLLOW", "🫡 Follow the supervisor's instruction",
                    new[]
                    {
                        new BriefingSceneLine("YSA", "Good. I expect the same professionalism from you every night.", "ysa_smile", "SHIFT SUPERVISOR")
                    },
                    "SYSTEM: PROTOCOL SCORE MAINTAINED.\nSYSTEM: EVIDENCE NOT PRESERVED.",
                    null, Color.FromArgb(200, 200, 160)));
                choices.Add(Shift4YsaChoice("OFFICIAL", "📋 Flag the missing records through the official procedure",
                    new[]
                    {
                        new BriefingSceneLine("", "SYSTEM: ANOMALY FLAGGED."),
                        new BriefingSceneLine("YSA", "Include the reason for this flag in your end-of-shift report.", "ysa_serious", "SHIFT SUPERVISOR"),
                        new BriefingSceneLine("YSA", "I'll review your documentation before the shift is closed. Do not make conclusions that the evidence cannot support.", "ysa_annoyed", "SHIFT SUPERVISOR")
                    },
                    "SYSTEM: ANOMALY FLAGGED.\nSYSTEM: EVIDENCE FLAG ADDED.",
                    () => RecordShift4Evidence(FlagShift4Anomaly, "ANOMALY FLAGGED THROUGH OFFICIAL PROCEDURE"),
                    Color.FromArgb(140, 200, 255), recommended: true));
                choices.Add(Shift4YsaChoice("PRESERVE", "🔒 Preserve the audio log for review",
                    new[]
                    {
                        new BriefingSceneLine("", "SYSTEM: AUDIO RECORDING PRESERVED."),
                        new BriefingSceneLine("YSA", "You were instructed not to make unauthorized changes.", "ysa_serious", "SHIFT SUPERVISOR"),
                        new BriefingSceneLine("", "[You preserved a copy without altering the original record.]"),
                        new BriefingSceneLine("YSA", "Document why you preserved it in your end-of-shift report. I'll expect an explanation before you leave.", "ysa_annoyed", "SHIFT SUPERVISOR")
                    },
                    "SYSTEM: AUDIO RECORDING PRESERVED.\nSYSTEM: EVIDENCE FLAG ADDED: PreservedEvidence.",
                    () => RecordShift4Evidence(FlagPreservedEvidence, "EVIDENCE: AUDIO LOG PRESERVED FOR REVIEW"),
                    Color.FromArgb(220, 120, 255)));
                return;
            }

            if (!Shift2Asked(S4EndShift))
            {
                // END-SHIFT CONVERSATION — PLAYER ACTION: Attempt to contact Steff
                choices.Add(Shift3RecordsTerminalChoice("END-SHIFT", "💬 Attempt to contact Steff on the records terminal",
                    StefConversation.Shift4EndOfShift(), S4EndShift));
                return;
            }

            choices.Add(new ActionChoice
            {
                Tag = "SHIFT COMPLETE",
                Caption = "📋 FINISH SHIFT 4",
                BorderColor = Color.FromArgb(140, 255, 180),
                TagColor = Color.FromArgb(180, 255, 210),
                HoverBgColor = Color.FromArgb(240, 24, 60, 30),
                IsRecommended = true,
                OnClick = () => ShowDebriefModal()
            });
        }

        // ──────────────────────────────────────────────────────────────
        // Shift 4 choice builders
        // ──────────────────────────────────────────────────────────────

        /// <summary>A choice that marks a step as done (one of several alternatives sharing <paramref name="stepKey"/>).</summary>
        private ActionChoice Shift4MarkedChoice(string stepKey, string tag, string caption, string response,
            Action? onChosen, Color accent, bool recommended = false) => new ActionChoice
        {
            Tag = tag,
            Caption = caption,
            BorderColor = accent,
            TagColor = ControlPaint.Light(accent),
            IsRecommended = recommended,
            OnClick = () =>
            {
                _shift2Asked.Add(stepKey);
                onChosen?.Invoke();
                AudioManager.Instance.PlayTypewriterKeystroke();
                StartTypewriter(response);
                UpdateDynamicChoices();
            }
        };

        /// <summary>A choice that ends the current call after its response.</summary>
        private ActionChoice Shift4ResolveChoice(EmergencyCall call, string tag, string caption, string response,
            Action? onChosen, Color accent, bool recommended = false) => new ActionChoice
        {
            Tag = tag,
            Caption = caption,
            BorderColor = accent,
            TagColor = ControlPaint.Light(accent),
            IsRecommended = recommended,
            OnClick = () =>
            {
                onChosen?.Invoke();
                AudioManager.Instance.PlayTypewriterKeystroke();
                StartTypewriter(response);
                Shift2ResolveCall(call);
            }
        };

        /// <summary>Answers Ysa: the outcome plays as a room scene and is logged on the terminal.</summary>
        private ActionChoice Shift4YsaChoice(string tag, string caption, BriefingSceneLine[] scene, string terminalLog,
            Action? onChosen, Color accent, bool recommended = false) => new ActionChoice
        {
            Tag = tag,
            Caption = caption,
            BorderColor = accent,
            TagColor = ControlPaint.Light(accent),
            IsRecommended = recommended,
            OnClick = () =>
            {
                _shift2Asked.Add(S4YsaAnswered);
                onChosen?.Invoke();
                StartTypewriter(terminalLog);
                PlayRoomScene(scene, UpdateDynamicChoices);
            }
        };
    }
}
