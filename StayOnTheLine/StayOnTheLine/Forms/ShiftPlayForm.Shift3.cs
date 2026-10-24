using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StayOnTheLine.Models;
using StayOnTheLine.Models.Dialogue;
using StayOnTheLine.Systems;
using StayOnTheLine.Systems.Dialogue;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// SHIFT 3 – RECORDS, following the Shift 3 flowchart. Kept in its own file so the shared
    /// shift screen doesn't grow further. Same building blocks as Shift 2 (answer / ask / next-call
    /// choices); Stef's two records-terminal threads open in <see cref="StefSceneForm"/>.
    ///
    /// Flow: Stef's message (before call convo, 01:50 AM) → shift briefing → Call 1 (window,
    /// 28 Ashford St) → Call 2 (inside the house, 62 Maple St) → Call 3 (outside the door,
    /// 28 Ashford St again) → audit notification (Clue 3) → Stef's end-shift thread → finish shift.
    ///
    /// The flowchart's loops are kept: questions return to the options, and a group of questions
    /// "converges" (adds the next lines) once every question in the group has been asked.
    /// </summary>
    public partial class ShiftPlayForm
    {
        private const string S3StefBeforeCalls = "s3.stef1";
        private const string S3Briefing = "s3.briefing";
        private const string S3StefEndShift = "s3.stef2";

        // Evidence flags written to OperatorProfile.AnomalyFlags
        private const string FlagMapleTimestamp = "MapleStreetTimestamp";
        private const string FlagAuditDuringCall = "AuditActivityDuringCall";
        private const string FlagAuditPreserved = "Shift3AuditEntryPreserved";

        /// <summary>The first call only starts ringing after Stef's message and the briefing.</summary>
        private bool Shift3BriefingPending =>
            Scenario.ShiftNumber == 3 && _activeCallIndex == 0 && !Shift2Asked(S3Briefing);

        private void AddShift3Choices(EmergencyCall call, List<ActionChoice> choices)
        {
            switch (_activeCallIndex)
            {
                case 0: AddShift3Call1(call, choices); break;
                case 1: AddShift3Call2(call, choices); break;
                case 2: AddShift3Call3(call, choices); break;
            }
        }

        /// <summary>Appends a group's convergence lines to whichever of its questions is asked last.</summary>
        private string Converge(string[] group, string key, string reply, string convergence) =>
            group.Where(k => k != key).All(Shift2Asked) ? reply + "\n\n" + convergence : reply;

        // ──────────────────────────────────────────────────────────────
        // BEFORE CALL CONVO + SHIFT BRIEFING + CALL 01 (28 Ashford Street, someone at the window)
        // ──────────────────────────────────────────────────────────────
        private void AddShift3Call1(EmergencyCall call, List<ActionChoice> choices)
        {
            switch (call.Stage)
            {
                case CallStage.Ringing:
                    if (!Shift2Asked(S3StefBeforeCalls))
                    {
                        // [01:50 AM — Dispatch Center] [The player receives a message on the records terminal.]
                        choices.Add(Shift3RecordsTerminalChoice("01:50 AM", "💬 Read the message on the records terminal",
                            StefConversation.MidShift(), S3StefBeforeCalls));
                    }
                    else if (!Shift2Asked(S3Briefing))
                    {
                        choices.Add(Shift3BriefingChoice());
                    }
                    else
                    {
                        // Wait for the first call to ring → *call ringing*
                        choices.Add(Shift2AnswerChoice(call, "INCOMING LINE 01", "📞 Answer call"));
                    }
                    break;

                case CallStage.Inquiry:
                    // The three questions loop back to the options; once all three are asked they
                    // converge on the caller describing the person and the location being verified.
                    string[] tier1 = { "s3.c1.enter", "s3.c1.address", "s3.c1.safe" };
                    const string tier1Converge =
                        "CALLER 1: I can't see their face. They're just standing there.\n" +
                        "CALLER 1: I thought they were looking into the house, but I think they're looking at me.\n" +
                        "SYSTEM: LOCATION VERIFIED.";
                    void AfterTier1() { if (tier1.All(Shift2Asked)) Shift3VerifyLocation(call); }

                    if (!Shift2Asked("s3.c1.enter"))
                    {
                        choices.Add(Shift2Option("s3.c1.enter", "INTRUDER", "🪟 Ask whether the person has tried to enter",
                            Converge(tier1, "s3.c1.enter",
                                "CALLER 1: I don't think so.\nCALLER 1: I haven't heard the window open, but they're standing really close to it.\n" +
                                "CALLER 1: What if they're waiting for me to come closer?", tier1Converge),
                            string.Empty, AfterTier1));
                    }
                    if (!Shift2Asked("s3.c1.address"))
                    {
                        choices.Add(Shift2Option("s3.c1.address", "ADDRESS", "📍 Ask for the address",
                            Converge(tier1, "s3.c1.address", "CALLER 1: It's 28 Ashford Street.\nSYSTEM: LOCATION VERIFIED.", tier1Converge),
                            string.Empty,
                            () => { Shift3VerifyLocation(call); AfterTier1(); }, recommended: true));
                    }
                    if (!Shift2Asked("s3.c1.safe"))
                    {
                        choices.Add(Shift2Option("s3.c1.safe", "SAFETY", "🛡 Ask the caller to move somewhere safe if possible",
                            Converge(tier1, "s3.c1.safe",
                                "CALLER 1: There's a hallway. I can go there.\n[The caller moves away from the window.]\n" +
                                "CALLER 1: Okay. I can't see them anymore.\nCALLER 1: I can still hear something outside, though.", tier1Converge),
                            string.Empty,
                            () =>
                            {
                                call.PanicPercentage = Math.Max(20, call.PanicPercentage - 15);
                                _lblPanic.Text = $"PANIC: {call.PanicPercentage}%";
                                AfterTier1();
                            }));
                    }

                    // After converging: asking about the person again gives the description and Charm's remark
                    if (tier1.All(Shift2Asked) && !Shift2Asked("s3.c1.describe"))
                    {
                        choices.Add(Shift2Option("s3.c1.describe", "INTRUDER", "🪟 Ask whether the person has tried to enter",
                            "CALLER 1: They're wearing something dark.\n" +
                            "CALLER 1: I can't tell how tall they are. I don't want to look for too long.\n" +
                            $"[{Profile.OperatorName} records the description.]\n" +
                            "SYSTEM: CALL DETAILS RECORDED.\nSYSTEM: THREAT ASSESSMENT UPDATED.\n\n" +
                            "CHARM: If you think the caller's in danger, don't leave them waiting.\n" +
                            "[Charm says it casually, then turns back to her own monitor.]",
                            string.Empty));
                    }

                    choices.Add(Shift3DispatchChoice(() =>
                    {
                        // IF THE LOCATION IS SUFFICIENTLY ESTABLISHED AND THE SITUATION WARRANTS DISPATCH
                        if (call.AddressVerified)
                        {
                            StartTypewriter(Shift3DispatchUnit(call) +
                                "\nCALLER 1: Thank you. Please don't let them come inside.\nSYSTEM: DISPATCH RECORDED.");
                            Shift2ResolveCall(call);
                        }
                        else
                        {
                            // ELSE → back to the options
                            StartTypewriter("SYSTEM: ADDITIONAL INFORMATION REQUIRED.");
                        }
                    }));
                    break;

                case CallStage.Resolved:
                    choices.Add(Shift2NextCallChoice(1, "INCOMING LINE 02"));
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // CALL 02 – "Please, someone is inside my house." (62 Maple Street)
        // ──────────────────────────────────────────────────────────────
        private void AddShift3Call2(EmergencyCall call, List<ActionChoice> choices)
        {
            switch (call.Stage)
            {
                case CallStage.Ringing:
                    choices.Add(Shift2AnswerChoice(call, "URGENT INCOMING", "📞 Answer call"));
                    break;

                case CallStage.Inquiry:
                    // Tier 1 converges on "They're reading your messages."
                    string[] tier1 = { "s3.c2.safer", "s3.c2.address", "s3.c2.log" };
                    const string tier1Converge =
                        "CALLER 2: They're looking at something.\nCALLER 2: They're reading your messages.\nSYSTEM: SIGNAL UNSTABLE.";
                    void AfterTier1() { if (tier1.All(Shift2Asked)) SetCallSignal("UNSTABLE"); }

                    // Tier 2 converges on the signal being lost, which ends the call
                    string[] tier2 = { "s3.c2.means", "s3.c2.verify", "s3.c2.flag" };
                    string tier2Converge =
                        "[The line crackles.]\nSYSTEM: SIGNAL LOST.\n\n" +
                        $"[{Profile.OperatorName} checks the call record.]\n" +
                        "SYSTEM: CALL STATUS: DISCONNECTED.\nSYSTEM: RESPONSE OUTCOME: UNCONFIRMED.";
                    void AfterTier2()
                    {
                        if (!tier2.All(Shift2Asked)) return;
                        SetCallSignal("LOST");
                        Shift2ResolveCall(call);
                    }

                    if (!Shift2Asked("s3.c2.safer"))
                    {
                        choices.Add(Shift2Option("s3.c2.safer", "SAFETY", "🛡 Ask whether the caller can move to a safer place",
                            Converge(tier1, "s3.c2.safer",
                                "CALLER 2: I don't think so. I'm in the hallway.\nCALLER 2: There's a door between us, but I can still hear them moving.",
                                tier1Converge),
                            string.Empty, AfterTier1));
                    }
                    if (!Shift2Asked("s3.c2.address"))
                    {
                        choices.Add(Shift2Option("s3.c2.address", "ADDRESS", "📍 Verify the address",
                            Converge(tier1, "s3.c2.address",
                                "CALLER 2: It's 62 Maple Street.\nSYSTEM: ADDRESS RECEIVED.\nSYSTEM: ADDRESS VERIFICATION IN PROGRESS.\n" +
                                $"[{Profile.OperatorName} checks the available location records.]\nSYSTEM: LOCATION VERIFIED.",
                                tier1Converge),
                            string.Empty,
                            () => { Shift3VerifyLocation(call); AfterTier1(); }, recommended: true));
                    }
                    if (!Shift2Asked("s3.c2.log"))
                    {
                        choices.Add(Shift2Option("s3.c2.log", "INCIDENT LOG", "🗄 Check the incident log",
                            Converge(tier1, "s3.c2.log",
                                $"[{Profile.OperatorName} checks the incident log.]\n" +
                                "SYSTEM: LAST MODIFIED: 02:14 AM.\nSYSTEM: CALL RECEIVED: 02:19 AM.\nSYSTEM: RECORD MODIFIED BEFORE CALL RECEIVED.\n\n" +
                                "CALLER 2: Hello? Why did you stop talking?\n" +
                                $"[{Profile.OperatorName} returns to the call.]",
                                tier1Converge),
                            string.Empty, AfterTier1));
                    }

                    if (tier1.All(Shift2Asked))
                    {
                        if (!Shift2Asked("s3.c2.means"))
                        {
                            choices.Add(Shift2Option("s3.c2.means", "WHAT THEY MEAN", "❓ Ask what the caller means",
                                Converge(tier2, "s3.c2.means",
                                    "CALLER 2: Papers. Maybe a screen. I can't see clearly from here.\n" +
                                    "CALLER 2: They're looking at it like they know what's written there.\nSYSTEM: SIGNAL UNSTABLE.\n" +
                                    "CALLER 2: I can't see them. I can only hear them moving things around.",
                                    tier2Converge),
                                string.Empty, AfterTier2));
                        }
                        if (!Shift2Asked("s3.c2.verify"))
                        {
                            choices.Add(Shift2Option("s3.c2.verify", "VERIFY", "📍 Continue verifying the location and immediate danger",
                                Converge(tier2, "s3.c2.verify",
                                    "CALLER 2: I already told you.\nCALLER 2: Why do you keep asking me that?\n[The line crackles.]\n" +
                                    "CALLER 2: They said you were going to ask for my address.\nSYSTEM: SIGNAL UNSTABLE.",
                                    tier2Converge),
                                string.Empty, AfterTier2));
                        }
                        if (!Shift2Asked("s3.c2.flag"))
                        {
                            choices.Add(Shift2Option("s3.c2.flag", "FLAG", "🚩 Flag the timestamp discrepancy",
                                Converge(tier2, "s3.c2.flag",
                                    "SYSTEM: TIMESTAMP DISCREPANCY FLAGGED.\n" +
                                    "SYSTEM: LAST MODIFIED: 02:14 AM.\nSYSTEM: CALL RECEIVED: 02:19 AM.\nSYSTEM: REVIEW PENDING.",
                                    tier2Converge),
                                string.Empty,
                                () =>
                                {
                                    call.DiscrepancyFlagged = true;
                                    if (!Profile.AnomalyFlags.Contains(FlagMapleTimestamp))
                                        Profile.AnomalyFlags.Add(FlagMapleTimestamp);
                                    AudioManager.Instance.PlayInkStamp();
                                    _lblClueBanner.Text = "⭐ TIMESTAMP DISCREPANCY FLAGGED: 62 MAPLE STREET";
                                    _lblClueBanner.Visible = true;
                                    AfterTier2();
                                }, recommended: true));
                        }
                    }

                    // The call may have just ended (signal lost); no dispatch after that
                    if (call.Stage != CallStage.Inquiry) break;

                    choices.Add(Shift3DispatchChoice(() =>
                    {
                        // IF THE LOCATION IS SUFFICIENTLY ESTABLISHED
                        if (call.AddressVerified)
                        {
                            StartTypewriter(Shift3DispatchUnit(call) + "\nSYSTEM: DISPATCH RECORDED.\nCALLER 2: Thank you. Please hurry.");
                            Shift2ResolveCall(call);
                        }
                        else
                        {
                            // ELSE → back to the options
                            StartTypewriter("SYSTEM: ADDITIONAL LOCATION INFORMATION REQUIRED.");
                        }
                    }));
                    break;

                case CallStage.Resolved:
                    choices.Add(Shift2NextCallChoice(2, "INCOMING LINE 03"));
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // CALL 03 – "Someone's outside my door." (28 Ashford Street, again)
        // ──────────────────────────────────────────────────────────────
        private void AddShift3Call3(EmergencyCall call, List<ActionChoice> choices)
        {
            switch (call.Stage)
            {
                case CallStage.Ringing:
                    choices.Add(Shift2AnswerChoice(call, "INCOMING LINE 03", "📞 Answer call"));
                    break;

                case CallStage.Inquiry:
                    // Tier 1 converges on the caller asking if you're still there and
                    // Charm's "Don't leave them waiting" from the neighboring console.
                    string[] tier1 = { "s3.c3.details", "s3.c3.address", "s3.c3.danger" };
                    string tier1Converge =
                        "CALLER 3: Hello? Are you still there?\nCALLER 3: They moved closer to the door.\n\n" +
                        "[At the neighboring console, Charm removes one side of her headset.]\n" +
                        "CHARM: Don't leave them waiting. You know how that goes.\n" +
                        $"[{Profile.OperatorName} looks at Charm.]\n" +
                        "CHARM: What? It's common sense.\n[She smiles and returns to her call.]";

                    if (!Shift2Asked("s3.c3.details"))
                    {
                        choices.Add(Shift2Option("s3.c3.details", "DETAILS", "❓ Ask for more details",
                            Converge(tier1, "s3.c3.details",
                                "CALLER 3: They were trying the door earlier.\nCALLER 3: Now they've stopped.\n" +
                                "CALLER 3: I can only see the shadow under the door.\n" +
                                "CALLER 3: I don't think they're trying to get inside anymore.\nCALLER 3: I think they're waiting for someone to answer.",
                                tier1Converge),
                            string.Empty));
                    }
                    if (!Shift2Asked("s3.c3.address"))
                    {
                        choices.Add(Shift2Option("s3.c3.address", "ADDRESS", "📍 Verify the address",
                            Converge(tier1, "s3.c3.address",
                                "CALLER 3: It's 28 Ashford Street.\nSYSTEM: ADDRESS RECEIVED.\nSYSTEM: ADDRESS STATUS: VERIFIED.",
                                tier1Converge),
                            string.Empty, () => Shift3VerifyLocation(call), recommended: true));
                    }
                    if (!Shift2Asked("s3.c3.danger"))
                    {
                        choices.Add(Shift2Option("s3.c3.danger", "DANGER", "⚠ Ask whether the caller is in immediate danger",
                            Converge(tier1, "s3.c3.danger",
                                "CALLER 3: No. They're still outside.\nCALLER 3: But they've been standing there for a long time.\n" +
                                "CALLER 3: I don't think they're trying to get inside anymore.\nCALLER 3: I think they're waiting for someone to answer.",
                                tier1Converge),
                            string.Empty));
                    }

                    if (!tier1.All(Shift2Asked)) break;

                    // Tier 2: asking Charm and focusing on the caller both loop back; only dispatch ends the call
                    if (!Shift2Asked("s3.c3.charm"))
                    {
                        choices.Add(Shift2Option("s3.c3.charm", "CHARM", "🎧 Ask Charm what she means by that",
                            "CHARM: I mean people call because they're scared. They need someone to answer.\n" +
                            "CHARM: You don't want to leave someone sitting there wondering if anyone heard them.\n" +
                            "[She turns back to her console.]\nCHARM: Anyway, you've got a caller waiting.\n" +
                            "[Charm's response sounds reasonable, but the timing of her comment feels unsettling.]\n" +
                            "SYSTEM: CALL DETAILS RECORDED.",
                            string.Empty));
                    }
                    if (!Shift2Asked("s3.c3.focus"))
                    {
                        choices.Add(Shift2Option("s3.c3.focus", "FOCUS", "📞 Focus on the caller and continue assessing the situation",
                            "CALLER 3: Hello?",
                            string.Empty, recommended: true));
                    }
                    else if (!Shift2Asked("s3.c3.still"))
                    {
                        choices.Add(Shift2Option("s3.c3.still", "PLAYER ACTION", "💬 I'm still here",
                            $"{Profile.OperatorName.ToUpperInvariant()}: I'm still here.\n" +
                            "CALLER 3: Okay...\nCALLER 3: They just knocked three times.\nCALLER 3: Now they're quiet again.\n" +
                            "SYSTEM: CALL DETAILS RECORDED.",
                            string.Empty, recommended: true));
                    }

                    choices.Add(Shift3DispatchChoice(() =>
                    {
                        string dispatch = State.AvailableUnits > 0
                            ? Shift3DispatchUnit(call) + "\nSYSTEM: UNIT DISPATCHED.\nSYSTEM: RESPONSE STATUS: EN ROUTE.\nCALLER 3: Thank you."
                            : Shift3DispatchUnit(call);
                        StartTypewriter(dispatch +
                            "\nCALLER 3: They stopped knocking.\nCALLER 3: I think they're listening.\n" +
                            "SYSTEM: CALL DETAILS RECORDED.\n\nSYSTEM: CALL STATUS: COMPLETED.\nSYSTEM: CALL DETAILS SAVED.");
                        Shift2ResolveCall(call);
                    }));
                    break;

                case CallStage.Resolved:
                    AddShift3AfterCalls(choices);
                    break;
            }
        }

        // ──────────────────────────────────────────────────────────────
        // After Call 3: audit notification (Clue 3), END-SHIFT CONVERSATION, FINISH SHIFT 3
        // ──────────────────────────────────────────────────────────────
        private void AddShift3AfterCalls(List<ActionChoice> choices)
        {
            if (!Shift2Asked("s3.notice"))
            {
                string login = Profile.OperatorName.ToUpperInvariant();
                choices.Add(Shift2Option("s3.notice", "RECORDS TERMINAL", "🔔 Check the notification on the records terminal",
                    "[A notification appears on the records terminal.]\n" +
                    $"SYSTEM: AUDIT LOG: NEW ACTIVITY DETECTED.\n\nSYSTEM: OPERATOR LOGIN: {login}.\n" +
                    "SYSTEM: ACTIVITY TIMESTAMP: DURING ANOTHER ACTIVE CALL.\n\n" +
                    $"[{Profile.OperatorName} opens the audit entry. It shows that activity was recorded under their login while they were handling a different call.]\n\n" +
                    "SYSTEM: RECORD MODIFICATION SOURCE: OPERATOR LOGIN.\nSYSTEM: OPERATOR IDENTITY: NOT VERIFIED.\n\n" +
                    "CLUE 3 UNLOCKED: The audit log shows activity under your login during a period when you were handling another call.",
                    string.Empty,
                    () =>
                    {
                        State.CluesFound++;
                        if (!Profile.AnomalyFlags.Contains(FlagAuditDuringCall))
                            Profile.AnomalyFlags.Add(FlagAuditDuringCall);
                        AudioManager.Instance.PlayInkStamp();
                        _lblClueBanner.Text = "⭐ CLUE 3: ACTIVITY UNDER YOUR LOGIN DURING ANOTHER CALL";
                        _lblClueBanner.Visible = true;
                    }, recommended: true));
                return;
            }

            // [The player can preserve the audit entry or return to the call queue.]
            if (!Shift2Asked("s3.preserve") && !Shift2Asked("s3.queue"))
            {
                choices.Add(Shift2Option("s3.preserve", "PRESERVE", "🔒 Preserve the audit entry",
                    "SYSTEM: AUDIT ENTRY PRESERVED.",
                    string.Empty,
                    () =>
                    {
                        if (!Profile.AnomalyFlags.Contains(FlagAuditPreserved))
                            Profile.AnomalyFlags.Add(FlagAuditPreserved);
                        AudioManager.Instance.PlayInkStamp();
                    }, recommended: true));
                choices.Add(Shift2Option("s3.queue", "QUEUE", "↩ Return to the call queue",
                    $"[{Profile.OperatorName} closes the audit entry and returns to the call queue.]",
                    string.Empty));
                return;
            }

            if (!Shift2Asked(S3StefEndShift))
            {
                choices.Add(Shift3RecordsTerminalChoice("END-SHIFT", "💬 A new message appears on the records terminal",
                    StefConversation.EndOfShift(), S3StefEndShift));
                return;
            }

            choices.Add(new ActionChoice
            {
                Tag = "SHIFT COMPLETE",
                Caption = "📋 FINISH SHIFT 3",
                BorderColor = Color.FromArgb(140, 255, 180),
                TagColor = Color.FromArgb(180, 255, 210),
                HoverBgColor = Color.FromArgb(240, 24, 60, 30),
                IsRecommended = true,
                OnClick = () => ShowDebriefModal()
            });
        }

        // ──────────────────────────────────────────────────────────────
        // Shift 3 helpers
        // ──────────────────────────────────────────────────────────────

        private void Shift3VerifyLocation(EmergencyCall call)
        {
            call.IdentityVerified = true;
            call.AddressVerified = true;
            _lblCallerName.Text = $"▸ {call.DisplayName} ({call.CallId})";
            _lblLocation.Text = $"LOCATION: {call.DisplayLocation}";
            UpdateQueueCards();
        }

        /// <summary>Two units for the whole shift; once they're gone, dispatch only logs the request.</summary>
        private string Shift3DispatchUnit(EmergencyCall call)
        {
            AudioManager.Instance.PlayInkStamp();
            if (State.AvailableUnits <= 0)
                return "SYSTEM: NO UNITS AVAILABLE.\nSYSTEM: RESPONSE STATUS: PENDING.";
            return Shift2DispatchUnit(call);
        }

        private ActionChoice Shift3DispatchChoice(Action onClick) => new ActionChoice
        {
            Tag = "DISPATCH",
            Caption = State.AvailableUnits > 0 ? $"🚓 Dispatch unit ({State.AvailableUnits} left)" : "🚓 Dispatch unit (none available)",
            BorderColor = Color.FromArgb(255, 120, 120),
            TagColor = Color.FromArgb(255, 170, 170),
            HoverBgColor = Color.FromArgb(220, 60, 20, 20),
            OnClick = onClick
        };

        // ──────────────────────────────────────────────────────────────
        // Shift 3 briefing scene (flowchart "Shift Briefing"), played in the room
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Shift Briefing scene (after Stef's thread): Krisha and Charm at the desk.
        /// The first call only starts ringing once the scene is over.
        /// </summary>
        private ActionChoice Shift3BriefingChoice() => new ActionChoice
        {
            Tag = "SHIFT BRIEFING",
            Caption = "📋 Shift briefing",
            BorderColor = Color.FromArgb(255, 215, 120),
            TagColor = Color.FromArgb(255, 230, 140),
            IsRecommended = true,
            OnClick = () =>
            {
                StartTypewriter("SYSTEM: SHIFT 3 STARTED.\nSYSTEM: AVAILABLE UNITS: 2.");
                PlayRoomScene(new[]
                {
                    new BriefingSceneLine("", "Shift Briefing"),
                    new BriefingSceneLine("", "SYSTEM: SHIFT 3 STARTED."),
                    new BriefingSceneLine("", "SYSTEM: AVAILABLE UNITS: 2."),
                    new BriefingSceneLine("KRISHA", "We have fewer units available tonight. Prioritize immediate danger and make sure your actions are documented.", "krisha_serious", "SHIFT SUPERVISOR"),
                    new BriefingSceneLine("KRISHA", "Any record irregularities must be reported through the proper channel. Do not make unauthorized changes.", "krisha_talking", "SHIFT SUPERVISOR"),
                    new BriefingSceneLine("CHARMEE", "Two units. That's going to be a long night.", "charm_serious", "NEIGHBORING CONSOLE"),
                    new BriefingSceneLine("CHARMEE", "Well, at least we have each other.", "charm_smile", "NEIGHBORING CONSOLE")
                }, () =>
                {
                    _shift2Asked.Add(S3Briefing);
                    UpdateDynamicChoices(); // Wait for the first call to ring → *call ringing*
                });
            }
        };

        /// <summary>
        /// Opens one of Stef's records-terminal threads over the shift. Finishing it marks the step
        /// done (its evidence flags land on the profile); backing out leaves the message unread.
        /// </summary>
        private ActionChoice Shift3RecordsTerminalChoice(string tag, string caption, ConversationScript script, string doneKey) => new ActionChoice
        {
            Tag = tag,
            Caption = caption,
            BorderColor = Color.FromArgb(240, 170, 205),
            TagColor = Color.FromArgb(255, 205, 228),
            HoverBgColor = Color.FromArgb(220, 56, 22, 40),
            IsRecommended = true,
            OnClick = () =>
            {
                AudioManager.Instance.StopPhoneRing();
                DialogResult result;
                using (var scene = new StefSceneForm(Profile, script))
                    result = scene.ShowDialog(this);

                if (result == DialogResult.OK)
                {
                    _shift2Asked.Add(doneKey);
                    _lblSupervisorBar.Text = "SYSTEM: RECORDS TERMINAL MESSAGE READ.";
                }
                else
                {
                    _lblSupervisorBar.Text = "SYSTEM: RECORDS TERMINAL: 1 UNREAD MESSAGE.";
                }
                Activate();
                UpdateDynamicChoices();
            }
        };
    }
}
