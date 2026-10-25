using System;
using System.Collections.Generic;

namespace StayOnTheLine.Models.Dialogue
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION (immutable record)
    /// Everything one records-terminal thread needs: its dialogue graph and where it starts,
    /// what the CRT header says, when the terminal clock starts, and the card shown once the
    /// thread is over. The scene form plays any script, so both of Shift 3's Stef threads
    /// use the same screen.
    /// </summary>
    public sealed record ConversationScript(IReadOnlyList<DialogueNode> Nodes, string StartNodeId)
    {
        /// <summary>Centre of the CRT header, e.g. "CALL 02 OF 03".</summary>
        public string CallLabel { get; init; } = string.Empty;

        /// <summary>Time of night the terminal clock starts at.</summary>
        public TimeSpan ClockStart { get; init; } = new(1, 50, 0);

        /// <summary>Flag written to the profile when the thread is finished.</summary>
        public string SeenFlag { get; init; } = string.Empty;

        public string EndTitle { get; init; } = "THREAD STILL OPEN";
        public string EndBody { get; init; } = string.Empty;
        public string EndButton { get; init; } = "RETURN TO QUEUE";

        /// <summary>Contact status shown at the end, or null to keep whatever the thread left it at.</summary>
        public string? EndStatus { get; init; }

        /// <summary>
        /// Contact status when the thread opens, or null for the default "ONLINE".
        /// Shown in red, for a contact who can't be reached (e.g. "USER UNAVAILABLE").
        /// </summary>
        public string? StartStatus { get; init; }

        /// <summary>False when the contact never appears in person (nobody is on the other end).</summary>
        public bool ShowContactPortrait { get; init; } = true;
    }
}
