namespace StayOnTheLine.Models.Dialogue
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// One selectable response (Option 01/02/03). <see cref="Text"/> is the short option label;
    /// <see cref="Reply"/> is what actually gets sent or said when it is picked.
    /// </summary>
    public sealed record DialogueChoice(string Text, string TargetNodeId)
    {
        public ChatMessage? Reply { get; init; }

        /// <summary>Evidence flag recorded on the operator profile when this choice is taken.</summary>
        public string? EvidenceFlag { get; init; }
    }

    /// <summary>
    /// A required player action instead of a reply (e.g. OPEN AUDIT LOG).
    /// The conversation does not continue until the action is performed.
    /// </summary>
    public sealed record PlayerAction(string Title, string Description, string ButtonText, string TargetNodeId)
    {
        public string? EvidenceFlag { get; init; }
    }
}
