using System;

namespace StayOnTheLine.Models.Dialogue
{
    /// <summary>Who a scene line belongs to; decides the bubble side and styling.</summary>
    public enum ChatSender
    {
        Stef,
        Player,
        System,
        Narration,
        Charm
    }

    /// <summary>
    /// Where a line is delivered: the records terminal (messenger thread)
    /// or the room around the desk (spoken caption under the monitor).
    /// </summary>
    public enum ChatChannel
    {
        Terminal,
        Room
    }

    /// <summary>
    /// Scene events a line triggers when it is delivered. Flags, so one line
    /// can (for example) close the audit log and bring Charm to the desk at once.
    /// </summary>
    [Flags]
    public enum SceneCue
    {
        None = 0,
        AuditNotification = 1,
        SignalGlitch = 2,
        CloseAuditLog = 4,
        CharmEnters = 8,
        CharmLeaves = 16,
        StefReturns = 32
    }
}
