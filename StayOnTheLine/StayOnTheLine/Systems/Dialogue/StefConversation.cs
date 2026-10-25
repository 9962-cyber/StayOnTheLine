using System;
using System.Collections.Generic;
using StayOnTheLine.Models.Dialogue;

namespace StayOnTheLine.Systems.Dialogue
{
    /// <summary>
    /// Shift 3 scripts for the records terminal threads with Stef (Records Archive), following
    /// the Shift 3 flowchart.
    /// Start of shift (01:50 AM, before the briefing): Stef's timestamp question, the three replies, the shared audit
    /// log, the operator-login match, Charm walking up to the desk, and Stef's warning to keep a copy.
    /// End of shift: Stef says an authorized account made the edits, the terminal loses her,
    /// and Charm brings coffee.
    /// </summary>
    public static class StefConversation
    {
        public const string StartNodeId = "intro";
        public const string EndStartNodeId = "endIntro";
        public const string Shift4StartNodeId = "s4Contact";

        // Evidence flags written to OperatorProfile.AnomalyFlags
        public const string FlagIgnoredStef = "Shift3IgnoredStef";
        public const string FlagOpenedAuditLog = "Shift3OpenedAuditLog";
        public const string FlagSuspectsSharedLogin = "Shift3SuspectsSharedLogin";
        public const string FlagToldCharm = "Shift3ToldCharmAboutRecords";
        public const string FlagThreadPreserved = "Shift3StefThreadPreserved";
        public const string FlagSceneSeen = "Shift3StefThreadSeen";
        public const string FlagAskedToIdentify = "Shift3AskedStefToIdentifyAccount";
        public const string FlagAskedToPreserve = "Shift3AskedStefToPreserveLog";
        public const string FlagToldStefToStop = "Shift3ToldStefToStop";
        public const string FlagEndThreadSeen = "Shift3StefEndThreadSeen";
        public const string FlagShift4SteffUnreachable = "Shift4SteffUnreachable";

        /// <summary>
        /// Shift 4 end-shift conversation: the player tries to reach Steff on the records terminal,
        /// the message is never delivered, and Charm rolls over to ask about it. Steff never appears.
        /// </summary>
        public static ConversationScript Shift4EndOfShift() => new(BuildShift4EndOfShift(), Shift4StartNodeId)
        {
            CallLabel = "END OF SHIFT",
            ClockStart = new TimeSpan(2, 58, 0),
            SeenFlag = FlagShift4SteffUnreachable,
            StartStatus = "USER UNAVAILABLE",
            ShowContactPortrait = false,
            EndTitle = "MESSAGE NOT DELIVERED",
            EndBody = "Steff still hasn't answered. The records terminal shows no activity from her account tonight.",
            EndButton = "BACK TO THE DESK",
            EndStatus = null
        };

        /// <summary>
        /// Thread that opens on the records terminal at the start of Shift 3
        /// ("Before call convo", 01:50 AM), before the shift briefing and Call 1.
        /// </summary>
        public static ConversationScript MidShift() => new(Build(), StartNodeId)
        {
            CallLabel = "BEFORE SHIFT BRIEFING",
            ClockStart = new TimeSpan(1, 50, 0),
            SeenFlag = FlagSceneSeen,
            EndTitle = "THREAD STILL OPEN",
            EndBody = "Steff's line stays active in the background. Save a copy of the log before you return to the queue.",
            EndButton = "RETURN TO QUEUE",
            EndStatus = "CONNECTION ACTIVE"
        };

        /// <summary>End-of-shift thread, after Call 3 and the audit notification.</summary>
        public static ConversationScript EndOfShift() => new(BuildEndOfShift(), EndStartNodeId)
        {
            CallLabel = "END OF SHIFT",
            ClockStart = new TimeSpan(2, 56, 0),
            SeenFlag = FlagEndThreadSeen,
            EndTitle = "USER UNAVAILABLE",
            EndBody = "The records terminal can't reach Steff. Save a copy of what's left before you finish the shift.",
            EndButton = "BACK TO THE DESK",
            EndStatus = null
        };

