namespace StayOnTheLine.Models.Dialogue
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION (immutable record)
    /// One row of the records-archive audit log.
    /// </summary>
    /// <param name="When">Shift and time of the change, e.g. "S1 01:42".</param>
    /// <param name="RecordId">Incident or archive id.</param>
    /// <param name="Location">Address on the record.</param>
    /// <param name="Change">What was modified.</param>
    /// <param name="Operator">Login that made the change.</param>
    /// <param name="MatchesPlayer">True when the change is tied to the player's own login.</param>
    public sealed record AuditRecord(string When, string RecordId, string Location, string Change, string Operator, bool MatchesPlayer);
}
