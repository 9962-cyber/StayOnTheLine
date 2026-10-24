using System.Collections.Generic;

namespace StayOnTheLine.Models
{
    /// <summary>One line of a short in-room scene. "{operator}" in the text is replaced with the player's name.</summary>
    public sealed record BriefingSceneLine(string Speaker, string Text, string? Sprite = null, string Tag = "");

    /// <summary>
    /// OOP Principle: ENCAPSULATION (immutable record)
    /// What the Shift Briefing book shows before a shift starts, plus an optional short scene
    /// played before the book opens. Each <see cref="IShiftScenario"/> supplies its own.
    /// </summary>
    public sealed record ShiftBriefing(
        string ShiftLabel,
        string Hours,
        IReadOnlyList<string> Procedure,
        string Note)
    {
        /// <summary>Scene played before the book (empty when the shift already has its own intro).</summary>
        public IReadOnlyList<BriefingSceneLine> OpeningScene { get; init; } = new List<BriefingSceneLine>();
    }
}