        public static IReadOnlyList<DialogueNode> Build()
        {
            return new List<DialogueNode>
            {
                // [01:50 AM - Dispatch Center] The player receives a message on the records terminal.
                new DialogueNode("intro",
                        Narration("[01:50 AM. A message arrives on the records terminal.]"),
                        Stef("Are you seeing strange timestamps on your end?", "stef_neutral"))
                    .Choice("Some records have been inconsistent.", "inconsistent",
                        Player("Yes. I've noticed some inconsistencies in the records."))
                    .Choice("What have you found?", "found",
                        Player("What did you find?"))
                    .Choice("Ignore the message and return to the queue.", "ignored",
                        Narration("[You close the message and return to the call queue.]"),
                        FlagIgnoredStef),

                new DialogueNode("inconsistent",
                        Stef("What kind of inconsistencies?", "stef_suspicious"),
                        Player("Some of the timestamps don't seem to match what happened."),
                        Stef("That's what I've been finding, too.", "stef_anxious"),
                        Stef("I've been checking the archived records. Some of them were modified after the incidents were supposedly closed."),
                        Stef("Others show edits from before the calls even came in.") with { TimestampSkewMinutes = -9 })
                    .Then("auditNotice"),

                new DialogueNode("found",
                        Stef("Archived records that don't match their own timestamps.", "stef_suspicious"),
                        Stef("Some were modified after the incidents were supposedly closed."),
                        Stef("Others show edits from before the calls even came in.") with { TimestampSkewMinutes = -9 },
                        Stef("I don't think these are all simple recording errors.", "stef_anxious"))
                    .Then("auditNotice"),

                new DialogueNode("ignored",
                        Narration("[Another message appears on the records terminal.]"),
                        Stef("If you're busy, I understand.", "stef_anxious"),
                        Stef("But please check the audit log when you can. I found something you should see."))
                    .Then("auditNotice"),

                // [The player receives a notification about the audit log.]
                new DialogueNode("auditNotice",
                        Sys("AUDIT LOG SHARED BY S. // RECORDS ARCHIVE") with { Cue = SceneCue.AuditNotification })
                    .Requires(new PlayerAction(
                        "PLAYER ACTION",
                        "Steff has linked the altered records. Open the audit log to review them.",
                        "OPEN AUDIT LOG",
                        "auditOpened") { EvidenceFlag = FlagOpenedAuditLog, Cue = SceneCue.OpenAuditLog }),

                new DialogueNode("auditOpened",
                        Sys("AUDIT LOG OPENED."),
                        Sys("MULTIPLE RECORD MODIFICATIONS DETECTED."),
                        Sys("MODIFICATIONS ASSOCIATED WITH PLAYER OPERATOR LOGIN.") with { IsAlert = true, Cue = SceneCue.SignalGlitch },
                        Stef("That's what I was afraid of.", "stef_suspicious"),
                        Stef("Were you using the system at those times?"))
                    .Choice("Say you don't remember making those changes.", "dontRemember",
                        Player("I don't remember making those changes."))
                    .Choice("Ask whether someone else could have used the login.", "otherLogin",
                        Player("Could someone else have used my operator login?"),
                        FlagSuspectsSharedLogin)
                    .Choice("Close the audit log without responding.", "closedLog",
                        Narration("[You close the audit log without replying.]") with { Cue = SceneCue.CloseAuditLog }),

                new DialogueNode("dontRemember",
                        Stef("Then don't assume the records are correct.", "stef_anxious"),
                        Stef("Check what you can, but keep a copy before anything changes again."))
                    .Then("charmApproach"),

                new DialogueNode("otherLogin",
                        Stef("I can't confirm that from the audit log alone.", "stef_suspicious"),
                        Stef("That's why I want you to preserve what you find."))
                    .Then("charmApproach"),

                // Per the flowchart, closing the log without replying goes straight to Charm's question
                new DialogueNode("closedLog",
                        Narration("[The records terminal remains active.]"))
                    .Then("charm"),

                // Charm approaches the console before the player can respond
                new DialogueNode("charmApproach",
                        Room("[Before you can respond, Charm approaches the console. You close the audit log.]"))
                    .Then("charm"),

                new DialogueNode("charm",
                        Charm("Everything okay over here?", "charm_smile")
                            with { Cue = SceneCue.CharmEnters | SceneCue.CloseAuditLog })
                    .Prompt("RESPOND TO CHARM - SHE IS STANDING AT YOUR DESK")
                    .InRoom()
                    .Choice("Say you're checking an unusual record.", "charmUnusual",
                        Room("[You tell her you're checking an unusual record.]"),
                        FlagToldCharm)
                    .Choice("Tell Charm it's nothing.", "charmNothing",
                        Room("[You tell her it's nothing.]"))
                    .Choice("Stay silent and return to the queue.", "charmSilent",
                        Room("[You say nothing and turn back to the queue.]")),

                new DialogueNode("charmUnusual",
                        Charm("Unusual how?", "charm_serious"),
                        Charm("Huh. Those old records have always been unreliable.", "charm_default"),
                        Room("[Charm glances at the closed audit log.]"),
                        Charm("Just be careful with them. You don't want to get blamed for a system error.", "charm_smile"))
                    .Then("charmLeaves"),

                new DialogueNode("charmNothing",
                        Charm("Oh, good. For a second, I thought something serious had happened.", "charm_smile"),
                        Room("[Charm glances at the closed audit log.]"),
                        Charm("Don't let those old records get to you. The system's always been a mess.", "charm_default"))
                    .Then("charmLeaves"),

                new DialogueNode("charmSilent",
                        Charm("Okay, then.", "charm_default"),
                        Room("[Charm pauses beside the console.]"),
                        Charm("Steff bothering you with her paperwork again?", "charm_serious"),
                        Room("[Charm glances at the closed audit log.]"),
                        Charm("Don't let those old records get to you. The system's always been a mess.", "charm_smile"))
                    .Then("charmLeaves"),

                // Ending: Stef's last message, the line stays open
                new DialogueNode("charmLeaves",
                        Room("[Charm gives a friendly little wave toward the records terminal and returns to her seat.]")
                            with { Cue = SceneCue.CharmLeaves },
                        Narration("[A new message appears on the records terminal.]") with { Cue = SceneCue.StefReturns },
                        Stef("Did you close the log?", "stef_anxious"),
                        Stef("Keep a copy of anything you find. Don't rely on the live record alone."),
                        Sys("CONNECTION ACTIVE."))
            };
        }

