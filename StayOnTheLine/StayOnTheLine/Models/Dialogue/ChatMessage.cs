using System;

namespace StayOnTheLine.Models.Dialogue
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION (immutable record)
    /// One line of the scene: a messenger bubble, a system notice, narration,
    /// or something said out loud at the desk. Script data never changes at runtime;
    /// the runner stamps a delivered copy with the terminal clock using <c>with</c>.
    /// </summary>
    public sealed record ChatMessage(ChatSender Sender, string Text)
    {
        public ChatChannel Channel { get; init; } = ChatChannel.Terminal;

        /// <summary>Sprite key shown when this line is delivered (e.g. "stef_anxious").</summary>
        public string? Portrait { get; init; }

        public SceneCue Cue { get; init; } = SceneCue.None;

        /// <summary>Red system text (the operator-login match).</summary>
        public bool IsAlert { get; init; }

        /// <summary>
        /// Minutes the terminal stamps this line away from the real clock.
        /// Negative values make a reply arrive "before" the message it answers.
        /// </summary>
        public int TimestampSkewMinutes { get; init; }

        /// <summary>Set by the runner on delivery; null while the line is still queued.</summary>
        public DateTime? Timestamp { get; init; }

        public bool IsIncoming => Sender is ChatSender.Stef or ChatSender.Charm;

        public string SpeakerName => Sender switch
        {
            ChatSender.Stef => "STEF",
            ChatSender.Player => "YOU",
            ChatSender.Charm => "CHARM",
            ChatSender.System => "SYSTEM",
            _ => string.Empty
        };
    }
}
