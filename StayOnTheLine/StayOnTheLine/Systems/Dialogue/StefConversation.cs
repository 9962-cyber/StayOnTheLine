using System.Collections.Generic;
using StayOnTheLine.Models.Dialogue;

namespace StayOnTheLine.Systems.Dialogue
{
    /// <summary>
    /// Shift 3 script for the records terminal thread with Stef (Records Archive),
    /// following the Shift 3 flowchart: Stef's timestamp question, the three replies,
    /// the shared audit log, the operator-login match, Charm walking up to the desk,
    /// and Stef's last warning to keep a copy.
    /// </summary>
    public static class StefConversation
    {
        public const string StartNodeId = "intro";

        // Evidence flags written to OperatorProfile.AnomalyFlags
        public const string FlagIgnoredStef = "Shift3IgnoredStef";
        public const string FlagOpenedAuditLog = "Shift3OpenedAuditLog";
        public const string FlagSuspectsSharedLogin = "Shift3SuspectsSharedLogin";
        public const string FlagToldCharm = "Shift3ToldCharmAboutRecords";
        public const string FlagThreadPreserved = "Shift3StefThreadPreserved";
        public const string FlagSceneSeen = "Shift3StefThreadSeen";

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
                        Narration("[You minimize the message and return to the call queue.]"),
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
                        "Stef has linked the altered records. Open the audit log to review them.",
                        "OPEN AUDIT LOG",
                        "auditOpened") { EvidenceFlag = FlagOpenedAuditLog }),

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
                    .Then("charm"),

                new DialogueNode("otherLogin",
                        Stef("I can't confirm that from the audit log alone.", "stef_suspicious"),
                        Stef("That's why I want you to preserve what you find."))
                    .Then("charm"),

                new DialogueNode("closedLog",
                        Narration("[The records terminal remains active.]"))
                    .Then("charm"),

                // Charm approaches the console before the player can respond
                new DialogueNode("charm",
                        Room("[Before you can respond, Charm walks up to your console. You close the audit log.]")
                            with { Cue = SceneCue.CharmEnters | SceneCue.CloseAuditLog },
                        Charm("Everything okay over here?", "charm_smile"))
                    .Prompt("RESPOND TO CHARM - SHE IS STANDING AT YOUR DESK")
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
                        Charm("Stef bothering you with her paperwork again?", "charm_serious"),
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