        public static IReadOnlyList<DialogueNode> BuildEndOfShift()
        {
            return new List<DialogueNode>
            {
                // END-SHIFT CONVERSATION: [A new message appears on the records terminal.]
                new DialogueNode("endIntro",
                        Narration("[A new message appears on the records terminal.]"),
                        Stef("I found something else.", "stef_anxious"),
                        Stef("The edits aren't random. Someone is using an authorized account.", "stef_suspicious"))
                    .Choice("Ask Steff to identify the account.", "identify",
                        Player("Can you identify which account is responsible?"),
                        FlagAskedToIdentify)
                    .Choice("Ask Steff to preserve the audit log.", "preserve",
                        Player("Can you preserve the audit log before anything else gets changed?"),
                        FlagAskedToPreserve)
                    .Choice("Tell Steff to stop investigating until the records can be reviewed officially.", "stop",
                        Player("Stop investigating for now. We should let the records be reviewed officially."),
                        FlagToldStefToStop),

                new DialogueNode("identify",
                        Stef("Not conclusively. The audit log shows which account was used, but that doesn't prove who was actually at the console.", "stef_suspicious"),
                        Stef("I need to compare the original records with the modified ones."),
                        Stef("If I'm right about this, someone has been changing records they were authorized to access.", "stef_anxious"))
                    .Then("freeze"),

                new DialogueNode("preserve",
                        Stef("I'm trying. The live database isn't enough. Some of the entries don't match the archived records.", "stef_anxious"),
                        Stef("I need to find the originals before we lose the chance to compare them."),
                        Stef("If you find anything on your end, keep a separate copy. Don't rely on the live record alone.", "stef_suspicious"))
                    .Then("freeze"),

                new DialogueNode("stop",
                        Stef("I understand. I don't want to make an accusation without proof.", "stef_neutral"),
                        Stef("But if the records are being altered, waiting could make it harder to find out what happened.", "stef_anxious"),
                        Stef("I'll stop digging for now. Just don't assume the live database tells the whole story."))
                    .Then("freeze"),

                // [Before the player can send another message, the terminal freezes.]
                new DialogueNode("freeze",
                        Narration("[Before you can send another message, the terminal freezes.]"),
                        Sys("CONNECTION LOST.") with { IsAlert = true, Cue = SceneCue.ConnectionLost })
                    .Requires(new PlayerAction(
                        "PLAYER ACTION",
                        "The records terminal stopped responding.",
                        "ATTEMPT TO RECONNECT",
                        "reconnect")),

                new DialogueNode("reconnect",
                        Sys("RECONNECTING..."),
                        Sys("CONNECTION FAILED.") with { IsAlert = true },
                        Sys("USER UNAVAILABLE.") with { IsAlert = true, Cue = SceneCue.SignalGlitch },
                        Room("[Charm approaches your console carrying two cups of coffee.]") with { Cue = SceneCue.CharmEnters },
                        Charm("Long night, huh?", "charm_smile"),
                        Charm("You look exhausted. Here.", "charm_default"),
                        Room("[She places a cup beside the keyboard.]"),
                        Charm("You should take a break when you can. You can't help anyone if you're too tired to think.", "charm_smile"),
                        Room("[Charm glances at the text terminal.]"),
                        Charm("Steff still working on those records?", "charm_serious"),
                        Room("[You look at her.]"),
                        Charm("What? I just asked.", "charm_smile"),
                        Room("[Charm returns to her console.]") with { Cue = SceneCue.CharmLeaves })
            };
        }

        public static IReadOnlyList<DialogueNode> BuildShift4EndOfShift()
        {
            return new List<DialogueNode>
            {
                // END-SHIFT CONVERSATION — PLAYER ACTION: Attempt to contact Steff.
                new DialogueNode(Shift4StartNodeId)
                    .Requires(new PlayerAction(
                        "PLAYER ACTION",
                        "Steff hasn't messaged tonight. Try to reach her on the records terminal.",
                        "ATTEMPT TO CONTACT STEFF",
                        "s4Undelivered")),

                new DialogueNode("s4Undelivered",
                        Player("Steff? Are you there?"),
                        Sys("MESSAGE NOT DELIVERED.") with { IsAlert = true },
                        Sys("USER UNAVAILABLE.") with { IsAlert = true, Cue = SceneCue.SignalGlitch },
                        Room("[Charm rolls her chair toward your console.]") with { Cue = SceneCue.CharmEnters },
                        Charm("Looking for Steff?", "charm_default"),
                        Room("[You point to the message.]"),
                        Charm("Huh. That's strange. She usually answers, even when she's busy.", "charm_serious"),
                        Charm("Maybe she finally went home. The girl practically lives in that records room.", "charm_smile"),
                        Room("[Charm looks at your saved evidence.]"),
                        Charm("Still looking into the old calls?", "charm_serious"),
                        Charm("You know, sometimes it's better to leave old mistakes in the past.", "charm_default"),
                        Room("[She pauses, then smiles.]"),
                        Charm("No offense. I just don't want you getting into trouble over something you didn't do.", "charm_smile"),
                        Room("[Charm rolls back to her console.]") with { Cue = SceneCue.CharmLeaves })
            };
        }

        // Script helpers keep the node list close to the flowchart's wording
        private static ChatMessage Stef(string text, string? portrait = null) =>
            new(ChatSender.Stef, text) { Portrait = portrait };

        private static ChatMessage Player(string text) => new(ChatSender.Player, text);

        private static ChatMessage Sys(string text) => new(ChatSender.System, text);

        private static ChatMessage Narration(string text) => new(ChatSender.Narration, text);

        private static ChatMessage Room(string text) =>
            new(ChatSender.Narration, text) { Channel = ChatChannel.Room };

        private static ChatMessage Charm(string text, string? portrait = null) =>
            new(ChatSender.Charm, text) { Channel = ChatChannel.Room, Portrait = portrait };
    }
}
